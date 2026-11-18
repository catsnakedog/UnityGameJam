using System;
using System.IO;
using System.Linq;
using GameJam.Data.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.GameDev;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

public static class GameDevSetup
{
    public const string PrefabPath = "Assets/Prefabs/UI/DevGageUI.prefab";
    const string ArtRoot = "Assets/Art/UI/Farming/";
    static TMP_FontAsset font;

    [MenuItem("Tools/Game Dev/Import Ending CSV")]
    public static void ImportEndings()
    {
        var plan = DataImporter.Preview(DataToolSettings.instance.csvFolder);
        GameDevEndingCatalog.FromTable(plan.Tables.Single(t => t.Name == GameDevKeys.EndingsTable));
        DataImporter.Apply(plan);
    }

    [MenuItem("Tools/Game Dev/Install Shelter Development UI")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before authoring.");
        EnsurePrefab();
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save the shelter scene before installing its development UI.");
            SceneManager.SetActiveScene(scene);
            var managers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameDevManager>(true)).ToArray();
            if (managers.Length > 1) throw new InvalidOperationException("Multiple GameDevManagers in shelter scene.");
            var manager = managers.SingleOrDefault() ?? new GameObject("GameDevManager").AddComponent<GameDevManager>();
            var uis = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DevGageUI>(true)).ToArray();
            if (uis.Length > 1) throw new InvalidOperationException("Multiple DevGageUI instances in shelter scene.");
            var ui = uis.SingleOrDefault();
            if (!ui) ui = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene)).GetComponent<DevGageUI>();
            Set(ui, "manager", manager);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            if (!scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Any())
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FirstOrDefault(s => s.path == GameDevKeys.ShelterScene);
            if (existing == null) scenes.Add(new EditorBuildSettingsScene(GameDevKeys.ShelterScene, true));
            else existing.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log("Saved GameDevManager and DevGageUI in " + GameDevKeys.ShelterScene);
    }

    const string SurvivorPath = "Assets/Prefabs/Shelter/Survivor.prefab";
    const string CardPath = "Assets/Prefabs/UI/ShelterCharacterCard.prefab";
    const string ClockDial = "Assets/Art/UI/ShelterClockDial.png";
    const string ClockArc = "Assets/Art/UI/ShelterClockElapsed.png";
    static readonly Vector2 TopLeft = new Vector2(0, 1);

    [MenuItem("Tools/Game Dev/Update Shelter HUD")]
    public static void UpdateShelterHud()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring the HUD.");
        EnsurePrefab();
        UpdatePrefab(SurvivorPath, ConfigureMentalGauge);
        UpdatePrefab(CardPath, MakeRoomForClock);
    }

    static void EnsurePrefab()
    {
        ImportClockSprite(ClockDial);
        ImportClockSprite(ClockArc);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
        if (!font) throw new InvalidOperationException("The shared Korean TMP font is missing.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
        {
            UpdatePrefab(PrefabPath, ConfigureDevelopmentHud);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        var root = new GameObject("DevGageUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DevGageUI));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            FixedAspectSetup.ConfigureCanvas(canvas);
            ConfigureDevelopmentHud(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ImportClockSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new FileNotFoundException("Missing shelter clock artwork.", path);
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
            !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    static void UpdatePrefab(string path, Func<GameObject, bool> configure)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (configure(root)) PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static bool ConfigureDevelopmentHud(GameObject root)
    {
        var ui = new SerializedObject(root.GetComponent<DevGageUI>());
        if (ui.FindProperty("clockFill").objectReferenceValue) return false;
        var viewport = root.transform.Find("Viewport 1920x1080");
        var panel = Rect("Development panel", viewport, 486, -24, 748, 68);
        var oldBackground = panel.GetComponent<Image>();
        if (oldBackground) oldBackground.enabled = false;
        foreach (var oldName in new[] { "Title", "Subtitle" })
        {
            var oldLabel = panel.Find(oldName);
            if (oldLabel) oldLabel.gameObject.SetActive(false);
        }

        var fields = ui.FindProperty("fields");
        fields.arraySize = 3;
        Field(fields, 0, panel, GameDevField.Planning, "Planning", "기획");
        Field(fields, 1, panel, GameDevField.Programming, "Programming", "프로그래밍");
        Field(fields, 2, panel, GameDevField.Graphics, "Graphics", "그래픽");
        var status = Text("Status", panel, 0, -74, 748, 64, 18);
        status.text = "개발 준비 중";
        status.alignment = TextAlignmentOptions.Top;
        status.textWrappingMode = TextWrappingModes.Normal;
        ui.FindProperty("status").objectReferenceValue = status;

        var dial = Picture("Shelter clock", viewport, 24, -20, 100, 100, ClockDial);
        dial.type = Image.Type.Simple;
        var arc = Picture("Elapsed day", dial.transform, 0, 0, 100, 100, ClockArc);
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = true;
        arc.fillAmount = 0;
        var time = Text("Time", dial.transform, 0, 0, 100, 100, 20);
        time.alignment = TextAlignmentOptions.Center;
        time.text = "00:00";
        ui.FindProperty("clockFill").objectReferenceValue = arc;
        ui.FindProperty("clockTime").objectReferenceValue = time;
        ui.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static void Field(SerializedProperty fields, int index, Transform panel, GameDevField field, string name, string title)
    {
        var card = Picture(name, panel, index * 254, 0, 240, 68, ArtRoot + "panel.png");
        card.raycastTarget = true;
        Text("Label", card.transform, 14, -8, 136, 30, 22).text = title;
        var percent = Text("Percentage", card.transform, 148, -8, 78, 30, 22);
        percent.alignment = TextAlignmentOptions.TopRight;
        percent.text = "0%";
        var track = Picture("Track", card.transform, 14, -43, 212, 13, ArtRoot + "panel.png");
        var fill = Picture("Fill", track.transform, 2, -2, 208, 9, ArtRoot + "gauge.png");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0;
        var entry = fields.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("field").enumValueIndex = (int)field;
        entry.FindPropertyRelative("fill").objectReferenceValue = fill;
        entry.FindPropertyRelative("percentage").objectReferenceValue = percent;
    }

    static bool MakeRoomForClock(GameObject root)
    {
        var card = (RectTransform)root.transform.Find("Viewport 1920x1080/Character card");
        if (card.anchoredPosition != new Vector2(24, -32)) return false;
        card.anchoredPosition = new Vector2(24, -136);
        return true;
    }

    static bool ConfigureMentalGauge(GameObject root)
    {
        var identity = root.GetComponent<CharacterIdentity>() ?? root.AddComponent<CharacterIdentity>();
        var data = new SerializedObject(identity);
        if (data.FindProperty("mentalFill").objectReferenceValue) return false;
        var label = root.GetComponent<ShelterWalker>().nameLabel;
        var rect = Rect("Mental gauge", root.transform, 0, 0, 104, 14);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one * 0.01f;
        rect.localPosition = new Vector3(0, 2.22f, 0);
        var canvas = rect.gameObject.GetComponent<Canvas>() ?? rect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = label.GetComponent<MeshRenderer>().sortingOrder + 1;
        var track = Picture("Track", rect, 0, 0, 104, 14, ArtRoot + "panel.png");
        var fill = Picture("Fill", track.transform, 2, -2, 100, 10, ArtRoot + "gauge.png");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1;
        data.FindProperty("mentalFill").objectReferenceValue = fill;
        data.ApplyModifiedPropertiesWithoutUndo();
        label.transform.localPosition = new Vector3(0, 2.34f, 0);
        return true;
    }

    static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = (RectTransform)parent.Find(name);
        if (!rect)
        {
            rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }
        rect.anchorMin = rect.anchorMax = rect.pivot = TopLeft;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    static Image Picture(string name, Transform parent, float x, float y, float width, float height, string spritePath)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (!image.sprite) throw new InvalidOperationException("Missing UI Sprite at " + spritePath);
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(string name, Transform parent, float x, float y, float width, float height, float fontSize)
    {
        var rect = Rect(name, parent, x, y, width, height);
        var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.color = new Color32(244, 235, 217, 255);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
