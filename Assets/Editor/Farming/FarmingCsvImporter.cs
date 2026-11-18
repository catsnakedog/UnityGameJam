using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ShelterRun.Editor
{
    public static class FarmingCsvImporter
    {
        public const string CsvPath = "Assets/Settings/Farming/FarmingData.csv";
        public const string DatabasePath = "Assets/Settings/Farming/FarmingDatabase.asset";
        public const string RulesPath = "Assets/Settings/Farming/FarmingRules.asset";

        public static List<FarmingRecord> Parse(string text)
        {
            var rows = ReadRows(text.TrimStart('\uFEFF'));
            // The shared spreadsheet template has description / type / name rows.
            // Keep FarmingData on its existing importer and database, never the common catalog.
            if (rows.Count >= 4 && rows[0].Count == 3 &&
                rows[1].SequenceEqual(new[] { "string", "string", "int" }) &&
                rows[2].SequenceEqual(new[] { "id", "type", "weight" }))
                rows.RemoveRange(0, 2);
            if (rows.Count < 2 || !rows[0].SequenceEqual(new[] { "id", "type", "weight" }))
                throw new FormatException("FarmingData: header must be id,type,weight with at least one row.");
            var result = new List<FarmingRecord>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0])) continue;
                if (row.Count != 3) throw new FormatException($"FarmingData row {i + 1}: expected 3 columns.");
                string id = row[0];
                if (string.IsNullOrWhiteSpace(id) || id.Any(char.IsWhiteSpace) || !ids.Add(id))
                    throw new FormatException($"FarmingData row {i + 1}: invalid/duplicate id {id}.");
                if (row[1] != "Person" && row[1] != "Item") throw new FormatException($"Invalid type at {id}: {row[1]}");
                var type = row[1] == "Person" ? FarmingObjectType.Person : FarmingObjectType.Item;
                if (!int.TryParse(row[2], NumberStyles.None, CultureInfo.InvariantCulture, out int weight))
                    throw new FormatException($"Invalid integer weight at {id}: {row[2]}");
                if (weight < (type == FarmingObjectType.Person ? 2 : 1) || weight > (type == FarmingObjectType.Person ? 4 : 2))
                    throw new FormatException($"Weight outside supported range at {id}: {weight}");
                result.Add(new FarmingRecord(id, type, weight));
            }
            if (result.Count == 0) throw new FormatException("FarmingData contains no records.");
            return result;
        }

        static List<List<string>> ReadRows(string input)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false, closed = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < input.Length && input[i + 1] == '"') { cell.Append('"'); i++; }
                        else { quoted = false; closed = true; }
                    }
                    else cell.Append(c);
                    continue;
                }
                if (c == '"')
                {
                    if (cell.Length > 0 || closed) throw new FormatException("Unexpected CSV quote.");
                    quoted = true;
                }
                else if (c == ',' || c == '\r' || c == '\n')
                {
                    row.Add(cell.ToString()); cell.Clear(); closed = false;
                    if (c != ',')
                    {
                        rows.Add(row); row = new List<string>();
                        if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n') i++;
                    }
                }
                else
                {
                    if (closed) throw new FormatException("Unexpected character after CSV closing quote.");
                    cell.Append(c);
                }
            }
            if (quoted) throw new FormatException("Unterminated quoted CSV field.");
            if (cell.Length > 0 || row.Count > 0 || closed) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }

        [MenuItem("Tools/Farming/Import FarmingData CSV")]
        public static void Import()
        {
            string csv = File.ReadAllText(CsvPath, Encoding.UTF8);
            var rows = Parse(csv); // No mutation before every row and deletion policy is checked.
            var database = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(DatabasePath);
            var oldRows = database ? database.Records.ToList() : new List<FarmingRecord>();
            var missing = oldRows.Where(x => rows.All(y => y.Id != x.Id)).Select(x => x.Id).ToArray();
            if (missing.Length > 0) throw new InvalidOperationException("CSV missing existing IDs; deletion requires an explicit data migration: " + string.Join(", ", missing));
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(csv))).Replace("-", "").ToLowerInvariant();
            if (database && database.SourceHash == hash) return;
            var preview = new StringBuilder();
            foreach (var row in rows)
            {
                var previous = oldRows.Find(x => x.Id == row.Id);
                if (previous == null) preview.AppendLine($"- Add {row.Id}: {row.Type}, {row.Weight}");
                else if (previous.Type != row.Type || previous.Weight != row.Weight)
                    preview.AppendLine($"- Update {row.Id}: {previous.Type}/{previous.Weight} → {row.Type}/{row.Weight}");
            }
            AppendReport("planned-csv", preview.ToString(), database, null);
            bool created = database == null;
            if (created) database = ScriptableObject.CreateInstance<FarmingDatabase>();
            string before = EditorJsonUtility.ToJson(database);
            try
            {
                database.ApplyValidated(rows, hash);
                database.Validate();
                if (created) AssetDatabase.CreateAsset(database, DatabasePath);
                EditorUtility.SetDirty(database);
                Register(database, FarmingDataKeys.Database);
                AssetDatabase.SaveAssets();
                AppendReport("applied-csv", preview.ToString(), database, null);
                Debug.Log($"FarmingData imported: {rows.Count} records. Rebuild Addressables after edits via Tools/Farming/Build Data Content.");
            }
            catch (Exception e)
            {
                if (!created) { EditorJsonUtility.FromJsonOverwrite(before, database); EditorUtility.SetDirty(database); AssetDatabase.SaveAssets(); }
                AppendReport("failed-csv", e.ToString(), database, null);
                throw;
            }
        }

        public static void Register(UnityEngine.Object asset, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup("Farming Local") ?? settings.CreateGroup("Farming Local", false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)), group);
            entry.address = address;
            settings.AddLabel("farming"); entry.SetLabel("farming", true);
            EditorUtility.SetDirty(settings);
        }

        [MenuItem("Tools/Farming/Build Data Content")]
        public static void BuildContent()
        {
            Import();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            // Use Existing Build checks the actual local bundles in Play Mode.
            int index = settings.DataBuilders.FindIndex(x => x is UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedPlayMode);
            if (index < 0) throw new InvalidOperationException("Packed Play Mode data builder missing.");
            settings.ActivePlayModeDataBuilderIndex = index;
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            AppendReport("built-content", $"Profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}\nOutput: {result.OutputPath}\nDuration: {result.Duration}",
                AssetDatabase.LoadAssetAtPath<FarmingDatabase>(DatabasePath), AssetDatabase.LoadAssetAtPath<FarmingRules>(RulesPath));
        }

        public static void AppendReport(string phase, string detail, FarmingDatabase database, FarmingRules rules)
        {
            Directory.CreateDirectory("Docs/DataHistory");
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fffffff");
            var sb = new StringBuilder($"# Farming · {phase}\n\nTimestamp: {DateTimeOffset.Now:O}\nProject: UnityGameJam\nPrevious: 20261118-01-planned.md / prior timestamped record\nReason: Farming CSV authoring and 60-second prototype.\n\n{detail}\n\n");
            foreach (var asset in new UnityEngine.Object[] { database, rules ? rules : AssetDatabase.LoadAssetAtPath<FarmingRules>(RulesPath) })
            {
                if (!asset) continue;
                string path = AssetDatabase.GetAssetPath(asset);
                string guid = AssetDatabase.AssetPathToGUID(path);
                var entry = AddressableAssetSettingsDefaultObject.Settings?.FindAssetEntry(guid);
                sb.AppendLine($"## {asset.name}\nPath: {path}\nGUID: {guid}\nAddress: {entry?.address ?? "unregistered"}\nGroup: {entry?.parentGroup.Name ?? "unregistered"}\nLabels: farming\n");
                sb.AppendLine("```json\n" + EditorJsonUtility.ToJson(asset, true) + "\n```\n");
            }
            sb.AppendLine("Source: FarmingData.csv v1 (id/type/weight), FarmingRules SO.\nConsumers: FarmingDataLoader → FarmingSession, FarmingPlayer, FarmingPickup, FarmingHud. Scene-owned handles released on teardown; values never mutated by gameplay.\nRuntime validation: see Docs/Farming/Verification.md; this record alone does not prove gameplay.");
            File.WriteAllText($"Docs/DataHistory/{stamp}-{phase}.md", sb.ToString(), new UTF8Encoding(false));
        }
    }

    public sealed class FarmingCsvPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Array.IndexOf(imported, FarmingCsvImporter.CsvPath) < 0 || !File.Exists(FarmingCsvImporter.DatabasePath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { FarmingCsvImporter.Import(); }
                catch (Exception e) { Debug.LogError("FarmingData was not applied: " + e.Message); }
            };
        }
    }
}
