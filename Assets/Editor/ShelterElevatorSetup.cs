using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class ShelterElevatorSetup
{
    public const string RulesPath = "Assets/Settings/Shelter/ElevatorRules.asset";
    public const string PrefabPath = "Assets/Prefabs/Shelter/Elevator.prefab";
    static Sprite Block => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shelter/Block.png");
    static Material Material => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Shelter/Blockout.mat");
    [MenuItem("Tools/Shelter/Install Elevators")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before installing elevators.");
        var rules = AssetDatabase.LoadAssetAtPath<ElevatorRules>(RulesPath);
        if (!rules)
        {
            rules = ScriptableObject.CreateInstance<ElevatorRules>();
            JsonUtility.FromJsonOverwrite("{\"travelSpeed\":3,\"queueSpacing\":2.4,\"boardingOffset\":2.4,\"exitOffset\":1.2}", rules);
            rules.Validate(); AssetDatabase.CreateAsset(rules, RulesPath);
        }
        rules.Validate();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var guid = AssetDatabase.AssetPathToGUID(RulesPath);
        if (settings.groups.Where(g => g).SelectMany(g => g.entries).Any(e => e.address == ElevatorRules.Address && e.guid != guid)) throw new InvalidOperationException("Duplicate elevator address.");
        settings.CreateOrMoveEntry(guid, settings.FindGroup("Game Data Local")).SetAddress(ElevatorRules.Address);
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
        {
            var root = BuildLift();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); UnityEngine.Object.DestroyImmediate(root);
        }
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save existing scene edits before elevator installation.");
            SceneManager.SetActiveScene(scene);
            var nav = Find<ShelterNavigation>(scene).Single();
            var sim = Find<BunkerSimulation>(scene).Single();
            var transit = nav.GetComponent<ShelterTransit>() ?? nav.gameObject.AddComponent<ShelterTransit>();
            transit.navigation = nav; transit.simulation = sim; transit.development = Find<GameDevManager>(scene).Single(); transit.commander = Find<ShelterCommander>(scene).Single();
            foreach (var old in Find<ShelterLadder>(scene).Where(l => l.isActiveAndEnabled).ToArray())
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                root.name = "Elevator - " + old.name; root.transform.position = old.Bottom;
                var lift = root.GetComponent<ShelterElevator>(); lift.height = old.Top.y - old.Bottom.y; lift.accessible = old.accessible;
                AddLanding(scene, lift, old.Bottom.y, rules); AddLanding(scene, lift, old.Top.y, rules);
                old.gameObject.SetActive(false);
                lift.RefreshVisuals(); EditorUtility.SetDirty(lift);
            }
            foreach (var text in Find<TextMesh>(scene))
                if (text.text == "6 ROOMS / 2 LADDERS") { text.text = "6 ROOMS / 2 ELEVATORS"; EditorUtility.SetDirty(text); }
            nav.Refresh(); foreach (var lift in Find<ShelterElevator>(scene)) lift.Configure(rules, sim, nav); nav.Refresh();
            EditorUtility.SetDirty(transit);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
    static GameObject BuildLift()
    {
        var root = new GameObject("Elevator"); var lift = root.AddComponent<ShelterElevator>();
        var shaft = new GameObject("Open shaft").transform; shaft.SetParent(root.transform, false); lift.shaftVisual = shaft;
        Rect("Shaft tint", shaft, new Vector2(0, 0), new Vector2(2.2f, 1), new Color(0.12f, 0.2f, 0.22f, 0.32f), 4);
        Rect("Left rail", shaft, new Vector2(-1.08f, 0), new Vector2(0.055f, 1), new Color(0.34f, 0.46f, 0.47f), 12);
        Rect("Right rail", shaft, new Vector2(1.08f, 0), new Vector2(0.055f, 1), new Color(0.34f, 0.46f, 0.47f), 12);
        var car = new GameObject("Visible cabin").transform; car.SetParent(root.transform, false); lift.cabin = car;
        Rect("Cabin glass", car, new Vector2(0, 1.2f), new Vector2(2.1f, 2.4f), new Color(0.21f, 0.36f, 0.38f, 0.32f), 17);
        Rect("Platform", car, new Vector2(0, -0.08f), new Vector2(2.2f, 0.16f), new Color(0.63f, 0.76f, 0.73f), 33);
        Rect("Roof", car, new Vector2(0, 2.47f), new Vector2(2.2f, 0.08f), new Color(0.63f, 0.76f, 0.73f), 33);
        Rect("Left frame", car, new Vector2(-1.06f, 1.2f), new Vector2(0.07f, 2.5f), new Color(0.63f, 0.76f, 0.73f), 33);
        Rect("Right frame", car, new Vector2(1.06f, 1.2f), new Vector2(0.07f, 2.5f), new Color(0.63f, 0.76f, 0.73f), 33);
        var label = new GameObject("Elevator ownership").AddComponent<TextMesh>(); label.transform.SetParent(root.transform, false);
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf"); label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.fontSize = 48; label.characterSize = 0.05f; label.color = new Color(0.65f, 0.88f, 0.86f);
        label.GetComponent<MeshRenderer>().sortingOrder = 50; lift.statusLabel = label; lift.RefreshVisuals(); return root;
    }
    static void AddLanding(Scene scene, ShelterElevator lift, float y, ElevatorRules rules)
    {
        var root = new GameObject("Elevator landing " + y); SceneManager.MoveGameObjectToScene(root, scene); root.transform.position = new Vector3(lift.Bottom.x, y);
        var area = root.AddComponent<ShelterArea>(); area.kind = ShelterArea.AreaKind.Corridor; area.displayName = "엘리베이터 승강장";
        area.width = rules.BoardingOffset * 2 + 1.2f; area.height = 3.1f;
        area.floorVisual = Rect("Landing floor", root.transform, Vector2.zero, Vector2.one, new Color(0.2f, 0.29f, 0.3f), 14).transform;
        area.RefreshVisuals();
    }
    static SpriteRenderer Rect(string name, Transform parent, Vector2 point, Vector2 size, Color color, int order)
    {
        var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = point; root.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = root.AddComponent<SpriteRenderer>(); r.sprite = Block; r.sharedMaterial = Material; r.color = color; r.sortingOrder = order; return r;
    }
    [MenuItem("Tools/Shelter/Build Elevator Data")]
    public static void BuildContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        settings.ActivePlayModeDataBuilderIndex = settings.DataBuilders.FindIndex(b => b is UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedPlayMode);
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Elevators");
        File.WriteAllText("Temp/Elevators/content-build.txt", $"Profile {settings.profileSettings.GetProfileName(settings.activeProfileId)}\n{result.OutputPath}\n{result.Duration}\nGUID {AssetDatabase.AssetPathToGUID(RulesPath)}\n" + JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<ElevatorRules>(RulesPath)));
    }
    static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
}
