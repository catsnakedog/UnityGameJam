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
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
        if (!font) throw new InvalidOperationException("The shared Korean TMP font is missing.");
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

    static void EnsurePrefab()
    {
        // Existing authored layouts and Sprite assignments belong to the user.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        var root = new GameObject("DevGageUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DevGageUI));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            FixedAspectSetup.ConfigureCanvas(canvas);
            var viewport = root.transform.Find("Viewport 1920x1080");
            var panel = Picture("Development panel", viewport, new Vector2(1, 1), new Vector2(-32, -160), new Vector2(320, 430), "panel");
            panel.raycastTarget = true;
            Text("Title", "게임 개발", panel.transform, new Vector2(24, -24), new Vector2(272, 40), 30);
            Text("Subtitle", "DEVELOPMENT PROGRESS", panel.transform, new Vector2(24, -68), new Vector2(272, 22), 14).color = new Color32(186, 169, 136, 255);
            var ui = root.GetComponent<DevGageUI>();
            var serialized = new SerializedObject(ui);
            var fields = serialized.FindProperty("fields");
            fields.arraySize = 3;
            Field(fields.GetArrayElementAtIndex(0), panel.transform, GameDevField.Planning, "Planning", "기획", 112);
            Field(fields.GetArrayElementAtIndex(1), panel.transform, GameDevField.Programming, "Programming", "프로그래밍", 184);
            Field(fields.GetArrayElementAtIndex(2), panel.transform, GameDevField.Graphics, "Graphics", "그래픽", 256);
            var status = Text("Status", "세 분야를 완성해 게임을 개발하세요.", panel.transform, new Vector2(24, -334), new Vector2(272, 78), 17);
            status.textWrappingMode = TextWrappingModes.Normal;
            serialized.FindProperty("status").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Field(SerializedProperty entry, Transform panel, GameDevField field, string name, string title, float y)
    {
        var row = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        Position(row, panel, new Vector2(0, 1), new Vector2(24, -y), new Vector2(272, 60));
        Text("Label", title, row, Vector2.zero, new Vector2(190, 28), 23);
        var percentage = Text("Percentage", "0%", row, new Vector2(190, 0), new Vector2(82, 28), 23);
        percentage.alignment = TextAlignmentOptions.TopRight;
        var track = Picture("Track", row, new Vector2(0, 1), new Vector2(0, -38), new Vector2(272, 16), "panel");
        var fill = Picture("Fill", track.transform, new Vector2(0, 1), new Vector2(2, -2), new Vector2(268, 12), "gauge");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0;
        entry.FindPropertyRelative("field").enumValueIndex = (int)field;
        entry.FindPropertyRelative("fill").objectReferenceValue = fill;
        entry.FindPropertyRelative("percentage").objectReferenceValue = percentage;
    }

    static void Position(RectTransform rect, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static Image Picture(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string sprite)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Position(image.rectTransform, parent, anchor, position, size);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + sprite + ".png");
        if (!image.sprite) throw new InvalidOperationException("Missing shared UI Sprite: " + sprite);
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(string name, string text, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Position(label.rectTransform, parent, new Vector2(0, 1), position, size);
        label.font = font;
        label.text = text;
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
