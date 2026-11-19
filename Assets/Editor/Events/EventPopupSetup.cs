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
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class EventPopupSetup
{
    public const string PrefabPath = "Assets/Prefabs/UI/EventLaptopPopup.prefab";
    const string Art = "Assets/Art/UI/Events/";
    static TMP_FontAsset font;
    [MenuItem("Tools/Events/Import Event CSV")]
    public static void Import()
    { ImportValidated(false); }
    public static void MigrateChoices()
    { ImportValidated(true); }
    static void ImportValidated(bool allowSchemaChange)
    {
        var plan = DataImporter.Preview(DataToolSettings.instance.csvFolder);
        var items=new ShelterItemCatalog(plan.Tables.Single(t=>t.Name==ShelterItemCatalog.TableName));
        items.ValidateSources(plan.Tables.Single(t => t.Name == EventCatalog.TableName),
            ShelterRun.Editor.FarmingCsvImporter.Parse(File.ReadAllText(ShelterRun.Editor.FarmingCsvImporter.CsvPath)));
        EventCatalog.FromTable(plan.Tables.Single(t => t.Name == EventCatalog.TableName),items);
        if(allowSchemaChange && plan.Changes.Any(c=>(c.StartsWith("Remove",StringComparison.Ordinal)||c.StartsWith("Change key",StringComparison.Ordinal)) &&
            !c.StartsWith("Remove row Events[",StringComparison.Ordinal) && !c.StartsWith("Remove/change column Events.",StringComparison.Ordinal) && !c.StartsWith("Change key Events:",StringComparison.Ordinal)))
            throw new InvalidOperationException("Event migration cannot remove or change another table.");
        DataImporter.Apply(plan,allowSchemaChange);
    }
    [MenuItem("Tools/Events/Install Laptop Popup")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
        if (!font) throw new InvalidOperationException("Shared Korean font missing.");
        EnsurePrefab();
        UpdateChoicesPrefab();
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(GameDevKeys.ShelterScene);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(GameDevKeys.ShelterScene, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save shelter scene changes before install.");
            SceneManager.SetActiveScene(scene);
            var simulation = Find<BunkerSimulation>(scene).Single();
            var manager = Find<GameDevManager>(scene).Single();
            var commander = Find<ShelterCommander>(scene).Single();
            var popup = Find<EventLaptopPopup>(scene).SingleOrDefault();
            if (!popup) popup = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene)).GetComponent<EventLaptopPopup>();
            var controller = simulation.GetComponent<EventPopupController>() ?? simulation.gameObject.AddComponent<EventPopupController>();
            var supplies=simulation.GetComponent<ShelterSupplies>() ?? simulation.gameObject.AddComponent<ShelterSupplies>();
            Set(controller,"supplies",supplies); Set(controller,"roster",Find<ShelterRoster>(scene).Single());
            Set(controller,"simulation",simulation); Set(controller,"development",manager); Set(controller,"commander",commander); Set(controller,"popup",popup);
            if (!Find<EventSystem>(scene).Any()) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene,true);
        }
    }
    static T[] Find<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static void UpdateChoicesPrefab()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var popup=root.GetComponent<EventLaptopPopup>();
            var laptop=root.GetComponentsInChildren<Image>(true).Single(i=>i.name=="Laptop").transform;
            var yes=ChoiceButton(laptop,"Yes",new Vector2(600,-485));
            var no=ChoiceButton(laptop,"No",new Vector2(810,-485));
            var result=laptop.Find("Result")?.GetComponent<TMP_Text>();
            if(!result) result=Text("Result","",laptop,new Vector2(130,-410),new Vector2(860,42),22);
            result.textWrappingMode=TextWrappingModes.NoWrap; result.enableAutoSizing=true; result.fontSizeMin=12; result.fontSizeMax=22;
            var area=laptop.Find("BodyScroll").GetComponent<RectTransform>(); area.sizeDelta=new Vector2(860,165);
            var viewport=area.Find("Viewport").GetComponent<RectTransform>(); viewport.sizeDelta=new Vector2(860,165);
            var hint=laptop.Find("Hint"); if(hint) hint.gameObject.SetActive(false);
            Set(popup,"yesButton",yes); Set(popup,"noButton",no); Set(popup,"resultLabel",result);
            laptop.Find("Confirm").gameObject.SetActive(false);
            LaptopTerminalLayout.Apply((RectTransform)laptop, true);
            PaperUISetup.Review(root);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static Button ChoiceButton(Transform laptop,string name,Vector2 position)
    {
        var existing=laptop.Find(name)?.GetComponent<Button>(); if(existing) return existing;
        var image=Picture(name,laptop,new Vector2(0,1),position,new Vector2(180,56),"button_normal");
        image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.SpriteSwap;
        button.spriteState=new SpriteState {highlightedSprite=Sprite("button_hover"),pressedSprite=Sprite("button_pressed"),selectedSprite=Sprite("button_hover")};
        button.navigation=new Navigation{mode=Navigation.Mode.None};
        var label=Text("Label",name,button.transform,Vector2.zero,new Vector2(180,56),24); label.alignment=TextAlignmentOptions.Center;
        return button;
    }
    static void EnsurePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var root = new GameObject("EventLaptopPopup",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(EventLaptopPopup));
        try
        {
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            FixedAspectSetup.ConfigureCanvas(canvas);
            var viewport = root.transform.Find("Viewport 1920x1080");
            var overlay = new GameObject("Overlay",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            Place(overlay.rectTransform,viewport,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(1920,1080));
            overlay.color=new Color(0.025f,0.04f,0.035f,0.82f); overlay.raycastTarget=true;
            var laptop=Picture("Laptop",overlay.transform,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(1120,760),"laptop_frame");
            var number=Text("Number","EVENT",laptop.transform,new Vector2(130,-100),new Vector2(700,32),22);
            number.color=new Color32(167,195,142,255);
            var title=Text("Title","사건 기록",laptop.transform,new Vector2(130,-139),new Vector2(860,55),34);
            title.enableAutoSizing=true; title.fontSizeMin=26; title.fontSizeMax=34;
            var bodyArea=new GameObject("BodyScroll",typeof(RectTransform),typeof(ScrollRect)).GetComponent<ScrollRect>();
            Place(bodyArea.GetComponent<RectTransform>(),laptop.transform,new Vector2(0,1),new Vector2(130,-225),new Vector2(860,220));
            var mask=new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D),typeof(Image)).GetComponent<RectTransform>();
            Place(mask,bodyArea.transform,new Vector2(0,1),Vector2.zero,new Vector2(860,220));
            mask.GetComponent<Image>().color=Color.clear;
            var body=Text("Content","",mask,Vector2.zero,new Vector2(852,220),28);
            body.textWrappingMode=TextWrappingModes.Normal; body.richText=false; body.lineSpacing=12;
            var fitter=body.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            bodyArea.viewport=mask; bodyArea.content=body.rectTransform; bodyArea.horizontal=false; bodyArea.vertical=true; bodyArea.movementType=ScrollRect.MovementType.Clamped; bodyArea.scrollSensitivity=30;
            var buttonImage=Picture("Confirm",laptop.transform,new Vector2(0,1),new Vector2(810,-485),new Vector2(180,56),"button_normal");
            buttonImage.raycastTarget=true;
            var button=buttonImage.gameObject.AddComponent<Button>(); button.targetGraphic=buttonImage; button.transition=Selectable.Transition.SpriteSwap;
            button.spriteState=new SpriteState {highlightedSprite=Sprite("button_hover"),pressedSprite=Sprite("button_pressed"),selectedSprite=Sprite("button_hover")};
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            var label=Text("Label","확인",button.transform,new Vector2(0,0),new Vector2(180,56),24); label.alignment=TextAlignmentOptions.Center;
            Text("Hint","",laptop.transform,new Vector2(130,-501),new Vector2(640,28),18).gameObject.SetActive(false);
            var popup=root.GetComponent<EventLaptopPopup>();
            Set(popup,"panel",overlay.gameObject); Set(popup,"number",number); Set(popup,"title",title); Set(popup,"body",body); Set(popup,"buttonLabel",label); Set(popup,"confirm",button); Set(popup,"scroll",bodyArea);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png") ?? throw new InvalidOperationException("Missing Sprite "+name);
    static Image Picture(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,string sprite)
    {
        var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
        Place(image.rectTransform,parent,anchor,pos,size); image.sprite=Sprite(sprite); image.raycastTarget=false; return image;
    }
    static TMP_Text Text(string name,string value,Transform parent,Vector2 pos,Vector2 size,float fontSize)
    {
        var t=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Place(t.rectTransform,parent,new Vector2(0,1),pos,size); t.font=font; t.text=value; t.fontSize=fontSize;
        t.color=new Color32(222,229,204,255); t.raycastTarget=false; t.textWrappingMode=TextWrappingModes.Normal; t.richText=false; return t;
    }
    static void Place(RectTransform t,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    { t.SetParent(parent,false); t.anchorMin=t.anchorMax=t.pivot=anchor; t.anchoredPosition=pos; t.sizeDelta=size; }
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    { var s=new SerializedObject(target); s.FindProperty(field).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); }
}

// All positions are measured from the top left of the supplied 1523 x 1033 image,
// scaled to 1440 logical pixels wide. The controls sit inside its black console.
internal static class LaptopTerminalLayout
{
    internal static void Apply(RectTransform laptop, bool eventScreen)
    {
        laptop.sizeDelta = new Vector2(1440, 1440f * 1033 / 1523);
        laptop.GetComponent<Image>().preserveAspect = true;
        var paper = new Color32(232, 237, 234, 255);
        var accent = new Color32(153, 220, 206, 255);
        var dim = new Color32(178, 195, 195, 255);

        RectTransform Box(string path, float x, float y, float w, float h)
        {
            var rect = (RectTransform)laptop.Find(path);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        TMP_Text Label(RectTransform rect, int size, Color32 color)
        {
            var label = rect.GetComponent<TMP_Text>();
            label.fontSize = size;
            label.color = color;
            return label;
        }
        void Button(string name, int x, int width)
        {
            var rect = Box(name, x, 515, width, 48);
            rect.GetComponent<Image>().color = new Color32(196, 215, 224, 255);
            var label = Label(Box(name + "/Label", 0, 0, width, 48), 22, paper);
            label.alignment = TextAlignmentOptions.Center;
        }

        Label(Box(eventScreen ? "Number" : "Score", 273, 188, 888, 30), 22, accent);
        var title = Label(Box("Title", 273, 225, 888, 49), 36, paper);
        title.fontSizeMax = 36;
        int bodyHeight = eventScreen ? 140 : 210;
        Box("BodyScroll", 273, 285, 888, bodyHeight);
        Box("BodyScroll/Viewport", 0, 0, 888, bodyHeight);
        var body = Label(Box("BodyScroll/Viewport/Content", 0, 0, 880, bodyHeight), 26, paper);
        body.lineSpacing = 3;

        if (eventScreen)
        {
            var result = Label(Box("Result", 273, 428, 888, 54), 23, accent);
            result.fontSizeMax = 23;
            result.fontSizeMin = 16;
            result.textWrappingMode = TextWrappingModes.Normal;
            Label(Box("Hint", 273, 525, 490, 32), 20, dim);
            Button("Yes", 824, 156);
            Button("No", 996, 165);
            Button("Confirm", 961, 200);
        }
        else
        {
            Button("Confirm", 693, 224);
            Button("Retry", 937, 224);
        }
    }
}
