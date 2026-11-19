using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.Shelter;
using UnityGameJam.Events;
using UnityGameJam.GameDev;

internal static class PaperUISetup
{
    const string Art = "Assets/Art/UI/Paper/";
    const string Prefabs = "Assets/Prefabs/";
    static readonly Color Ink = new Color32(51, 41, 36, 255);
    static readonly Color Teal = new Color32(65, 113, 107, 255);
    static readonly Color Cream = new Color32(255, 247, 225, 255);
    static readonly Vector2 TopLeft = new Vector2(0, 1);
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png") ?? throw new InvalidOperationException(name);
    static RectTransform Rect(Transform t) => (RectTransform)t;
    static RectTransform Find(Transform t, string path) => Rect(t.Find(path) ?? throw new InvalidOperationException(t.name + "/" + path));
    static void Box(RectTransform t, float x, float y, float w, float h)
    {
        t.anchorMin = t.anchorMax = t.pivot = TopLeft;
        t.anchoredPosition = new Vector2(x,-y);
        t.sizeDelta = new Vector2(w,h);
    }
    static RectTransform Box(Transform t, string path, float x, float y, float w, float h)
    {
        var rect = Find(t,path);
        Box(rect,x,y,w,h);
        return rect;
    }
    static Image ArtImage(Transform t, string name)
    {
        var image = t.GetComponent<Image>();
        image.sprite = Sprite(name);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        return image;
    }
    static void Clear(Transform t) => t.GetComponent<Image>().color = Color.clear;
    static TMP_Text Type(Transform t, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var label = t.GetComponent<TMP_Text>();
        label.color = color;
        label.fontSize = label.fontSizeMax = size;
        label.fontSizeMin = Mathf.Min(size, 14);
        label.enableAutoSizing = true;
        label.alignment = alignment;
        return label;
    }
    static void FitLabel(TMP_Text label, bool multiline = false)
    {
        float maximum = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
        label.fontSizeMax = maximum;
        label.fontSizeMin = Mathf.Min(12f, maximum);
        label.enableAutoSizing = true;
        label.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }
    static void Size(Transform t, float w, float h) => Rect(t).sizeDelta = new Vector2(w,h);
    static TMP_Text Caption(Transform t, string path, int x,int y,int w,int h,int size,Color color,TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        => Type(Box(t,path,x,y,w,h),size,color,align);
    static RectTransform Child(Transform p,string name,Type type)
    {
        var t = p.Find(name);
        if(t) return Rect(t);
        var child = new GameObject(name,typeof(RectTransform),type);
        child.transform.SetParent(p,false);
        return Rect(child.transform);
    }
    static Image Pic(Transform p,string name,string sprite,int x,int y,int w,int h)
    {
        var rect = Child(p,name,typeof(Image));
        Box(rect,x,y,w,h);
        var image = ArtImage(rect,sprite);
        image.raycastTarget = false;
        return image;
    }
    static TMP_Text NewText(Transform p,string name,string text,int x,int y,int w,int h,int size,Color color)
    {
        var rect=Child(p,name,typeof(TextMeshProUGUI));
        Box(rect,x,y,w,h);
        var label=Type(rect,size,color,TextAlignmentOptions.Center);
        label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
        label.text=text;label.raycastTarget=false;return label;
    }
    static void Field(UnityEngine.Object target,string key,UnityEngine.Object value)
    {
        var data=new SerializedObject(target);
        data.FindProperty(key).objectReferenceValue=value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void ButtonStyle(Transform t,string sprite,float size,Color ink)
    {
        var button=t.GetComponent<Button>();var image=t.GetComponent<Image>();
        var label=t.GetComponentInChildren<TMP_Text>(true);
        if (sprite==null) Clear(t); else ArtImage(t,sprite);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
        label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        Type(label.transform,size,ink,TextAlignmentOptions.Center);
        button.transition=Selectable.Transition.ColorTint;
        button.targetGraphic=sprite==null?(Graphic)label:image;
        var colors=ColorBlock.defaultColorBlock;
        colors.highlightedColor=colors.selectedColor=new Color(.82f,.90f,.83f);
        colors.pressedColor=new Color(.67f,.78f,.73f);
        button.colors=colors;
    }
    static void Edit(string path,Action<GameObject> edit)
    {
        path=Prefabs+path+".prefab";
        var obj=PrefabUtility.LoadPrefabContents(path);
        try {edit(obj);PrefabUtility.SaveAsPrefabAsset(obj,path,out bool ok);if(!ok)throw new IOException(path);}
        finally {PrefabUtility.UnloadPrefabContents(obj);}
    }
    static Transform View(GameObject root) => root.transform.Find("Viewport 1920x1080");

    [MenuItem("Tools/UI/Apply Paper Artwork")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before applying UI artwork.");
        Title();
        Options();
        Inventory();
        Card();
        Development();
        World();
        Reports();
        Farming();
    }
    public static void ImportArt()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var path in Directory.GetFiles(Art,"*.png")){
            var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;
            ti.spritePixelsPerUnit=100;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;
            ti.filterMode=Path.GetFileName(path).StartsWith("Face_")?FilterMode.Point:FilterMode.Bilinear;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        }
    }
    public static void Options()
    {
        Edit("UI/Resources/GameOptions", root=>{
            var t=root.transform.Find("Options modal/Sound settings");Size(t,491,478);ArtImage(t,"Options");
            Caption(t,"Heading",62,22,180,52,30,Cream,TextAlignmentOptions.Center);
            Clear(t.Find("Rule"));
            int[] ys={124,178,224,276,328};string[] names={"전체 음량","배경 음악","효과음","인터페이스","환경음"};
            for(int i=0;i<5;i++){
                Caption(t,names[i]+" label",34,ys[i]-16,140,32,20,Ink);
                var slider=Box(t,names[i],183,ys[i]-18,240,36);
                Clear(slider);Clear(slider.Find("Track"));
                Size(slider.Find("Fill area"),230,12); Size(slider.Find("Handle area"),230,28);
                slider.Find("Fill area/Volume").GetComponent<Image>().color=Teal;
            }
            ButtonStyle(Box(t,"Main menu",63,389,175,50),null,21,Cream);
            ButtonStyle(Box(t,"Close",270,389,153,50),null,22,Cream);
            ButtonStyle(root.transform.Find("Options tab"),"Button2",20,Ink);
        });
    }
    public static void Inventory()
    {
        Edit("UI/ShelterInventorySlot",r=>{
            Clear(r.transform);Size(r.transform,111,105);
            Size(r.transform.Find("Hover corners"),111,105);
            Box(r.transform,"Icon",18,7,75,64);
            FitLabel(Caption(r.transform,"Item name",6,72,99,27,18,Ink,TextAlignmentOptions.Center));
        });
        Edit("UI/ShelterInventory",r=>{
            var opener=View(r).Find("Bag button").GetComponent<Image>();
            opener.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Farming/slot_empty.png");
            opener.type=Image.Type.Simple;opener.color=Color.white;
            var t=View(r).Find("Inventory panel");Size(t,390,368);ArtImage(t,"Bag");Rect(t).anchoredPosition=new Vector2(-32,190);
            Box(t,"Backpack",53,31,31,33);
            Caption(t,"Title",88,26,156,41,25,Ink);
            Caption(t,"Owned count",245,70,101,27,17,Ink,TextAlignmentOptions.Center);
            var grid=Box(t,"Grid",84,104,239,225).GetComponent<GridLayoutGroup>();
            grid.cellSize=new Vector2(111,105);grid.spacing=new Vector2(15,12);
            var slip=Pic(t,"Inventory footer","Button2",5,350,286,45);slip.transform.SetAsFirstSibling();
            var footer = Caption(t,"Hint",35,354,228,34,16,Ink,TextAlignmentOptions.Center);
            footer.text = "";
            footer.gameObject.SetActive(false);
            slip.gameObject.SetActive(false);
            Field(r.GetComponent<ShelterInventory>(), "footerPaper", slip.gameObject);
            FitLabel(footer, true);
            FitLabel(t.Find("Owned count").GetComponent<TMP_Text>());
            var pages=Box(t,"Page navigation",279,349,107,42);
            ButtonStyle(Box(pages,"Previous",0,0,30,40),"Button1",17,Ink);
            ButtonStyle(Box(pages,"Next",77,0,30,40),"Button3",17,Ink);
            Pic(pages,"Paper","Button2",26,1,56,37).transform.SetAsFirstSibling();
            FitLabel(Caption(pages,"Page",25,3,57,32,16,Ink,TextAlignmentOptions.Center));
            var tip=View(r).Find("Item tooltip");ArtImage(tip,"Button2");
            FitLabel(Caption(tip,"Name",40,17,197,38,21,Ink));
            FitLabel(Caption(tip,"Details",40,55,198,64,18,Ink), true);
        });
    }
    public static void Card()
    {
        Edit("UI/ShelterCharacterCard",r=>{
            var t=View(r).Find("Character card");Size(t,379,591);ArtImage(t,"Survivor");
            Caption(t,"Name",66,16,123,62,25,Cream,TextAlignmentOptions.Center);
            Caption(t,"Role",185,16,116,62,19,Cream,TextAlignmentOptions.Center);
            ButtonStyle(Box(t,"Close",316,33,42,42),"Button3",23,Ink);
            var portrait=Box(t,"Portrait mask",0,92,379,257);Clear(portrait);
            var mask=portrait.GetComponent<Mask>();if(mask)mask.enabled=false;
            Box(portrait,"Portrait",38,24,127,153).GetComponent<Image>().preserveAspect=true;
            string[] stats={"planning","programming","art","combat"};
            for(int i=0;i<4;i++){
                Caption(portrait,stats[i]+" label",198,4+39*i,111,35,18,Ink);
                Caption(portrait,stats[i]+" value",309,4+39*i,40,35,20,Ink,TextAlignmentOptions.Right);
            }
            Box(portrait,"Mental track",96,224,251,23);var track=portrait.Find("Mental track");Clear(track);
            Box(track,"Mental fill",1,2,248,19).GetComponent<Image>().color=new Color(.44f,.66f,.60f);
            track.SetAsFirstSibling();
            Caption(portrait,"Mental heading",103,217,78,34,17,Ink);
            Caption(portrait,"Mental value",198,217,143,34,18,Ink,TextAlignmentOptions.Right);
            var hunger=Box(t,"Hunger",91,367,261,32);
            Caption(hunger,"Hunger label",5,0,49,30,17,Ink);
            for(int i=0;i<10;i++){var cell=Box(hunger,"Cell "+(i+1),58+20*i,8,18,16); Box(cell,"Filled",1,1,16,14);}
            Caption(t,"Equipment heading",38,394,310,32,18,Ink);
            var empty=Box(t,"Empty equipment",37,449,313,93);Clear(empty);Caption(empty,"Message",114,12,198,63,18,Ink);
            var list=Box(t,"Equipment list",37,449,313,93);Clear(list);Size(list.Find("Items"),313,93);
            foreach (var label in t.GetComponentsInChildren<TMP_Text>(true)) FitLabel(label);
        });
        Edit("UI/CharacterEquipmentRow",r=>{
            Size(r.transform,313,90);if(r.GetComponent<Image>())Clear(r.transform);
            Box(r.transform,"Icon",8,7,78,76);FitLabel(Caption(r.transform,"Item name",115,7,180,76,19,Ink), true);
            var layout=r.GetComponent<LayoutElement>();if(layout) layout.preferredHeight=90;
        });
    }
    public static void World()
    {
        Edit("UI/ShelterRoomHeader",r=>{
            var header=ArtImage(r.transform,"Button2");
            header.color=new Color32(60, 92, 89, 255);
            FitLabel(Caption(r.transform,"Room name",22,3,102,30,21,Cream));
            Box(r.transform,"Occupancy icon",129,7,21,21);
            FitLabel(Caption(r.transform,"Occupancy",152,3,36,30,19,Cream,TextAlignmentOptions.Center));
        });
        Edit("UI/ShelterSpeech",r=>{
            r.GetComponent<Image>().color=new Color32(246,231,208,255);
            r.GetComponentInChildren<TMP_Text>(true).color=Ink;
        });
    }
    public static void Development()
    {
        Edit("UI/DevGageUI",r=>{
            var t=View(r).Find("Development panel");Clear(t);
            // Keep the three fields in one row so the map below remains clickable.
            Size(t,987,61);string[] names={"Planning","Programming","Graphics"};
            for(int i=0;i<3;i++){
                var row=Box(t,names[i],i*331,0,327,58);ArtImage(row,names[i]);
                FitLabel(Caption(row,"Label",51,7,82,42,19,Ink));
                var track=Box(row,"Track",137,17,168,23);Clear(track);
                var fill=Box(track,"Fill",2,2,164,19).GetComponent<Image>();fill.color=new Color(.43f,.65f,.59f);
                track.SetAsFirstSibling(); // Labels and the lock draw over the gauge.
                Caption(row,"Percentage",151,12,140,33,18,Ink,TextAlignmentOptions.Center);
                if(i!=0)Box(row,"Work lock",287,18,18,18);
            }
            FitLabel(Box(t,"Status",0,65,950,52).GetComponent<TMP_Text>(), true);
            foreach (var hint in t.GetComponentsInChildren<TMP_Text>(true))
                if (hint.name=="Hint") FitLabel(hint, true);
        });
    }
    static void SheetBody(Transform t,string scroll,int x,int y,int w,int h,int size)
    {
        var area=Box(t,scroll,x,y,w,h);
        var viewport=Box(area,"Viewport",0,0,w,h);
        var body=Type(Box(viewport,"Content",0,0,w-5,h),size,Ink);
        body.lineSpacing=3;body.alignment=TextAlignmentOptions.TopLeft;
    }
    static void Faces(UnityEngine.Object target)
    {
        Field(target,"angryFace",Sprite("Face_angry"));Field(target,"neutralFace",Sprite("Face_hmm"));Field(target,"happyFace",Sprite("Face_happy"));
    }
    internal static void Ending(GameObject r)
    {
        var t=View(r).Find("Overlay/Laptop");Size(t,654,365);Rect(t).localScale=Vector3.one*1.5f;ArtImage(t,"Ending");
        Caption(t,"Title",117,46,250,44,27,Cream);
        Caption(t,"Score",60,111,528,44,26,Ink);
        SheetBody(t,"BodyScroll",62,179,282,68,20);
        var face=Pic(t,"Reaction","Face_hmm",434,166,87,84);
        ButtonStyle(Box(t,"Confirm",99,274,212,53),null,23,Cream);
        ButtonStyle(Box(t,"Retry",341,274,199,53),null,23,Cream);
        var popup=r.GetComponent<GameDevEndingPopup>();Field(popup,"reactionFace",face);Faces(popup);
    }
    internal static void Review(GameObject r)
    {
        var host=View(r).Find("Overlay");
        var laptop=host.Find("Laptop");
        var instructions = laptop.Find("Hint");
        if (instructions) instructions.gameObject.SetActive(false);
        FitLabel(laptop.Find("Title").GetComponent<TMP_Text>());
        FitLabel(laptop.Find("Result").GetComponent<TMP_Text>(), true);
        ButtonStyle(laptop.Find("Yes"),"Button1",22,Ink);
        ButtonStyle(laptop.Find("No"),"Button3",22,Ink);
        ButtonStyle(laptop.Find("Confirm"),"Button2",22,Ink);
        var t=Child(host,"Presentation sheet",typeof(Image));
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);t.anchoredPosition=Vector2.zero;
        Size(t,514,491);t.localScale=Vector3.one*1.5f;
        ArtImage(t,"Review").raycastTarget=false;
        var popup=r.GetComponent<EventLaptopPopup>();Field(popup,"presentationSheet",t.gameObject);
        NewText(t,"Title","중간 발표",57,25,204,57,25,Cream);
        var plate=Pic(t,"Grade tab","Button2",168,83,190,53);
        plate.color=new Color(.27f,.29f,.27f);
        var grade=NewText(t,"Result","",169,84,188,49,34,Cream);Field(popup,"reviewGrade",grade);
        var face=Pic(t,"Reaction","Face_hmm",126,79,53,55);Field(popup,"reviewFace",face);Faces(popup);
        string[] labels={"기획","프로그래밍","그래픽"};
        string[] bindings={"reviewPlanning","reviewProgramming","reviewGraphics"};
        for(int i=0;i<3;i++){
            int y=169+i*41;var label=NewText(t,bindings[i]+" label",labels[i],62,y,184,32,21,Ink);
            label.alignment=TextAlignmentOptions.MidlineLeft;
            var value=NewText(t,bindings[i],"",269,y,184,33,21,Ink);Field(popup,bindings[i],value);
        }
        Field(popup,"reviewScore",NewText(t,"Score","",42,300,180,82,19,Ink));
        Field(popup,"reviewMental",NewText(t,"Mental","",229,300,242,82,17,Ink));
        var hit=Child(t,"Confirm",typeof(Image));Box(hit,158,397,210,54);
        NewText(hit,"Label","계속",0,0,210,54,24,Cream);
        var b=hit.GetComponent<Button>()??hit.gameObject.AddComponent<Button>();
        ButtonStyle(hit,null,24,Cream);
        if(b.onClick.GetPersistentEventCount()==0)UnityEventTools.AddPersistentListener(b.onClick,popup.Confirm);
        t.gameObject.SetActive(false);
    }
    public static void Reports()
    {
        Edit("UI/GameDevEndingPopup",Ending);Edit("UI/EventLaptopPopup",Review);
    }
    public static void Farming()
    {
        Edit("Farming/FarmingHud",r=>{
            var v=View(r);var t=v.Find("Ready and finished");
            Size(t,720,580);ArtImage(t,"Work");
            Caption(t,"Phase label",150,90,270,56,24,Ink,TextAlignmentOptions.Center);
            Caption(t,"Title",104,166,540,63,34,Ink);
            Caption(t,"Instructions",104,235,548,234,25,Ink);
            ButtonStyle(Box(t,"Action",209,481,309,63),"Button2",23,Ink);
            foreach(string n in new[]{"Carry","Saved","Interaction","Stage","Timer"}){
                var pane=v.Find(n);ArtImage(pane,"Button2");
                foreach(var text in pane.GetComponentsInChildren<TMP_Text>(true))Type(text.transform,text.fontSize,Ink,text.alignment);
            }
            var saved=v.Find("Saved");Pic(saved,"Supplies","Crate",14,12,45,43);
            Caption(saved,"Saved counts",65,10,293,48,23,Ink);
            // The runtime urgency colour is drawn over this dark ink patch.
            var timer=v.Find("Timer");Rect(timer).anchoredPosition=new Vector2(-28,-72);var backing=Pic(timer,"Time backing","Button2",12,30,200,65);backing.color=new Color(.25f,.25f,.24f);backing.transform.SetAsFirstSibling();
            var timerLabel = Caption(timer,"Label",34,8,150,26,18,Ink,TextAlignmentOptions.Center);
            Field(r.GetComponent<ShelterRun.FarmingHud>(), "timerLabel", timerLabel);
            Caption(timer,"Seconds",47,35,100,53,40,Cream,TextAlignmentOptions.Center);
            Caption(timer,"Unit",143,46,38,35,20,Cream,TextAlignmentOptions.Center);
            Box(timer,"Time gauge",34,94,152,5);
            var carry=v.Find("Carry");
            Caption(carry,"Capacity",32,13,208,30,21,Ink,TextAlignmentOptions.Center).text="운반";
            var slots=Find(carry,"Slots");
            slots.anchorMin=slots.anchorMax=slots.pivot=new Vector2(.5f,.5f);
            slots.anchoredPosition=new Vector2(0,-15);
            slots.GetComponent<LayoutGroup>().childAlignment=TextAnchor.MiddleCenter;
        });
    }
    public static void Title()
    {
        const string path="Assets/Scenes/Title.unity";
        var scene=SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            if(scene.isDirty)throw new InvalidOperationException("Title has unsaved changes.");
            var t=scene.GetRootGameObjects().Single(r=>r.name=="Start screen").transform;
            var home=t.Find("Main menu");
            string[] names={"Start","Options","Quit"};
            for(int i=0;i<names.Length;i++)ButtonStyle(home.Find(names[i]),"Button"+(i+1),25,Ink);
            var select=t.Find("Character selection");var preview=select.Find("Survivor preview");
            ArtImage(preview,"Work");
            var stage=preview.Find("Idle stage");Clear(stage);
            var previewName=preview.Find("Selected name");Box(Rect(previewName),116,73,214,38);Type(previewName,26,Ink,TextAlignmentOptions.Center);
            Type(preview.Find("Introduction"),20,Ink,TextAlignmentOptions.Center);
            for(int i=1;i<=6;i++)ButtonStyle(preview.Find("Candidate "+i),"Button"+(1+(i-1)%3),20,Ink);
            ButtonStyle(select.Find("Back"),"Button1",24,Ink);ButtonStyle(select.Find("Confirm"),"Button2",24,Ink);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException(path);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}

