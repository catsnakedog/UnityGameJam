using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace ShelterRun.Editor
{
    public static class FarmingIsoVerification
    {
        static readonly List<string> checks=new List<string>();
        static Keyboard keyboard;
        static InputSettings.BackgroundBehavior background;
        static InputSettings.EditorInputBehaviorInPlayMode inputBehavior;
        static string rulesBefore,dbBefore;
        static bool inputOwned;
        public static string Status { get; private set; } = "Not run";
        public static void Start()
        {
            var session=UnityEngine.Object.FindFirstObjectByType<FarmingSession>();
            if(!EditorApplication.isPlaying||!session||session.Phase!=FarmingPhase.Ready)throw new InvalidOperationException("Farming Play must be Ready.");
            checks.Clear();Status="Running";
            rulesBefore=EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath));
            dbBefore=EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath));
            background=InputSystem.settings.backgroundBehavior;inputBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            inputOwned=true;
            EditorApplication.playModeStateChanged+=OnPlayState;
            AssemblyReloadEvents.beforeAssemblyReload+=Restore;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>("Farming2DVerification");keyboard.MakeCurrent();
            session.StartCoroutine(Guard(Run(session)));
        }
        static IEnumerator Guard(IEnumerator routine)
        {
            while(true)
            {
                bool next;
                try{next=routine.MoveNext();}
                catch(Exception ex){checks.Add("FAIL: "+ex.Message);Status="Failed";Debug.LogException(ex);Restore();Write();yield break;}
                if(!next)break;yield return routine.Current;
            }
            Status="Passed";Restore();Write();Debug.Log("FARMING 2D VERIFIED: "+checks.Count);
        }
        static void Restore()
        {
            EditorApplication.playModeStateChanged-=OnPlayState;
            AssemblyReloadEvents.beforeAssemblyReload-=Restore;
            if(!inputOwned)return;
            inputOwned=false;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;
            InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=inputBehavior;
        }
        static void OnPlayState(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode)return;
            if(Status=="Running"){Status="Interrupted";Write();}
            Restore();
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add("PASS: "+message);}
        static void Write(){Directory.CreateDirectory("Docs/Farming");File.WriteAllText("Docs/Farming/Verification-2D.txt",DateTimeOffset.Now.ToString("O")+"\n"+Status+"\n"+string.Join("\n",checks));}
        static void Place(FarmingPlayer p,Vector2 logical){p.transform.position=FarmingMap2D.Project(logical);Physics2D.SyncTransforms();}
        static void Approach(FarmingPlayer p,FarmingPickup item){p.transform.position=item.transform.position+new Vector3(.01f,-.01f,0);Physics2D.SyncTransforms();p.RefreshFocus();Check(p.Focused==item,"Target "+item.RecordId);}
        static IEnumerator Run(FarmingSession s)
        {
            var map=FarmingMap2D.Instance;var p=s.Player;
            Check(map&&map.Rooms.Length==3&&map.ActiveRoom==null,"Starts in corridor with three left rooms");
            var alongHall=FarmingMap2D.Project(Vector2.up);var intoRoom=FarmingMap2D.Project(Vector2.left);
            Check(alongHall.x>0&&alongHall.y>0,"Corridor rises from lower-left toward upper-right");
            Check(intoRoom.x<0&&intoRoom.y>0,"Rooms extend to the upper-left of the corridor");
            Check(map.GetComponent<Grid>().cellLayout==GridLayout.CellLayout.Isometric,"Actual Isometric Grid / XY Tilemap");
            Check(Camera.main.transparencySortMode==TransparencySortMode.CustomAxis&&Vector3.Dot(Camera.main.transparencySortAxis.normalized,FarmingDepth2D.SortAxis.normalized)>.9999f,"Isometric depth axis cancels visual height (Y-Z)");
            Check(UnityEngine.Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None).Where(x=>x.name.Contains("wall tier")||x.name=="Corridor outer edge").All(x=>x.sortingOrder==FarmingDepth2D.WorldOrder&&x.mode==TilemapRenderer.Mode.Individual),"Wall tiles interleave with world sprites in Individual mode");
            Check(s.Pickups.Where(x=>x.Record.Type==FarmingObjectType.Item).All(x=>x.transform.parent&&x.transform.parent.GetComponent<UnityEngine.Rendering.SortingGroup>()&&!x.GetComponent<FarmingDepth2D>().enabled&&x.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder>=10),"Table-top items sort inside their table group above its surface");
            Check(UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length==0,"No 3D colliders / CharacterController");
            Check(UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Length==0,"No 3D mesh geometry in farming scene");
            foreach(var tile in AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Art/Farming/Isometric/Tiles"}))
            {
                var t=AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(tile));var sprite=t.sprite;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                Check(sprite.rect.size==new Vector2(64,64)&&sprite.pixelsPerUnit==64&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Consistent tile format: "+t.name);
            }
            Check(s.PeopleTotal==6&&s.Pickups.Count==18&&s.Remaining==60&&p.Inventory.Capacity==4,"Existing six people / twelve items / 60 sec / four slots preserved");
            Check(s.Pickups.All(x=>x.GetComponentInChildren<SpriteRenderer>().color.a==0),"All room occupants hidden in corridor");
            Place(p,new Vector2(.3f,6));p.RefreshFocus();Check(p.Focused==null,"Cannot pick up a nearby person through a closed room wall");
            s.Begin();yield return null;yield return null;float started=Time.time;
            foreach(var key in new[]{Key.W,Key.A,Key.S,Key.D})
            {
                Place(p,new Vector2(3,10));var before=p.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSeconds(.12f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                Vector3 direction=key==Key.W?Vector3.up:key==Key.S?Vector3.down:key==Key.A?Vector3.left:Vector3.right;
                Check(Vector3.Dot(p.transform.position-before,direction)>.1f,"Keyboard "+key+" moves in screen direction");
            }
            for(int r=0;r<map.Rooms.Length;r++)
            {
                float doorY=map.Rooms[r].Footprint.yMin+4.5f;
                Place(p,new Vector2(1.5f,doorY));yield return null;
                Vector2 direction=FarmingMap2D.Project(Vector2.left);direction.Normalize();
                for(int n=0;n<10;n++){p.Move(direction,.035f);yield return null;}
                Check(map.RoomAt(p.transform.position)==map.Rooms[r],"Walk through room "+r+" doorway");
                yield return new WaitForSeconds(s.Rules.RoomRevealDuration*.25f);
                Check(map.Rooms[r].Reveal>0&&map.Rooms[r].Reveal<1,"Room "+r+" wall transition is gradual");
                yield return new WaitForSeconds(s.Rules.RoomRevealDuration);
                Check(map.Rooms[r].Reveal==1&&map.Rooms.Where(x=>x!=map.Rooms[r]).All(x=>x.Reveal==0),"Only entered room "+r+" is revealed");
                for(int n=0;n<10;n++){p.Move(-direction,.035f);yield return null;}
                Check(map.RoomAt(p.transform.position)==null,"Walk back out of room "+r);
                yield return new WaitForSeconds(s.Rules.RoomRevealDuration+.1f);
                Check(map.Rooms.All(x=>x.Reveal==0),"Room "+r+" hides after leaving");
            }
            Place(p,new Vector2(1.5f,12));
            Vector2 west=FarmingMap2D.Project(Vector2.left);west.Normalize();p.Move(west,1);
            Check(FarmingMap2D.Unproject(p.transform.position).x>=0,"Front wall blocks large movement step outside doorway");
            Place(p,new Vector2(5,10));p.Move(-west,1);
            Check(FarmingMap2D.Unproject(p.transform.position).x<6,"Outer hallway wall blocks escape");
            Place(p,new Vector2(-2,12));p.Move(west,1);
            Check(FarmingMap2D.Unproject(p.transform.position).x>-3.45f,"Table Collider2D blocks movement");

            var people=s.Pickups.Where(x=>x.Record.Type==FarmingObjectType.Person).ToArray();
            var light=people.Where(x=>x.Record.Weight==2).ToArray();var medium=people.First(x=>x.Record.Weight==3);var heavy=people.First(x=>x.Record.Weight==4);
            Approach(p,light[0]);yield return new WaitForSeconds(s.Rules.RoomRevealDuration);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));yield return new WaitForSeconds(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(p.Inventory.Used==2,"E picks up two-slot person once");
            Check(light[0].transform.parent.GetComponentInParent<UnityEngine.Rendering.SortingGroup>()==p.GetComponent<UnityEngine.Rendering.SortingGroup>(),"Carried person sorts with the player group");
            var lying=light[0].GetComponentInChildren<SpriteRenderer>().sprite;
            Check(lying.rect.width>lying.rect.height,"Person switches to horizontal two-hand carry sprite");
            Approach(p,medium);p.Interact();Check(p.Inventory.Used==2&&medium.State==PickupState.Available,"2+3 capacity overflow rejected");
            Approach(p,light[1]);p.Interact();Check(p.Inventory.Used==4,"Two two-slot people fill inventory");
            Place(p,new Vector2(3,10));yield return new WaitForSeconds(s.Rules.RoomRevealDuration+.1f);
            Check(light.All(x=>x.GetComponentInChildren<SpriteRenderer>().color.a==1),"Carried people remain visible after leaving room");
            p.transform.position=s.Bunker.LandingPosition;Physics2D.SyncTransforms();p.Interact();
            Check(p.Inventory.Used==0&&s.PeopleSaved==2,"Bunker interaction deposits all carried people");
            yield return new WaitForSeconds(s.Rules.ThrowDuration+s.Rules.FadeDuration*.5f);
            float alpha=light[0].GetComponentInChildren<SpriteRenderer>().color.a;Check(alpha>0&&alpha<1,"Deposit visibly fades sprite alpha");
            yield return new WaitForSeconds(s.Rules.FadeDuration);
            Check(light.All(x=>x.State==PickupState.Deposited&&!x.gameObject.activeSelf),"Deposited people disappear");
            s.Deposit(light[0]);Check(s.PeopleSaved==2,"Duplicate deposit has no additional credit");
            Approach(p,heavy);p.Interact();Check(p.Inventory.Used==4,"Four-slot person can be carried alone");
            Place(p,new Vector2(3,14));p.RefreshFocus();Check(p.Focused==null,"Empty corridor selects ground throw");p.Interact();
            yield return new WaitForSeconds(s.Rules.ThrowDuration+.1f);
            Check(heavy.State==PickupState.Available&&map.IsFloor(heavy.transform.position)&&s.PeopleSaved==2,"Ground throw remains available on walkable floor");
            Approach(p,heavy);p.Interact();Check(p.Inventory.Used==4,"Dropped person can be picked up again");
            p.transform.position=s.Bunker.LandingPosition;Physics2D.SyncTransforms();p.Interact();
            var items=s.Pickups.Where(x=>x.Record.Type==FarmingObjectType.Item).ToArray();
            foreach(var item in items.Where(x=>x.Record.Weight==1).Take(2).Concat(items.Where(x=>x.Record.Weight==2).Take(1))){Approach(p,item);p.Interact();}
            Check(p.Inventory.Used==4,"Item combination 1+1+2 fills four slots");
            p.transform.position=s.Bunker.LandingPosition;Physics2D.SyncTransforms();p.Interact();Check(s.ItemsSaved==3&&s.PeopleSaved==3,"People and item ledger retained");
            Approach(p,medium);p.Interact();int completed=0;s.Completed+=_=>completed++;
            while(s.Phase==FarmingPhase.Running)yield return null;
            Check(Time.time-started>=59&&Time.time-started<62&&s.Remaining==0,"Real configured 60-second timer ends at zero");
            var pos=p.transform.position;p.Move(Vector2.one,1);p.Interact();s.Tick(10);
            Check(completed==1&&p.transform.position==pos&&p.Inventory.Used==3&&s.PeopleSaved==3,"Deadline freezes actions and excludes unbanked carry");
            Check(rulesBefore==EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath))&&dbBefore==EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath)),"Gameplay leaves authoritative data unchanged");
        }
    }
}
