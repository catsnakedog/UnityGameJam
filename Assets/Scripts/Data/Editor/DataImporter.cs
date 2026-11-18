using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace GameJam.Data.Editor
{
    public sealed class DataImportPlan
    {
        public List<DataTable> Tables { get; internal set; }
        public string SourceHash { get; internal set; }
        public string PreviousJson { get; internal set; }
        public string GeneratedCode { get; internal set; }
        public List<string> Changes { get; } = new List<string>();
        public bool HasRemovals { get; internal set; }
        public string Folder { get; internal set; }
    }

    public static class DataImporter
    {
        public const string DatabasePath = "Assets/Settings/Data/GameDatabase.asset";
        public const string HistoryFolder = "Docs/DataHistory";
        public const string GroupName = "Game Data Local";

        public static Dictionary<string, string> ReadSources(string folder)
        {
            string full = ValidateFolder(folder);
            if (!Directory.Exists(full)) throw new DirectoryNotFoundException("CSV folder does not exist: " + folder);
            var sources = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(full, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                if (!string.Equals(Path.GetExtension(file), ".csv", StringComparison.OrdinalIgnoreCase)) continue;
                string name = Path.GetFileName(file);
                if (name.StartsWith("~") || name.StartsWith(".") || name.StartsWith("#")) continue;
                // Excel can keep a write handle open while allowing shared reads.
                // Apply still re-reads and compares the complete source fingerprint.
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true))
                    sources.Add(file.Substring(full.Length).TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/'), reader.ReadToEnd());
            }
            return sources;
        }

        public static string ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Choose a CSV folder under Assets.");
            string full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar);
            if (!full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("CSV folder must be a subfolder of this project's Assets folder.");
            return full;
        }

        public static DataImportPlan Preview(string folder) => PreviewSources(folder, ReadSources(folder));

        public static string SourcesHash(IDictionary<string, string> sources)
        {
            var fingerprint = new StringBuilder();
            foreach (var pair in sources.OrderBy(p => p.Key, StringComparer.Ordinal))
                fingerprint.Append(pair.Key.Length).Append(':').Append(pair.Key).Append(pair.Value.Length).Append(':').Append(pair.Value);
            return Hash(fingerprint.ToString());
        }

        public static DataImportPlan PreviewSources(string folder, IDictionary<string, string> sources)
        {
            ValidateFolder(folder);
            if (sources.Count == 0) throw new FormatException("No CSV files found. Empty imports are rejected.");
            var plan = new DataImportPlan { Folder = folder, Tables = new List<DataTable>() };
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in sources.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var table = CsvTableReader.Read(pair.Key, pair.Value);
                if (!names.Add(table.Name)) throw new FormatException("Duplicate table filename: " + table.Name);
                plan.Tables.Add(table);
            }
            plan.SourceHash = SourcesHash(sources);
            plan.GeneratedCode = DataCodeGenerator.Generate(plan.Tables);
            var current = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            plan.PreviousJson = current ? EditorJsonUtility.ToJson(current, true) : null;
            if (current) current.Initialize();
            Compare(plan, current ? current.Tables : Array.Empty<DataTable>());
            return plan;
        }

        public static void Compare(DataImportPlan plan, IReadOnlyList<DataTable> previous)
        {
            foreach (var old in previous)
            {
                var next = plan.Tables.FirstOrDefault(t => t.Name == old.Name);
                if (next == null) { Remove(plan, $"Remove table {old.Name} ({old.Count} rows)"); continue; }
                foreach (var column in old.Columns)
                    if (!next.Columns.Any(c => c.Name == column.Name && c.Type == column.Type))
                        Remove(plan, $"Remove/change column {old.Name}.{column.Name}:{column.Type}");
                if (old.Columns[old.KeyIndex].Name != next.Columns[next.KeyIndex].Name)
                    Remove(plan, $"Change key {old.Name}: {old.Columns[old.KeyIndex].Name} -> {next.Columns[next.KeyIndex].Name}");
                foreach (var row in old.Rows)
                    if (!next.TryGet(row.Id, out _)) Remove(plan, $"Remove row {old.Name}[{row.Id}]");
            }
            foreach (var next in plan.Tables)
            {
                var old = previous.FirstOrDefault(t => t.Name == next.Name);
                if (old == null) plan.Changes.Add($"Add table {next.Name} ({next.Count} rows)");
                foreach (var column in next.Columns)
                    if (old == null || !old.Columns.Any(c => c.Name == column.Name)) plan.Changes.Add($"Add column {next.Name}.{column.Name}:{column.Type}");
                foreach (var row in next.Rows)
                {
                    DataRow oldRow = null;
                    bool exists = old != null && old.TryGet(row.Id, out oldRow);
                    for (int c = 0; c < next.Columns.Count; c++)
                    {
                        string before = exists && old.Columns.Any(x => x.Name == next.Columns[c].Name) ? oldRow.Cells[old.ColumnIndex(next.Columns[c].Name)] : null;
                        string after = row.Cells[c];
                        if (before != after) plan.Changes.Add($"{next.Name}[{row.Id}].{next.Columns[c].Name}: {Display(before)} -> {Display(after)}");
                    }
                }
            }
        }

        static string Display(string value) => value == null ? "<missing>" : "\"" + value.Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        static void Remove(DataImportPlan plan, string change) { plan.HasRemovals = true; plan.Changes.Add(change); }

        public static void Apply(DataImportPlan plan, bool allowRemovals = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before importing data.");
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var fresh = Preview(plan.Folder);
            if (fresh.SourceHash != plan.SourceHash || fresh.PreviousJson != plan.PreviousJson)
                throw new InvalidOperationException("CSV or database changed since preview. Validate again before applying.");
            if (fresh.HasRemovals && !allowRemovals) throw new InvalidOperationException("Removal/schema changes detected. Review the preview and enable Allow removals.");
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            string existingGuid = AssetDatabase.AssetPathToGUID(DatabasePath);
            if (settings.groups.Where(g => g != null).SelectMany(g => g.entries).Any(e => e.address == GameData.CatalogAddress && e.guid != existingGuid))
                throw new InvalidOperationException("Address already in use: " + GameData.CatalogAddress);
            WriteReport("planned", fresh, null, "Validation succeeded; applying the reviewed snapshot.");
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
            Directory.CreateDirectory(Path.GetDirectoryName(DataCodeGenerator.OutputPath));
            AssetDatabase.Refresh();
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            bool created = !database;
            string oldCode = File.Exists(DataCodeGenerator.OutputPath) ? File.ReadAllText(DataCodeGenerator.OutputPath) : null;
            try
            {
                if (created)
                {
                    database = ScriptableObject.CreateInstance<GameDatabase>();
                    AssetDatabase.CreateAsset(database, DatabasePath);
                }
                database.ApplyValidated(fresh.Tables, fresh.SourceHash);
                var group = settings.FindGroup(GroupName) ?? settings.CreateGroup(GroupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string guid = AssetDatabase.AssetPathToGUID(DatabasePath);
                var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group);
                entry.address = GameData.CatalogAddress;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                if (oldCode != fresh.GeneratedCode) File.WriteAllText(DataCodeGenerator.OutputPath, fresh.GeneratedCode, new UTF8Encoding(false));
                WriteReport("applied", fresh, database, "CSV -> database and typed accessors. Content build is still required for deployment.");
            }
            catch (Exception error)
            {
                if (created)
                {
                    settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(DatabasePath));
                    AssetDatabase.DeleteAsset(DatabasePath);
                }
                else if (database)
                {
                    EditorJsonUtility.FromJsonOverwrite(fresh.PreviousJson, database);
                    database.Initialize(); EditorUtility.SetDirty(database);
                }
                if (oldCode != null) File.WriteAllText(DataCodeGenerator.OutputPath, oldCode, new UTF8Encoding(false));
                else if (File.Exists(DataCodeGenerator.OutputPath)) File.Delete(DataCodeGenerator.OutputPath);
                AssetDatabase.SaveAssets();
                WriteReport("failed", fresh, database, "Restored previous database/code after error: " + error.Message);
                throw;
            }
            finally { AssetDatabase.Refresh(); }
        }

        public static void BuildContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before building content.");
            var plan = Preview(DataToolSettings.instance.csvFolder);
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (!database || database.SourceHash != plan.SourceHash) throw new InvalidOperationException("Import the current CSV files before building content.");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            WriteReport(string.IsNullOrEmpty(result.Error) ? "built" : "failed", plan, database, "Addressables content build: " + (string.IsNullOrEmpty(result.Error) ? "success" : result.Error));
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        }

        public static string Hash(string text)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }

        static void WriteReport(string phase, DataImportPlan plan, GameDatabase database, string result)
        {
            Directory.CreateDirectory(HistoryFolder);
            string id = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fffffff") + "-data-" + phase;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings ? settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(DatabasePath)) : null;
            string profile = settings ? settings.profileSettings.GetProfileName(settings.activeProfileId) : "unconfigured";
            string content = $"# {id}\n\n- Project: UnityGameJam\n- Timestamp: {DateTimeOffset.Now:O}\n- Phase: {phase}\n- Source: `{plan.Folder}` (CSV schema v1, authoritative)\n- SHA256: `{plan.SourceHash}`\n- Asset: `{DatabasePath}`\n- Type: GameJam.Data.GameDatabase\n- GUID: `{AssetDatabase.AssetPathToGUID(DatabasePath)}`\n- Address: `{GameData.CatalogAddress}`\n- Group: `{entry?.parentGroup?.Name ?? "unregistered"}`\n- Profile: `{profile}`\n- Labels: {string.Join(", ", entry?.labels ?? new HashSet<string>())}\n- Consumers: GameData.InitializeAsync / GetTable / Get / TryGet; generated typed accessors. Application lifetime, Release on shutdown.\n- Result: {result}\n\n## Changes\n\n";
            content += string.Join("\n", plan.Changes.Select(c => "- " + c));
            content += "\n\n## Full managed database snapshot\n\n```json\n" + (database ? EditorJsonUtility.ToJson(database, true) : plan.PreviousJson ?? "null") + "\n```\n";
            using (var stream = new FileStream(Path.Combine(HistoryFolder, id + ".md"), FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(content);
        }
    }
}
