using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class BunkerSetup
{
    public const string RulesPath = "Assets/Settings/Shelter/BunkerRules.asset";
    // Seed fields when installing a new asset or upgrading one that predates presentations.
    // Subsequent balancing happens on the saved BunkerRules asset.
    public static void InitializeEventSchedule(SerializedObject data)
    {
        var first = data.FindProperty("firstInvasionTick");
        if (first.intValue == 0) first.intValue = 20;
        var grades = data.FindProperty("presentationGrades");
        if (grades.arraySize != 0) return;
        var ticks = data.FindProperty("presentationTicks");
        if (ticks.arraySize == 0)
        {
            ticks.arraySize = 3;
            for (int i = 0; i < ticks.arraySize; i++)
                ticks.GetArrayElementAtIndex(i).intValue = (i + 1) * 240;
        }
        grades.arraySize = 4;
        void Grade(int index, double minimum, string label, int mental, Color32 color)
        {
            var grade = grades.GetArrayElementAtIndex(index);
            grade.FindPropertyRelative("minimumScore").doubleValue = minimum;
            grade.FindPropertyRelative("label").stringValue = label;
            grade.FindPropertyRelative("mentalDelta").intValue = mental;
            grade.FindPropertyRelative("color").colorValue = color;
        }
        Grade(0, 0, "망겜", -15, new Color32(248, 113, 113, 255));
        Grade(1, .7, "흠", 0, new Color32(175, 184, 183, 255));
        Grade(2, 1, "굿", 10, new Color32(112, 218, 153, 255));
        Grade(3, 1.2, "갓겜", 20, new Color32(255, 209, 91, 255));
    }
    [MenuItem("Tools/Bunker/Install Tick Simulation")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
        var rules = AssetDatabase.LoadAssetAtPath<BunkerRules>(RulesPath);
        if (!rules)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RulesPath));
            AssetDatabase.Refresh();
            rules = ScriptableObject.CreateInstance<BunkerRules>();
            var data = new SerializedObject(rules);
            data.FindProperty("totalTicks").intValue = 960;
            data.FindProperty("secondsPerTick").floatValue = 1;
            data.FindProperty("randomEventEveryTicks").intValue = 40;
            data.FindProperty("invasionEveryTicks").intValue = 80;
            InitializeEventSchedule(data);
            data.FindProperty("invasionApproachTicks").intValue = 6;
            data.FindProperty("doorHitLimit").intValue = 3;
            data.FindProperty("guardVolleyEveryTicks").intValue = 2;
            data.FindProperty("invasionMentalLoss").intValue = 2;
            data.FindProperty("guardMentalLossPerTick").intValue = 2;
            data.FindProperty("workEveryTicks").intValue = 1;
            data.FindProperty("percentPerAbility").floatValue = 0.1f;
            data.FindProperty("mentalCostPerWork").intValue = 1;
            data.FindProperty("planningUnlockPercent").floatValue = 50;
            data.FindProperty("mentalMaximum").intValue = 100;
            data.FindProperty("tiredMentalThreshold").intValue = 50;
            data.FindProperty("hungerEveryTicks").intValue = 5;
            data.FindProperty("hungerLoss").intValue = 1;
            data.FindProperty("foodRestore").intValue = 5;
            data.FindProperty("servingsPerCan").intValue = 4;
            data.FindProperty("sleepMentalPerTick").intValue = 5;
            data.FindProperty("sleepingRoomCapacity").intValue = 2;
            data.FindProperty("wakeMental").intValue = 50;
            // Initial authoring only; the existing SO remains the editable source of truth.
            data.FindProperty("evaluationTargetProgress").doubleValue = 96;
            data.FindProperty("evaluationReferenceTicks").intValue = 24;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(rules, RulesPath);
        }
        UpdateRoomAndEventRules(rules);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("Game Data Local");
        if (!group) throw new InvalidOperationException("Existing Game Data Local group missing.");
        var guid = AssetDatabase.AssetPathToGUID(RulesPath);
        if (settings.groups.Where(g => g).SelectMany(g => g.entries).Any(e => e.address == BunkerRules.Address && e.guid != guid))
            throw new InvalidOperationException("Duplicate bunker rules address.");
        settings.CreateOrMoveEntry(guid, group).SetAddress(BunkerRules.Address);
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Shelter scene has unsaved edits.");
            var roots = scene.GetRootGameObjects();
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<GameDevManager>(true)).Single();
            var roster = roots.SelectMany(r => r.GetComponentsInChildren<ShelterRoster>(true)).Single();
            var simulation = manager.GetComponent<BunkerSimulation>();
            bool firstInstall = !simulation;
            if (!simulation) simulation = manager.gameObject.AddComponent<BunkerSimulation>();
            var so = new SerializedObject(simulation);
            so.FindProperty("development").objectReferenceValue = manager;
            so.FindProperty("roster").objectReferenceValue = roster;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (firstInstall)
            {
                // Initial sample placement only; subsequent installs preserve designer choices.
                var rooms = roots.SelectMany(r => r.GetComponentsInChildren<ShelterArea>(true))
                    .Where(a => a.kind == ShelterArea.AreaKind.Room && a.displayName != "ENTRY")
                    .OrderBy(a => a.FloorY).ThenBy(a => a.Left).Take(3).ToArray();
                if (rooms.Length != 3) throw new InvalidOperationException("Three work rooms required for initial placement.");
                rooms[0].roomType = ShelterRoomType.Programming; rooms[0].displayName = "PROGRAMMING";
                rooms[1].roomType = ShelterRoomType.Graphics; rooms[1].displayName = "GRAPHICS";
                rooms[2].roomType = ShelterRoomType.Planning; rooms[2].displayName = "PLANNING";
                foreach (var room in rooms) { room.RefreshVisuals(); EditorUtility.SetDirty(room); }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void UpdateRoomAndEventRules(BunkerRules rules)
    {
        var data = new SerializedObject(rules);
        InitializeEventSchedule(data);
        var capacity = data.FindProperty("sleepingRoomCapacity");
        if (capacity.intValue == 0) capacity.intValue = 2;
        data.ApplyModifiedPropertiesWithoutUndo();
        rules.Validate();
        AssetDatabase.SaveAssetIfDirty(rules);
    }
}
