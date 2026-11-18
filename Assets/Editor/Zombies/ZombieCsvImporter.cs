using System;
using System.IO;
using GameJam.Data;
using GameJam.Data.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameJam.Zombies;

// Editor projection only: both sheets generate the existing data/zombies asset.
public static class ZombieCsvImporter
{
    public const string CatalogPath = "Assets/Settings/Shelter/Zombies.csv";
    public const string RulesPath = "Assets/Settings/Shelter/ZombieRules.csv";

    public static ZombieData Parse(string catalogCsv, string rulesCsv)
    {
        var catalog = CsvTableReader.Read("Zombies.csv", catalogCsv);
        var rules = CsvTableReader.Read("ZombieRules.csv", rulesCsv);
        return ZombieData.FromTables(catalog, rules);
    }

    public static void ValidateMigration(ZombieData next, ZombieData current) => ZombieData.ValidateMigration(next, current);

    public static void Apply(ZombieData next, ZombieData target)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before importing zombies.");
        ValidateMigration(next, target);
        // JsonUtility only copies the data fields; the existing name, script, GUID and references survive.
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(next), target);
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }

    public static void ValidateCurrent()
    {
        var parsed = Parse(File.ReadAllText(CatalogPath), File.ReadAllText(RulesPath));
        try
        {
            var target = AssetDatabase.LoadAssetAtPath<ZombieData>(ZombieSetup.DataPath);
            ValidateMigration(parsed, target);
            if (JsonUtility.ToJson(parsed) != JsonUtility.ToJson(target))
                throw new InvalidOperationException("ZombieData differs from its CSV snapshots. Import all sheets before building.");
        }
        finally { UnityEngine.Object.DestroyImmediate(parsed); }
    }

    public static void WriteReport(string phase, ZombieData data, string result)
    {
        string file = Path.Combine(DataImporter.HistoryFolder, DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fffffff") + "-zombie-sheets-" + phase + ".md");
        string text = "# Zombie sheet import " + phase + "\n\n- Data: data.zombies\n- Canonical: Google Sheets Zombies / ZombieRules, see Docs/DataSpecs/Zombies.md\n" +
            "- Generated asset: " + ZombieSetup.DataPath + "\n- GUID: " + AssetDatabase.AssetPathToGUID(ZombieSetup.DataPath) +
            "\n- Address: " + ZombieData.Address + "\n- Group: Game Data Local; labels: none\n- Consumer: GameData.LoadSettingsAsync -> ZombieInvasionController / ZombieAgent / ZombieCombat\n" +
            "- Result: " + result + "\n\n```json\n" + JsonUtility.ToJson(data, true) + "\n```\n";
        using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false))) writer.Write(text);
    }
}

[CustomEditor(typeof(ZombieData))]
public sealed class ZombieDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Zombies / ZombieRules 시트에서 편집하고 Tools > Data Manager > 전체 시트 가져오기를 실행하세요. 이 SO는 생성물입니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(true)) DrawDefaultInspector();
    }
}
