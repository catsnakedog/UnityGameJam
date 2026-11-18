using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Characters;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ZombieSetup
{
    public const string DataPath = "Assets/Settings/Shelter/ZombieData.asset";
    public const string PrefabPath = "Assets/Prefabs/Shelter/Zombie.prefab";
    [MenuItem("Tools/Zombies/Install Invasion")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var data = AssetDatabase.LoadAssetAtPath<ZombieData>(DataPath);
        if (!data)
        {
            data = ZombieCsvImporter.Parse(File.ReadAllText(ZombieCsvImporter.CatalogPath), File.ReadAllText(ZombieCsvImporter.RulesPath));
            data.name = "ZombieData";
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.Validate();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("Game Data Local");
        var guid = AssetDatabase.AssetPathToGUID(DataPath);
        if (settings.groups.Where(g => g).SelectMany(g => g.entries).Any(e => e.address == ZombieData.Address && e.guid != guid))
            throw new InvalidOperationException("Duplicate data/zombies address.");
        settings.CreateOrMoveEntry(guid, group).SetAddress(ZombieData.Address);
        var bunker = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        var so = new SerializedObject(bunker);
        // Only the pre-feature schema (all fields absent/zero) is migrated. Subsequent designer values are preserved.
        if (so.FindProperty("mentalMaximum").intValue == 0 && so.FindProperty("faintDurationTicks").intValue == 0 && so.FindProperty("wakeMental").intValue == 0)
        {
            so.FindProperty("mentalMaximum").intValue = 100;
            so.FindProperty("faintDurationTicks").intValue = 40;
            so.FindProperty("wakeMental").intValue = 30;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        bunker.Validate();
        EnsurePrefab();
        EnsureSurvivorLabel();
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save shelter scene edits before installation.");
            SceneManager.SetActiveScene(scene);
            var simulation = Find<BunkerSimulation>(scene).Single();
            var ctrl = simulation.GetComponent<ZombieInvasionController>() ?? simulation.gameObject.AddComponent<ZombieInvasionController>();
            Set(ctrl, "simulation", simulation); Set(ctrl, "development", Find<GameDevManager>(scene).Single());
            Set(ctrl, "roster", Find<ShelterRoster>(scene).Single()); Set(ctrl, "navigation", Find<ShelterNavigation>(scene).Single());
            Set(ctrl, "zombiePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<ZombieAgent>());
            var status = Find<TextMesh>(scene).FirstOrDefault(t => t.name == "Zombie event status");
            if (!status)
            {
                var template = Find<ShelterArea>(scene).First(a => a.isActiveAndEnabled && a.nameLabel).nameLabel;
                status = UnityEngine.Object.Instantiate(template, ctrl.transform);
                status.name = "Zombie event status"; status.transform.position = new Vector3(-16.5f, 15.7f, -0.5f);
                status.anchor = TextAnchor.UpperLeft; status.alignment = TextAlignment.Left;
                status.characterSize = 0.065f; status.fontSize = 48; status.color = new Color(0.92f, 0.55f, 0.43f);
                status.text = "좀비 침입 대기";
            }
            Set(ctrl, "statusLabel", status);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
    static void EnsurePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shelter/Survivor.prefab");
        var instance = UnityEngine.Object.Instantiate(source); instance.name = "Zombie";
        try
        {
            var identity = instance.GetComponent<CharacterIdentity>(); if (identity) UnityEngine.Object.DestroyImmediate(identity);
            var walker = instance.GetComponent<ShelterWalker>(); walker.playerSelectable = false; walker.externallyDriven = true;
            walker.characterName = "좀비"; walker.walkSpeed = walker.climbSpeed = 0; // Runtime catalog provides speeds.
            if (walker.selectionMarker) walker.selectionMarker.enabled = false;
            foreach (var sprite in instance.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sprite.name == "Head" || sprite.name == "Neck" || sprite.name == "Nose") sprite.color = new Color(0.43f, 0.59f, 0.37f);
                if (sprite.name.Contains("Coat") || sprite.name.Contains("arm")) sprite.color = new Color(0.38f, 0.27f, 0.23f);
                if (sprite.name == "Backpack") sprite.gameObject.SetActive(false);
            }
            var agent = instance.AddComponent<ZombieAgent>();
            var label = walker.nameLabel; walker.nameLabel = null;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            if (!font) throw new InvalidOperationException("Shared Korean font missing.");
            label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.fontSize = 48; label.characterSize = 0.055f; label.color = new Color(1, 0.5f, 0.4f);
            label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center;
            label.transform.localPosition = new Vector3(0, 2.25f, -0.1f); label.text = "좀비";
            Set(agent, "healthLabel", label);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
    static void EnsureSurvivorLabel()
    {
        const string path = "Assets/Prefabs/Shelter/Survivor.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var label = root.GetComponent<ShelterWalker>().nameLabel;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
            if (!label || !font) throw new InvalidOperationException("Survivor label/font missing.");
            label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center;
            label.characterSize = 0.045f; label.fontSize = 48;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    [MenuItem("Tools/Zombies/Build Data Content")]
    public static void BuildContent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before building zombie data.");
        ZombieCsvImporter.ValidateCurrent();
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        int index = settings.DataBuilders.FindIndex(b => b is UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedPlayMode);
        settings.ActivePlayModeDataBuilderIndex = index; EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/ZombieInvasion");
        File.WriteAllText("Temp/ZombieInvasion/content-build.txt", $"Profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}\nOutput: {result.OutputPath}\nDuration: {result.Duration}\nGUID: {AssetDatabase.AssetPathToGUID(DataPath)}\n" + EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ZombieData>(DataPath), true));
    }
    static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
}

[CustomEditor(typeof(ZombieInvasionController))]
public sealed class ZombieInvasionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ctrl = (ZombieInvasionController)target;
        EditorGUILayout.HelpBox(ctrl.LastError ?? ctrl.Message, ctrl.LastError == null ? MessageType.Info : MessageType.Error);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !ctrl.CanAdvance))
            if (GUILayout.Button("좀비 1마리 생성 (Play 테스트)")) ctrl.SpawnWave();
        if (!Application.isPlaying) return;
        foreach (var zombie in ctrl.Zombies) if (zombie)
            EditorGUILayout.LabelField(zombie.Definition.DisplayName, $"{zombie.State} · HP {zombie.Hp}");
    }
}
