using System;
using System.Linq;
using GameJam.Data;
using GameJam.Data.Editor;
using ShelterRun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

public static class CharacterSetup
{
    [MenuItem("Tools/Characters/Import and Validate")]
    public static void Import()
    {
        var plan = DataImporter.Preview("Assets/Settings/Data/Csv");
        Validate(plan.Tables.First(t => t.Name == CharacterKeys.Table));
        DataImporter.Apply(plan);
    }
    public static void Validate(DataTable table)
    {
        new CharacterCatalog(table).ValidateFarming(AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
    }
    [MenuItem("Tools/Characters/Install Shelter Roster")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play first.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ShelterPrototype.unity");
        var commander = UnityEngine.Object.FindFirstObjectByType<ShelterCommander>();
        var roster = commander.GetComponent<ShelterRoster>();
        if (!roster) roster = commander.gameObject.AddComponent<ShelterRoster>();
        foreach (var walker in UnityEngine.Object.FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None))
            walker.gameObject.SetActive(false); // Preserve authored examples for editing, never spawn them in gameplay.
        var serialized = new SerializedObject(roster);
        serialized.FindProperty("survivorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shelter/Survivor.prefab").GetComponent<ShelterWalker>();
        var points = serialized.FindProperty("spawnPoints");
        if (points.arraySize == 0)
        {
            var rooms = UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None);
            var entry = rooms.FirstOrDefault(a => a.displayName == "ENTRY")
                ?? rooms.Single(a => a.kind == ShelterArea.AreaKind.Room && a.roomType == ShelterRoomType.Common);
            var db = AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset");
            int count = db.Records.Count(r => r.Type == FarmingObjectType.Person);
            points.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var point = new GameObject("Survivor spawn " + (i + 1)).transform;
                point.SetParent(roster.transform);
                point.position = new Vector3(Mathf.Lerp(entry.Left, entry.Right, (i + 1f) / (count + 1f)), entry.FloorY, 0);
                points.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
