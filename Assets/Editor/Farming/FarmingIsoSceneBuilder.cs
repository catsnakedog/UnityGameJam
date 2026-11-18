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
using UnityEngine.Tilemaps;

namespace ShelterRun.Editor
{
    public static class FarmingIsoSceneBuilder
    {
        const string Prefabs = "Assets/Prefabs/Farming/";
        const string Tiles = "Assets/Art/Farming/Isometric/Tiles/";
        static readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
        static TMP_FontAsset font;
        static Transform gridRoot;
        static Tilemap floor;
        static FarmingRules rules;

        [MenuItem("Tools/Farming/Rebuild 2D Corridor Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Current scene has unsaved edits; save them before rebuilding.");
            Directory.CreateDirectory(Tiles);
            rules = AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath);
            var data = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);
            data.Validate();
            // Schema migration is already saved in the authoritative Rules asset.
            // Invalid authored values must fail validation, not silently reset here.
            rules.Validate();
            FarmingIsoArt.Create(data); tiles.Clear();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grid = new GameObject("Isometric 2D Grid", typeof(Grid)).GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric; grid.cellSize = new Vector3(1,.5f,1);
            gridRoot = grid.transform;
            floor = Layer("Floor",gridRoot,-10000);
            int width = Mathf.RoundToInt(rules.MapHalfExtents.x * 2), height = Mathf.RoundToInt(rules.MapHalfExtents.y * 2);
            int stride = height + 2, length = 3 * stride + 4;
            Paint(floor,new RectInt(0,0,6,length),"floor_hall");
            for (int y=0;y<length;y++)
            {
                floor.SetTile(Cell(5,y),Tile("floor_border"));
                if (y % stride == 1) for(int x=0;x<5;x++) floor.SetTile(Cell(x,y),Tile("floor_border"));
            }
            var environment = new GameObject("Corridor and rooms").transform;
            var hallWalls = Layer("Corridor outer edge",gridRoot,FarmingDepth2D.WorldOrder);
            for(int y=0;y<length;y++) hallWalls.SetTile(Cell(6,y),Tile("wall_hall"));
            Barrier("Corridor outer wall",environment,new Rect(6,0,.25f,length));
            Barrier("Corridor start",environment,new Rect(0,-.25f,6,.25f));
            Barrier("Corridor end",environment,new Rect(0,length,6,.25f));
            // Close the short connections between rooms, including both hallway end caps.
            for (int y=0;y<length;y++)
                if (!Enumerable.Range(0,3).Any(i=>y>=2+i*stride&&y<2+i*stride+height))
                { Barrier("Hall left return",environment,new Rect(-.25f,y,.25f,1)); hallWalls.SetTile(Cell(-1,y),Tile("wall")); }

            var prefabMap = CreatePickups(data);
            var tablePrefab = CreateTable();
            var rooms = new List<FarmingRoom2D>(); var pickups = new List<FarmingPickup>();
            var people = data.Records.Where(r=>r.Type==FarmingObjectType.Person).ToList();
            var items = data.Records.Where(r=>r.Type==FarmingObjectType.Item).ToList();
            for(int r=0;r<3;r++)
            {
                int yMin = 2+r*stride;
                var bounds = new RectInt(-width,yMin,width,height);
                Paint(floor,bounds,"floor_room");
                var root = new GameObject("Room " + (325-r)).transform; root.SetParent(environment);
                var room = root.gameObject.AddComponent<FarmingRoom2D>(); rooms.Add(room);
                var interior = new GameObject("Interior furniture").transform; interior.SetParent(root);
                var cover = Layer("Room "+(325-r)+" opaque ceiling",gridRoot,6000);
                Paint(cover,bounds,"roof"); cover.transform.localPosition=Vector3.up*1.5f;
                var walls=new List<Tilemap>();
                for(int level=0;level<3;level++)
                {
                    var wall=Layer("Room "+(325-r)+" wall tier "+level,gridRoot,FarmingDepth2D.WorldOrder); walls.Add(wall);
                    // Z is projected height metadata for sorting, not 3D geometry.
                    // A subpixel tie break draws the upper cube over its own lower cube.
                    wall.transform.localPosition=new Vector3(0,level*.5f,level*.5f+level/1024f);
                    for(int y=yMin;y<yMin+height;y++)
                    {
                        wall.SetTile(Cell(-width-1,y),Tile("wall"));
                        if(y<yMin+3||y>=yMin+6)wall.SetTile(Cell(-1,y),Tile("wall"));
                    }
                    for(int x=-width-1;x<0;x++)
                    {wall.SetTile(Cell(x,yMin-1),Tile("wall"));wall.SetTile(Cell(x,yMin+height),Tile("wall"));}
                }
                Barrier("Back wall",root,new Rect(-width-.25f,yMin,.25f,height));
                Barrier("Lower wall",root,new Rect(-width,yMin-.25f,width,.25f));
                Barrier("Upper wall",root,new Rect(-width,yMin+height,width,.25f));
                Barrier("Front before doorway",root,new Rect(-.2f,yMin,.2f,3));
                Barrier("Front after doorway",root,new Rect(-.2f,yMin+6,.2f,height-6));
                for(int y=yMin+3;y<yMin+6;y++)for(int x=-2;x<=0;x++)floor.SetTile(Cell(x,y),Tile("floor_door"));
                Label((325-r).ToString(),root,FarmingMap2D.Project(new Vector2(-1,yMin+6.8f))+Vector3.up*1.65f,.44f,6500);
                Label("←  "+(325-r),root,FarmingMap2D.Project(new Vector2(1.7f,yMin+4.5f)),.31f,-8000);
                for(int i=0;i<6;i++)
                {
                    var center=new Vector2(-5-(i%3)*6,yMin+4+(i/3)*6);
                    var table=(GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);table.transform.SetParent(interior);table.transform.position=FarmingMap2D.Project(center);
                    table.name="Table "+(i+1);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var chair=SpriteObject("Chair","chair",interior);chair.transform.position=FarmingMap2D.Project(center+new Vector2(0,side*1.7f));chair.AddComponent<FarmingDepth2D>();
                    }
                    if(r!=0)continue;
                    Spawn(people[i%people.Count],center+new Vector2(2.2f,0),0,prefabMap,pickups);
                    for(int j=0;j<2;j++)
                    {
                        var item=Spawn(items[(i*2+j)%items.Count],center+new Vector2(j==0?-.7f:.7f,0),.64f,prefabMap,pickups);
                        item.transform.SetParent(table.transform,true);
                        item.GetComponent<FarmingDepth2D>().enabled=false;
                        item.GetComponent<SortingGroup>().sortingOrder=10+j;
                    }
                }
                var so=new SerializedObject(room);so.FindProperty("footprint").rectValue=new Rect(bounds.x,bounds.y,bounds.width,bounds.height);so.ApplyModifiedPropertiesWithoutUndo();
                Set(room,"cover",cover);Set(room,"interior",interior);SetArray(room,"walls",walls.Cast<UnityEngine.Object>().ToArray());
                room.Apply(0);
            }
            var player=CreatePlayer();player.transform.position=FarmingMap2D.Project(new Vector2(3,6));
            var bunker=CreateBunker(environment);
            var hud=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"FarmingHud.prefab"))).GetComponent<FarmingHud>();
            var session=new GameObject("Farming Session").AddComponent<FarmingSession>();
            Set(session,"player",player);Set(session,"bunker",bunker);Set(session,"hud",hud);SetArray(session,"pickups",pickups.Cast<UnityEngine.Object>().ToArray());
            var map=grid.gameObject.AddComponent<FarmingMap2D>();Set(map,"floor",floor);Set(map,"player",player);Set(map,"session",session);SetArray(map,"rooms",rooms.Cast<UnityEngine.Object>().ToArray());
            var camGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camGo.tag="MainCamera";
            var camera=camGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.4f;camera.backgroundColor=FarmingIsoArt.C("192E39");camera.clearFlags=CameraClearFlags.SolidColor;
            camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=FarmingDepth2D.SortAxis;
            camGo.transform.position=new Vector3(player.transform.position.x,player.transform.position.y+1,-20);
            var follow=camGo.AddComponent<FarmingCamera>();Set(follow,"target",player.transform);Number(follow,"referenceSize",6.4f);
            ConfigureRenderer(camera);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            RenderSettings.fog=false;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,FarmingSceneBuilder.ScenePath);
            var scenes=EditorBuildSettings.scenes.ToList();if(scenes.All(s=>s.path!=FarmingSceneBuilder.ScenePath))scenes.Insert(0,new EditorBuildSettingsScene(FarmingSceneBuilder.ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(player.transform.position,Quaternion.identity,9);}
            Debug.Log("Authored 2D farming corridor, 3 rooms, 6 people and 12 items. " + FarmingSceneBuilder.ScenePath);
        }
        static Dictionary<string,GameObject> CreatePickups(FarmingDatabase db)
        {
            var result=new Dictionary<string,GameObject>();
            foreach(var row in db.Records)
            {
                var root=new GameObject(row.Id);root.AddComponent<FarmingDepth2D>();var pickup=root.AddComponent<FarmingPickup>();
                var visual=SpriteObject("Visual",row.Id,root.transform);var sprite=visual.GetComponent<SpriteRenderer>();sprite.sortingOrder=1;
                var ring=SpriteObject("Focus","focus",root.transform);ring.GetComponent<SpriteRenderer>().sortingOrder=-1;ring.SetActive(false);
                var label=Label(row.Weight+"칸",root.transform,new Vector3(0,row.Type==FarmingObjectType.Person?1.55f:.8f,0),.23f,20);
                Set(pickup,"recordId",row.Id);Set(pickup,"visual",visual.transform);Set(pickup,"sprite",sprite);Set(pickup,"standingSprite",FarmingIsoArt.Load(row.Id));
                Set(pickup,"lyingSprite",FarmingIsoArt.Load(row.Type==FarmingObjectType.Person?row.Id+"_lying":row.Id));Set(pickup,"focusMarker",ring);Set(pickup,"weightLabel",label);
                result.Add(row.Id,PrefabUtility.SaveAsPrefabAsset(root,Prefabs+row.Id+".prefab"));UnityEngine.Object.DestroyImmediate(root);
            }
            return result;
        }
        static GameObject CreateTable()
        {
            var root=SpriteObject("Table","table",null);root.AddComponent<FarmingDepth2D>();
            var collider=root.AddComponent<PolygonCollider2D>();collider.points=Polygon(new Rect(-1.55f,-.875f,3.1f,1.75f));
            var obstacle=root.AddComponent<FarmingObstacle2D>();var so=new SerializedObject(obstacle);so.FindProperty("blocksInteraction").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"Table.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static FarmingPlayer CreatePlayer()
        {
            var root=new GameObject("Player");root.AddComponent<FarmingDepth2D>();var feet=root.AddComponent<CircleCollider2D>();feet.radius=.17f;feet.isTrigger=true;
            var body=SpriteObject("Body","player_idle",root.transform);var player=root.AddComponent<FarmingPlayer>();
            Set(player,"body",body.transform);Set(player,"sprite",body.GetComponent<SpriteRenderer>());Set(player,"idleSprite",FarmingIsoArt.Load("player_idle"));Set(player,"carrySprite",FarmingIsoArt.Load("player_carry"));Set(player,"feet",feet);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"Player.prefab");UnityEngine.Object.DestroyImmediate(root);
            return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<FarmingPlayer>();
        }
        static FarmingBunker CreateBunker(Transform parent)
        {
            var root=SpriteObject("Bunker drop zone","bunker",parent);root.transform.position=FarmingMap2D.Project(new Vector2(3,1.8f));root.GetComponent<SpriteRenderer>().sortingOrder=-8000;
            var collider=root.AddComponent<PolygonCollider2D>();collider.points=Polygon(new Rect(-1.8f,-1.6f,3.6f,3.2f));collider.isTrigger=true;
            var marker=SpriteObject("Ready glow","focus",root.transform);marker.transform.localScale=Vector3.one*1.8f;marker.GetComponent<SpriteRenderer>().sortingOrder=-7999;marker.SetActive(false);
            var landing=new GameObject("Landing").transform;landing.SetParent(root.transform,false);
            var bunker=root.AddComponent<FarmingBunker>();Set(bunker,"depositZone",collider);Set(bunker,"landingPoint",landing);Set(bunker,"readyMarker",marker);
            Label("BUNKER  ·  E",root.transform,new Vector3(0,-.65f,0),.28f,-7998);
            return bunker;
        }
        static FarmingPickup Spawn(FarmingRecord row,Vector2 point,float height,Dictionary<string,GameObject> prefabs,List<FarmingPickup> list)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[row.Id]);go.transform.position=FarmingMap2D.Project(point);var item=go.GetComponent<FarmingPickup>();Number(item,"displayHeight",height);list.Add(item);
            // Prevent an initial-frame reveal before the data loader and room controller initialize.
            foreach(var sprite in go.GetComponentsInChildren<SpriteRenderer>())sprite.color=new Color(1,1,1,0);
            foreach(var label in go.GetComponentsInChildren<TMP_Text>()){var c=label.color;c.a=0;label.color=c;}
            return item;
        }
        static void ConfigureRenderer(Camera camera)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(!pipeline)return;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Farming/FarmingRenderer.asset");
            var so=new SerializedObject(pipeline);var array=so.FindProperty("m_RendererDataList");
            for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).objectReferenceValue==renderer){camera.GetUniversalAdditionalCameraData().SetRenderer(i);return;}
            throw new InvalidOperationException("Existing FarmingRenderer must remain registered.");
        }
        static Tile Tile(string name)
        {
            if(tiles.TryGetValue(name,out var tile))return tile;
            string path=Tiles+name+".asset";tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(!tile){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
            tile.sprite=FarmingIsoArt.Load(name);tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;EditorUtility.SetDirty(tile);tiles.Add(name,tile);return tile;
        }
        static Tilemap Layer(string name,Transform parent,int order)
        {
            var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);
            var map=go.GetComponent<Tilemap>();map.tileAnchor=new Vector3(.5f,.5f,0);
            var renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=order;
            renderer.mode=order==FarmingDepth2D.WorldOrder?TilemapRenderer.Mode.Individual:TilemapRenderer.Mode.Chunk;return map;
        }
        static Vector3Int Cell(int u,int v) => new Vector3Int(v,-u-1,0);
        static void Paint(Tilemap map,RectInt area,string tile){for(int y=area.yMin;y<area.yMax;y++)for(int x=area.xMin;x<area.xMax;x++)map.SetTile(Cell(x,y),Tile(tile));}
        static Vector2[] Polygon(Rect area)=>new[]{(Vector2)FarmingMap2D.Project(new Vector2(area.xMin,area.yMin)),(Vector2)FarmingMap2D.Project(new Vector2(area.xMax,area.yMin)),(Vector2)FarmingMap2D.Project(new Vector2(area.xMax,area.yMax)),(Vector2)FarmingMap2D.Project(new Vector2(area.xMin,area.yMax))};
        static void Barrier(string name,Transform parent,Rect area){var go=new GameObject(name,typeof(PolygonCollider2D),typeof(FarmingObstacle2D));go.transform.SetParent(parent,false);go.GetComponent<PolygonCollider2D>().points=Polygon(area);}
        static GameObject SpriteObject(string name,string sprite,Transform parent){var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=FarmingIsoArt.Load(sprite);renderer.spriteSortPoint=SpriteSortPoint.Pivot;return go;}
        static TMP_Text Label(string text,Transform parent,Vector3 position,float size,int order)
        {
            var label=new GameObject("Label "+text,typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.transform.SetParent(parent,false);label.transform.position=parent.position+position;
            label.font=font;label.text=text;label.fontSize=size*10;label.alignment=TextAlignmentOptions.Center;label.color=FarmingIsoArt.C("F4EBD9");label.rectTransform.sizeDelta=new Vector2(4,1);label.GetComponent<MeshRenderer>().sortingOrder=order;return label;
        }
        static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Set(UnityEngine.Object target,string field,string value){var so=new SerializedObject(target);so.FindProperty(field).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Number(UnityEngine.Object target,string field,float value){var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void SetArray(UnityEngine.Object target,string field,UnityEngine.Object[] values){var so=new SerializedObject(target);var array=so.FindProperty(field);array.arraySize=values.Length;for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
