using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace ShelterRun.Editor
{
    public static class FarmingSideViewBuilder
    {
        const string Prefabs="Assets/Prefabs/Farming/";
        static TMP_FontAsset font;
        [MenuItem("Tools/Farming/Rebuild Side View Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring.");
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene edits before rebuilding.");
            var rules=AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath);
            var db=AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);rules.Validate();db.Validate();
            Directory.CreateDirectory("Temp/FarmingBeforeSideView");
            if(File.Exists(FarmingSceneBuilder.ScenePath)&&!File.Exists("Temp/FarmingBeforeSideView/Farming.unity"))File.Copy(FarmingSceneBuilder.ScenePath,"Temp/FarmingBeforeSideView/Farming.unity");
            FarmingSideViewArt.Create(db);font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
            UpdateHud();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var map=new GameObject("Side View Map").AddComponent<FarmingMap2D>();
            var corridor=Area("복도",0,32,true);var rooms=new List<FarmingRoom2D>();
            for(int r=0;r<3;r++)rooms.Add(Area((325-r)+"호",(r+1)*20,rules.MapHalfExtents.x*2,false));
            var doorPrefab=DoorPrefab();var doors=new List<FarmingDoor2D>();
            for(int r=0;r<rooms.Count;r++)
            {
                var outside=Door(doorPrefab,corridor,8+r*9,rooms[r].name);
                var inside=Door(doorPrefab,rooms[r],1.25f,"복도");
                Set(outside,"target",inside);Set(inside,"target",outside);doors.Add(outside);doors.Add(inside);
            }
            var pickupPrefabs=PickupPrefabs(db);var tablePrefab=TablePrefab();
            for(int r=0;r<rooms.Count;r++)
            {
                var room=rooms[r];var interior=room.transform.Find("Visuals");
                for(int i=0;i<6;i++)
                {
                    float x=4+i*(room.Right-6)/5;
                    var table=(GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);table.name="Table "+(i+1);table.transform.SetParent(interior);table.transform.position=new Vector3(x,room.GroundY,0);
                    SpriteObject("Chair","chair",interior,new Vector3(x-.6f,room.GroundY,0),5);
                }
            }
            var player=Player();player.transform.position=new Vector3(5,0,0);
            var bunker=Bunker(corridor.transform.Find("Visuals"));
            var hud=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"FarmingHud.prefab"))).GetComponent<FarmingHud>();
            var session=new GameObject("Farming Session").AddComponent<FarmingSession>();Set(session,"player",player);Set(session,"bunker",bunker);Set(session,"hud",hud);
            SetArray(session,"pickupPrefabs",db.Records.Select(row => pickupPrefabs[row.Id].GetComponent<FarmingPickup>()).ToArray());
            Set(map,"corridor",corridor);SetArray(map,"rooms",rooms.Cast<UnityEngine.Object>().ToArray());SetArray(map,"doors",doors.Cast<UnityEngine.Object>().ToArray());Set(map,"player",player);Set(map,"session",session);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=FarmingSideViewArt.C("192E39");camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transparencySortMode=TransparencySortMode.Orthographic;
            var follow=cameraObject.AddComponent<FarmingCamera>();Set(follow,"target",player.transform);Number(follow,"referenceSize",4.5f);cameraObject.transform.position=new Vector3(8,2,-20);ConfigureRenderer(camera);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            corridor.Apply(1);foreach(var room in rooms)room.Apply(0);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,FarmingSceneBuilder.ScenePath);
            if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(new Vector3(10,2,0),Quaternion.identity,8);}
            Debug.Log($"Farming side view saved with {pickupPrefabs.Count} pickup prefabs.");
        }
        static FarmingRoom2D Area(string name,float y,float width,bool hall)
        {
            var root=new GameObject(name);var room=root.AddComponent<FarmingRoom2D>();var visuals=new GameObject("Visuals").transform;visuals.SetParent(root.transform);
            var left=Anchor("Walk left",root.transform,new Vector3(0,y,0));var right=Anchor("Walk right",root.transform,new Vector3(width,y,0));var camera=Anchor("Camera height",root.transform,new Vector3(width*.5f,y+2,0));
            var wall=SpriteObject("Wall",hall?"wall_hall":"wall_room",visuals,new Vector3(0,y,0),-100);wall.GetComponent<SpriteRenderer>().drawMode=SpriteDrawMode.Tiled;wall.GetComponent<SpriteRenderer>().size=new Vector2(width,4);
            var floor=SpriteObject("Floor","floor",visuals,new Vector3(0,y-.75f,0),-90);floor.GetComponent<SpriteRenderer>().drawMode=SpriteDrawMode.Tiled;floor.GetComponent<SpriteRenderer>().size=new Vector2(width,.75f);
            if(hall)for(float x=12.5f;x<width-1;x+=9)SpriteObject("Window","window",visuals,new Vector3(x,y+1.45f,0),-50);
            else {SpriteObject("Window","window",visuals,new Vector3(width*.5f,y+1.6f,0),-50);Label(name+"  /  파밍 구역",visuals,new Vector3(width*.5f,y+3.65f,0),2.8f,30);}
            Label(hall?"CONFERENCE HALL":"",visuals,new Vector3(width*.5f,y+4.4f,0),2.8f,30);
            var so=new SerializedObject(room);so.FindProperty("footprint").rectValue=new Rect(0,y-2,width,8);so.ApplyModifiedPropertiesWithoutUndo();Set(room,"walkLeft",left);Set(room,"walkRight",right);Set(room,"cameraAnchor",camera);Set(room,"interior",visuals);return room;
        }
        static GameObject DoorPrefab()
        {
            var root=SpriteObject("Door","door",null,Vector3.zero,0);var door=root.AddComponent<FarmingDoor2D>();
            var focus=SpriteObject("Focus","door_focus",root.transform,Vector3.zero,1);focus.SetActive(false);Set(door,"focusMarker",focus);Set(door,"arrival",Anchor("Arrival",root.transform,Vector3.right));
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"Door.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static FarmingDoor2D Door(GameObject prefab,FarmingRoom2D room,float x,string destination)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);root.name="Door to "+destination;root.transform.SetParent(room.transform.Find("Visuals"));root.transform.position=new Vector3(x,room.GroundY,0);var door=root.GetComponent<FarmingDoor2D>();Set(door,"area",room);Text(door,"destinationLabel",destination);Label(destination,root.transform,new Vector3(x,room.GroundY+3.25f,0),3,30);return door;
        }
        static Dictionary<string,GameObject> PickupPrefabs(FarmingDatabase db)
        {
            var result=new Dictionary<string,GameObject>();
            foreach(var row in db.Records)
            {
                var root=new GameObject(row.Id);root.AddComponent<FarmingDepth2D>();var pickup=root.AddComponent<FarmingPickup>();
                var visual=SpriteObject("Visual",row.Id,root.transform,Vector3.zero,1);var focus=SpriteObject("Focus","focus",root.transform,Vector3.zero,-1);focus.SetActive(false);
                var label=Label(row.Weight+"칸",root.transform,new Vector3(0,row.Type==FarmingObjectType.Person?1.55f:.8f,0),2.3f,20);
                Text(pickup,"recordId",row.Id);Set(pickup,"visual",visual.transform);Set(pickup,"sprite",visual.GetComponent<SpriteRenderer>());Set(pickup,"standingSprite",FarmingSideViewArt.Load(row.Id));Set(pickup,"lyingSprite",FarmingSideViewArt.Load(row.Type==FarmingObjectType.Person?row.Id+"_lying":row.Id));Set(pickup,"focusMarker",focus);Set(pickup,"weightLabel",label);
                result.Add(row.Id,PrefabUtility.SaveAsPrefabAsset(root,Prefabs+row.Id+".prefab"));UnityEngine.Object.DestroyImmediate(root);
            }
            return result;
        }
        static GameObject TablePrefab()
        {
            var root=SpriteObject("Table","table",null,Vector3.zero,0);root.AddComponent<SortingGroup>().sortingOrder=10;
            AddTabletopSlots(root);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"Table.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        internal static void AddTabletopSlots(GameObject root)
        {
            var table = root.AddComponent<FarmingSpawnTable>();
            var left = Anchor("Left tabletop slot", root.transform, root.transform.TransformPoint(new Vector3(-.65f, 1.4f)));
            var right = Anchor("Right tabletop slot", root.transform, root.transform.TransformPoint(new Vector3(.35f, 1.4f)));
            SetArray(table, "tabletopSlots", new UnityEngine.Object[] { left, right });
        }
        static FarmingPlayer Player()
        {
            var root=new GameObject("Player");var depth=root.AddComponent<FarmingDepth2D>();var so=new SerializedObject(depth);so.FindProperty("worldOrder").intValue=200;so.ApplyModifiedPropertiesWithoutUndo();var feet=root.AddComponent<CircleCollider2D>();feet.radius=.17f;feet.isTrigger=true;
            var body=SpriteObject("Body","player_idle",root.transform,Vector3.zero,1);var player=root.AddComponent<FarmingPlayer>();Set(player,"body",body.transform);Set(player,"sprite",body.GetComponent<SpriteRenderer>());Set(player,"idleSprite",FarmingSideViewArt.Load("player_idle"));Set(player,"carrySprite",FarmingSideViewArt.Load("player_carry"));Set(player,"feet",feet);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"Player.prefab");UnityEngine.Object.DestroyImmediate(root);return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<FarmingPlayer>();
        }
        static FarmingBunker Bunker(Transform parent)
        {
            var root=SpriteObject("Bunker drop zone","bunker",parent,new Vector3(2,0,0),0);var collider=root.AddComponent<PolygonCollider2D>();collider.points=new[]{new Vector2(-1.25f,-.3f),new Vector2(1.25f,-.3f),new Vector2(1.25f,.5f),new Vector2(-1.25f,.5f)};collider.isTrigger=true;
            var marker=SpriteObject("Ready glow","focus",root.transform,root.transform.position,1);marker.transform.localScale=Vector3.one*1.6f;marker.SetActive(false);
            var bunker=root.AddComponent<FarmingBunker>();Set(bunker,"depositZone",collider);Set(bunker,"landingPoint",Anchor("Landing",root.transform,root.transform.position));Set(bunker,"readyMarker",marker);Label("BUNKER",parent,new Vector3(2,3.3f,0),3,30);return bunker;
        }
        static void UpdateHud()
        {
            string path=Prefabs+"FarmingHud.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))if(label.text.Contains("W A S D"))label.text="A / D  좌우 이동   ·   E  문 / 들기 / 내려놓기";PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void ConfigureRenderer(Camera camera)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(!pipeline)return;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Farming/FarmingRenderer.asset");var so=new SerializedObject(pipeline);var array=so.FindProperty("m_RendererDataList");for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).objectReferenceValue==renderer){camera.GetUniversalAdditionalCameraData().SetRenderer(i);return;}throw new InvalidOperationException("Existing FarmingRenderer is missing.");
        }
        static GameObject SpriteObject(string name,string sprite,Transform parent,Vector3 position,int order)
        {var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.position=position;var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=FarmingSideViewArt.Load(sprite);renderer.spriteSortPoint=SpriteSortPoint.Pivot;renderer.sortingOrder=order;return go;}
        static Transform Anchor(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;return t;}
        static TMP_Text Label(string text,Transform parent,Vector3 position,float size,int order)
        {var go=new GameObject("Label "+text,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.position=position;var label=go.GetComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=FarmingSideViewArt.C("F4EBD9");label.rectTransform.sizeDelta=new Vector2(5,1);label.GetComponent<MeshRenderer>().sortingOrder=order;return label;}
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Text(UnityEngine.Object target,string field,string value){var so=new SerializedObject(target);so.FindProperty(field).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Number(UnityEngine.Object target,string field,float value){var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void SetArray(UnityEngine.Object target,string field,UnityEngine.Object[] values){var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
