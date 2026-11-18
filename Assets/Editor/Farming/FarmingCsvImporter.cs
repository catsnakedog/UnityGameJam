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

namespace ShelterRun.Editor
{
    public static class FarmingCsvImporter
    {
        public const string CsvPath = "Assets/Settings/Farming/FarmingData.csv";
        public const string DatabasePath = "Assets/Settings/Farming/FarmingDatabase.asset";
        public const string RulesPath = "Assets/Settings/Farming/FarmingRules.asset";

        public static void ValidateRemovals(IEnumerable<FarmingRecord> previous, List<FarmingRecord> next) =>
            FarmingCsv.ValidateRemovals(previous, next);

        public static List<FarmingRecord> Parse(string text) => FarmingCsv.Parse(text);

        [MenuItem("Tools/Farming/Import FarmingData CSV")]
        public static void Import()
        {
            string csv = File.ReadAllText(CsvPath, Encoding.UTF8);
            var rows = Parse(csv); // No mutation before every row and deletion policy is checked.
            var database = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(DatabasePath);
            var oldRows = database ? database.Records.ToList() : new List<FarmingRecord>();
            ValidateRemovals(oldRows, rows);
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
            var sb = new StringBuilder($"# Farming · {phase}\n\nTimestamp: {DateTimeOffset.Now:O}\nProject: UnityGameJam\nPrevious: 20261118-01-planned.md / prior timestamped record\nReason: Farming data and content build workflow.\n\n{detail}\n\n");
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
