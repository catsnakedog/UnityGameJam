using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
namespace ShelterRun.Editor
{
    public static class FarmingVerification
    {
        static readonly List<string> checks = new List<string>();
        static string databaseSnapshot, rulesSnapshot;
        public static string RunEditChecks()
        {
            checks.Clear();
            var rows = FarmingCsvImporter.Parse(File.ReadAllText(FarmingCsvImporter.CsvPath));
            Check(rows.Count(x => x.Type == FarmingObjectType.Person) == 6, "CSV contains exactly six people");
            Check(rows.All(x => x.Weight >= (x.Type == FarmingObjectType.Person ? 2 : 1) && x.Weight <= (x.Type == FarmingObjectType.Person ? 4 : 2)), "CSV weights respect type ranges");
            var quoted = FarmingCsvImporter.Parse("\uFEFFid,type,weight\r\n\"item,quoted\",\"Item\",\"2\"\r\n");
            Check(quoted[0].Id == "item,quoted", "CSV supports BOM, CRLF and quoted comma");
            var escaped = FarmingCsvImporter.Parse("id,type,weight\n\"a\"\"b\",Item,1\n");
            Check(escaped[0].Id == "a\"b", "CSV supports escaped quotes");
            foreach (string bad in new[] {
                "id,type,weight\na,Item,1\na,Item,2", "id,type,weight\na,Person,1",
                "id,type,weight\na,Item,3", "id,type,weight\na,Other,1",
                "id,type,weight\na,Item,NaN", "id,type,weight\na,Item,1.5",
                "id,type,weight\n\"broken,Item,1", "id,type,weight\n\"line\nbreak\",Item,1" })
            {
                bool rejected = false;
                try { FarmingCsvImporter.Parse(bad); } catch (FormatException) { rejected = true; }
                Check(rejected, "Invalid CSV rejected: " + bad.Replace('\n', '/'));
            }
            var db = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);
            var rules = AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath);
            db.Validate(); rules.Validate();
            Check(rules.Duration == 60 && rules.Capacity == 4, "Saved SO defaults: 60 seconds / 4 slots");
            databaseSnapshot = EditorJsonUtility.ToJson(db);
            rulesSnapshot = EditorJsonUtility.ToJson(rules);
            Write("edit-passed");
            return string.Join("\n", checks);
        }

        // Kept for existing CLI callers; play verification now exercises the 2D map.
        public static void StartPlayChecks() => FarmingIsoVerification.Start();
        static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks.Add("PASS: " + label);
        }
        static void Write(string phase)
        {
            Directory.CreateDirectory("Docs/Farming");
            File.WriteAllText("Docs/Farming/Verification-" + phase + ".txt", DateTimeOffset.Now.ToString("O") + "\n" + string.Join("\n", checks), new UTF8Encoding(false));
        }
    }
}
