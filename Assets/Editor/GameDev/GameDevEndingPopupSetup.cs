using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class GameDevEndingPopupSetup
{
    public const string PrefabPath = "Assets/Prefabs/UI/GameDevEndingPopup.prefab";
    const string Art = "Assets/Art/UI/Events/";
    static TMP_FontAsset font;

    [MenuItem("Tools/Game Dev/Install Ending Popup")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save shelter scene changes before install.");
            var manager = Find<GameDevManager>(scene).Single();
            var simulation = Find<BunkerSimulation>(scene).Single();
            var commander = Find<ShelterCommander>(scene).Single();
            if (!Find<EventSystem>(scene).Any()) throw new InvalidOperationException("Shelter EventSystem missing.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
            if (!font) throw new InvalidOperationException("Shared Korean font missing.");
            EnsurePrefab();
            var popup = Find<GameDevEndingPopup>(scene).SingleOrDefault();
            if (!popup) popup = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene)).GetComponent<GameDevEndingPopup>();
            Set(popup, "manager", manager); Set(popup, "simulation", simulation); Set(popup, "commander", commander);
            PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
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

    static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

    static void EnsurePrefab()
    {
        // Keep authored placement and artwork on repeated installation.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var root = new GameObject("GameDevEndingPopup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameDevEndingPopup));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            FixedAspectSetup.ConfigureCanvas(canvas);
            var viewport = root.transform.Find("Viewport 1920x1080");
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            Place(overlay.rectTransform, viewport, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1920,1080));
            overlay.color = new Color(0.025f,0.04f,0.035f,0.82f); overlay.raycastTarget = true;
            var frame = Picture("Laptop", overlay.transform, new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1120,760), "laptop_frame");
            var score = Text("Score", "평가 점수", frame.transform, new Vector2(130,-100), new Vector2(860,32), 22);
            score.color = new Color32(167,195,142,255);
            var title = Text("Title", "개발 결과", frame.transform, new Vector2(130,-139), new Vector2(860,55), 34);
            title.enableAutoSizing = true; title.fontSizeMin = 26; title.fontSizeMax = 34;
            var scroll = new GameObject("BodyScroll", typeof(RectTransform), typeof(ScrollRect)).GetComponent<ScrollRect>();
            Place((RectTransform)scroll.transform, frame.transform, new Vector2(0,1), new Vector2(130,-225), new Vector2(860,220));
            var mask = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            Place(mask, scroll.transform, new Vector2(0,1), Vector2.zero, new Vector2(860,220));
            mask.GetComponent<Image>().color = Color.clear;
            var body = Text("Content", "엔딩 제목과 내용은 GameDevEndings에서 표시됩니다.", mask, Vector2.zero, new Vector2(852,220), 28);
            body.lineSpacing = 12;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = mask; scroll.content = body.rectTransform; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var buttonImage = Picture("Confirm", frame.transform, new Vector2(0,1), new Vector2(810,-485), new Vector2(180,56), "button_normal");
            buttonImage.raycastTarget = true;
            var button = buttonImage.gameObject.AddComponent<Button>(); button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = Sprite("button_hover"), pressedSprite = Sprite("button_pressed"), selectedSprite = Sprite("button_hover") };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text("Label", "확인", button.transform, Vector2.zero, new Vector2(180,56), 24).alignment = TextAlignmentOptions.Center;
            Text("Hint", "ENTER / SPACE / ESC · 닫기", frame.transform, new Vector2(130,-501), new Vector2(640,28), 18).color = new Color32(145,164,135,255);
            var popup = root.GetComponent<GameDevEndingPopup>();
            Set(popup,"panel",overlay.gameObject); Set(popup,"title",title); Set(popup,"body",body); Set(popup,"score",score); Set(popup,"confirm",button); Set(popup,"scroll",scroll);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png") ?? throw new InvalidOperationException("Missing Sprite " + name);
    static Image Picture(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string sprite)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Place(image.rectTransform, parent, anchor, pos, size); image.sprite = Sprite(sprite); image.raycastTarget = false; return image;
    }
    static TMP_Text Text(string name, string value, Transform parent, Vector2 pos, Vector2 size, float fontSize)
    {
        var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Place(t.rectTransform,parent,new Vector2(0,1),pos,size); t.font = font; t.text = value; t.fontSize = fontSize;
        t.color = new Color32(222,229,204,255); t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.richText = false; return t;
    }
    static void Place(RectTransform t, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    { t.SetParent(parent,false); t.anchorMin = t.anchorMax = t.pivot = anchor; t.anchoredPosition = pos; t.sizeDelta = size; }
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    { var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
}
