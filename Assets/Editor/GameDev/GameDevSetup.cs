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
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPath)) UpdatePrefab(CardPath, MakeRoomForClock);
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
    const string WaveMark = "Assets/Art/UI/ShelterClockInvasion.png";
    const string PresentationMark = "Assets/Art/UI/ShelterClockPresentation.png";
    const string LockSprite = "Assets/Art/UI/ShelterWorkLock.png";
    const string MarkerPrefabPath = "Assets/Prefabs/UI/ShelterClockMarker.prefab";
    const int ClockDiameter = 176;
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
        var lockImporter = (TextureImporter)AssetImporter.GetAtPath(LockSprite);
        ImportClockSprite(LockSprite);
        if (lockImporter.filterMode != FilterMode.Point)
        {
            lockImporter.filterMode = FilterMode.Point;
            lockImporter.SaveAndReimport();
        }
        ImportClockSprite(ClockDial);
        ImportClockSprite(ClockArc);
        ImportClockSprite(WaveMark);
        ImportClockSprite(PresentationMark);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        EnsureMarkerPrefab();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
        if (!font) throw new InvalidOperationException("The shared Korean TMP font is missing.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
        {
            UpdatePrefab(PrefabPath, ConfigureDevelopmentHud);
            return;
        }
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

    static void EnsureMarkerPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPrefabPath)) return;
        var marker = new GameObject("ShelterClockMarker", typeof(RectTransform), typeof(Image));
        try
        {
            var image = marker.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WaveMark);
            if (!image.sprite) throw new InvalidOperationException("Missing clock marker Sprite.");
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(16, 16);
            PrefabUtility.SaveAsPrefabAsset(marker, MarkerPrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(marker); }
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
        var viewport = root.transform.Find("Viewport 1920x1080");
        if (!ui.FindProperty("clockFill").objectReferenceValue) ConfigureDevelopmentPanel(ui, viewport);
        var fields = ui.FindProperty("fields");
        bool addedLock = false;
        for (int i = 0; i < fields.arraySize; i++)
        {
            var entry = fields.GetArrayElementAtIndex(i);
            var field = (GameDevField)entry.FindPropertyRelative("field").enumValueIndex;
            var hint = entry.FindPropertyRelative("lockHint");
            if (field == GameDevField.Planning || hint.objectReferenceValue) continue;
            var percent = (TMP_Text)entry.FindPropertyRelative("percentage").objectReferenceValue;
            var card = percent.transform.parent;
            var padlock = Picture("Work lock", card, 134, -12, 20, 20, LockSprite);
            padlock.type = Image.Type.Simple;
            var tip = Picture("Work lock tooltip", card, 0, -72, 402, 42, ArtRoot + "panel.png");
            var caption = Text("Hint", tip.transform, 12, -2, 378, 38, 18);
            caption.font = percent.font;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            tip.gameObject.SetActive(false);
            padlock.gameObject.SetActive(false);
            var component = card.gameObject.AddComponent<DevelopmentLockHint>();
            Set(component, "padlock", padlock.gameObject);
            Set(component, "tooltip", tip.gameObject);
            Set(component, "caption", caption);
            hint.objectReferenceValue = component;
            addedLock = true;
        }
        if (ui.FindProperty("clockMarkerPrefab").objectReferenceValue && ui.FindProperty("presentationMarkerSprite").objectReferenceValue)
        {
            ui.ApplyModifiedPropertiesWithoutUndo();
            return addedLock;
        }
        var dial = Picture("Shelter clock", viewport, 24, -20, ClockDiameter, ClockDiameter, ClockDial);
        dial.type = Image.Type.Simple;
        var arc = Picture("Elapsed day", dial.transform, 0, 0, ClockDiameter, ClockDiameter, ClockArc);
        StretchToParent(arc.rectTransform);
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = true;
        arc.fillAmount = 0;
        var time = Text("Time", dial.transform, 0, 0, ClockDiameter, ClockDiameter, 26);
        StretchToParent(time.rectTransform);
        time.alignment = TextAlignmentOptions.Center;
        time.text = "00:00";
        var legend = Picture("Event legend", dial.transform, ClockDiameter + 12, -50, 136, 76, ArtRoot + "panel.png");
        LegendEntry(legend.transform, "Invasion", "좀비 습격", WaveMark, -8);
        LegendEntry(legend.transform, "Presentation", "중간 발표", PresentationMark, -40);
        ui.FindProperty("clockFill").objectReferenceValue = arc;
        ui.FindProperty("clockTime").objectReferenceValue = time;
        ui.FindProperty("clockMarkerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MarkerPrefabPath).GetComponent<Image>();
        ui.FindProperty("presentationMarkerSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PresentationMark);
        ui.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void LegendEntry(Transform legend, string kind, string title, string sprite, float y)
    {
        Picture(kind + " mark", legend, 12, y - 6, 16, 16, sprite).type = Image.Type.Simple;
        var label = Text(kind + " label", legend, 36, y, 96, 28, 18);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = title;
    }

    static void ConfigureDevelopmentPanel(SerializedObject ui, Transform viewport)
    {
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
        if (card.anchoredPosition != new Vector2(24, -32) && card.anchoredPosition != new Vector2(24, -136)) return false;
        card.anchoredPosition = new Vector2(24, -(20 + ClockDiameter + 16));
        return true;
    }

    static bool ConfigureMentalGauge(GameObject root)
    {
        if (!root.TryGetComponent<CharacterIdentity>(out var identity)) identity = root.AddComponent<CharacterIdentity>();
        var data = new SerializedObject(identity);
        if (data.FindProperty("mentalFill").objectReferenceValue) return false;
        var label = root.GetComponent<ShelterWalker>().nameLabel;
        var rect = Rect("Mental gauge", root.transform, 0, 0, 104, 14);
        if (!rect.TryGetComponent<Canvas>(out var canvas)) canvas = rect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(104, 14);
        rect.localScale = Vector3.one * 0.01f;
        rect.localPosition = new Vector3(0, 2.22f, 0);
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
        if (!rect.TryGetComponent<Image>(out var image)) image = rect.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (!image.sprite) throw new InvalidOperationException("Missing UI Sprite at " + spritePath);
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(string name, Transform parent, float x, float y, float width, float height, float fontSize)
    {
        var rect = Rect(name, parent, x, y, width, height);
        if (!rect.TryGetComponent<TextMeshProUGUI>(out var label)) label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        // TextMeshPro installs its default RectTransform size when first added.
        rect.sizeDelta = new Vector2(width, height);
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
