using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace ShelterRun.Editor
{
    public static class FarmingSideViewVerification
    {
        static readonly List<string> checks=new List<string>();
        static Keyboard keyboard;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldInput;
        static bool oldRun,owned;
        static FarmingPlayer waitingPlayer;
        static bool waitingPlayerEnabled;
        static string rulesBefore,dbBefore;
        public static string Status{get;private set;}="Not run";
        [MenuItem("Tools/Farming/Verify Side View Play")]
        public static void Start()
        {
            var s=UnityEngine.Object.FindFirstObjectByType<FarmingSession>();
            if(!EditorApplication.isPlaying||!s||s.Phase!=FarmingPhase.Ready)throw new InvalidOperationException("Fresh Farming Play must be Ready.");
            checks.Clear();Status="Running";
            rulesBefore=EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath));dbBefore=EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath));
            oldBackground=InputSystem.settings.backgroundBehavior;oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;oldRun=Application.runInBackground;owned=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>("FarmingSideViewVerification");keyboard.MakeCurrent();EditorApplication.playModeStateChanged+=PlayState;AssemblyReloadEvents.beforeAssemblyReload+=Restore;
            s.StartCoroutine(Guard(Run(s)));
        }
        static IEnumerator Guard(IEnumerator routine)
        {
            while(true)
            {
                bool next;try{next=routine.MoveNext();}catch(Exception error){Status="Failed";checks.Add("FAIL: "+error);Restore();Write();Debug.LogException(error);yield break;}
                if(!next)break;yield return routine.Current;
            }
            Status="Passed";Restore();Write();Debug.Log("Farming side view verified: "+checks.Count);
        }
        static void Restore()
        {
            if(waitingPlayer)waitingPlayer.enabled=waitingPlayerEnabled;
            waitingPlayer=null;
            EditorApplication.playModeStateChanged-=PlayState;AssemblyReloadEvents.beforeAssemblyReload-=Restore;if(!owned)return;owned=false;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;Application.runInBackground=oldRun;
        }
        static void PlayState(PlayModeStateChange state){if(state!=PlayModeStateChange.ExitingPlayMode)return;if(Status=="Running"){Status="Interrupted";Write();}Restore();}
        static void Write(){File.WriteAllText("Docs/Farming/Verification-SideView.txt",DateTimeOffset.Now.ToString("O")+"\n"+Status+"\n"+string.Join("\n",checks));}
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add("PASS: "+message);}
        static void Place(FarmingPlayer p,Vector3 position){p.Teleport(position);p.RefreshFocus();}
        static void TravelTo(FarmingPlayer player, FarmingRoom2D destination)
        {
            var map = FarmingMap2D.Instance;
            if (map.ActiveArea == destination) return;
            if (map.ActiveRoom) Door(player, map.Doors.Single(d => d.Area == map.ActiveRoom));
            if (destination != map.Corridor) Door(player, map.Doors.Single(d => d.Area == map.Corridor && d.Target.Area == destination));
        }
        static void Pick(FarmingPlayer p,FarmingPickup item){TravelTo(p,FarmingMap2D.Instance.AreaAt(item.transform.position));Place(p,item.transform.position);Check(p.Focused==item,"Focus "+item.RecordId);p.Interact();}
        static void Door(FarmingPlayer p,FarmingDoor2D door){Place(p,door.transform.position);p.Interact();Check(FarmingMap2D.Instance.ActiveArea==door.Target.Area,"Door reaches "+door.DestinationLabel);}
        static IEnumerator KeyPress(Key key,float duration=.1f){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSeconds(duration);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        public static string CheckMovement()
        {
            var s=UnityEngine.Object.FindFirstObjectByType<FarmingSession>();if(!s||!s.CanAct)throw new InvalidOperationException("Running Farming required");
            var p=s.Player;var map=FarmingMap2D.Instance;var area=map.ActiveArea;var original=p.transform.position;
            try
            {
                Place(p,area.Constrain(new Vector3((area.Left+area.Right)*.5f,area.GroundY,0)));var start=p.transform.position;p.Move(Vector2.up,1);p.Move(Vector2.down,1);Check(p.transform.position==start,"W/S cannot leave horizontal walk line");
                p.Move(Vector2.right,100);Check(p.transform.position.x<area.Right&&p.transform.position.y==area.GroundY,"Large step stops before right boundary");
                var edge=p.transform.position;p.Move(Vector2.left,.1f);Check(p.transform.position.x<edge.x-.5f,"Can leave right wall immediately");
                p.Move(Vector2.left,100);Check(p.transform.position.x>area.Left&&p.transform.position.y==area.GroundY,"Large step stops before left boundary");
                p.transform.position=new Vector3(area.Left-5,area.GroundY+10,0);p.Move(Vector2.zero,0);Check(p.transform.position.x>area.Left&&p.transform.position.y==area.GroundY,"Invalid overlap/position recovers onto safe walk line");
                return "5 movement checks passed";
            }
            finally{Place(p,original);}
        }
        static IEnumerator Run(FarmingSession s)
        {
            var map=FarmingMap2D.Instance;var p=s.Player;
            Check(map.ActiveArea==map.Corridor&&map.ActiveRoom==null,"Starts in corridor");
            Check(map.Rooms.Length==3&&map.Doors.Length==6,"Three rooms with paired entrance/exit doors");
            Check(map.Doors.All(d=>d.Target&&d.Target.Target==d&&map.AreaAt(d.Arrival)==d.Area),"Door targets and arrival points are reciprocal and valid");
            Check(Camera.main.orthographic&&Camera.main.transparencySortMode==TransparencySortMode.Orthographic,"True orthographic side view");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None).Length==0,"No isometric tilemaps in scene");
            Check(UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length==0,"No 3D collision geometry");
            Check(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).All(x=>!AssetDatabase.GetAssetPath(x.sprite).Contains("Isometric")),"All world sprites use side-view artwork");
            int placedCount = s.Pickups.Count;
            var db = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);
            Check(s.PeopleTotal==6 && p.Inventory.Capacity==4,"Six people and four carry slots");
            Check(s.Pickups.Count == db.Records.Count + map.Rooms.Length - 1 && db.Records.All(row =>
                s.Pickups.Count(x => x.RecordId == row.Id) == (row.Id == FarmingDataKeys.SpaghettiCan ? map.Rooms.Length : 1)),
                "Every roster ID spawns once, with one can per room");
            var entrances = map.Doors.Where(d => d.Area == map.Corridor).OrderBy(d => Mathf.Abs(d.transform.position.x - s.Bunker.LandingPosition.x)).ToArray();
            for (int i = 0; i < entrances.Length; i++)
            {
                var room = entrances[i].Target.Area;
                var tables = room.GetComponentsInChildren<FarmingSpawnTable>().OrderBy(t => t.transform.position.x).ToArray();
                var aisles = tables.Zip(tables.Skip(1), (a,b) => (a.transform.position.x + b.transform.position.x) * .5f).ToArray();
                var inRoom = s.Pickups.Where(x => map.RoomAt(x.transform.position) == room).ToArray();
                var peopleInRoom = inRoom.Where(x => x.Record.Type == FarmingObjectType.Person).ToArray();
                Check(peopleInRoom.Length == s.Rules.RoomSpawns[i].People && peopleInRoom.All(x => aisles.Contains(x.transform.position.x)), room.name + " person quota and aisle placement");
                Check(inRoom.Count(x => x.RecordId == FarmingDataKeys.SpaghettiCan) == 1 && inRoom.Where(x => x.Record.Type == FarmingObjectType.Item).All(x =>
                    tables.SelectMany(t => t.TabletopSlots).Any(slot => Vector3.Distance(slot.position, x.AimPosition) < .001f)), room.name + " can and items on tabletops");
                Check(inRoom.Select(x => x.transform.position.x).Distinct().Count() == inRoom.Length, room.name + " unique reachable positions");
            }
            float duration = AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath).Duration;
            Check(s.Rules.Duration == duration && s.Remaining == duration,
                $"Ready timer matches the saved FarmingRules duration ({duration}s)");
            Check(s.Pickups.All(x=>x.GetComponentInChildren<SpriteRenderer>().color.a==0),"Room contents hidden from corridor");
            float started = Time.time;
            s.Begin();yield return null;yield return null;
            foreach(var key in new[]{Key.D,Key.A})
            {
                Place(p,new Vector3(5,map.Corridor.GroundY,0));var before=p.transform.position;yield return KeyPress(key);
                Check((key==Key.D?p.transform.position.x-before.x:before.x-p.transform.position.x)>.2f&&p.transform.position.y==before.y,"Keyboard "+key+" moves horizontally");
            }
            CheckMovement();
            foreach(var entrance in map.Doors.Where(d=>d.Area==map.Corridor).ToArray())
            {
                Place(p,entrance.transform.position);yield return new WaitForSeconds(.15f);Check(map.ActiveArea==map.Corridor,"Walking to door does not auto-enter "+entrance.DestinationLabel);
                float remaining=s.Remaining;yield return KeyPress(Key.E);Check(map.ActiveArea==entrance.Target.Area,"E enters "+entrance.DestinationLabel);Check(s.Remaining<remaining,"Timer continues through door");
                yield return new WaitForSeconds(s.Rules.RoomRevealDuration+.05f);Check(map.ActiveArea.Reveal==1&&map.Corridor.Reveal==0&&map.Rooms.Where(r=>r!=map.ActiveArea).All(r=>r.Reveal==0),"Only current room is visible");
                CheckMovement();Door(p,entrance.Target);Check(map.ActiveArea==map.Corridor,"Room exit returns to corridor");
            }
            var first=map.Doors.First(d=>d.Area==map.Corridor);Door(p,first);
            var people=s.Pickups.Where(x=>x.Record.Type==FarmingObjectType.Person).ToArray();var light=people.Where(x=>x.Record.Weight==2).ToArray();var medium=people.First(x=>x.Record.Weight==3);var heavy=people.First(x=>x.Record.Weight==4);
            Pick(p,light[0]);Check(p.Inventory.Used==2&&light[0].State==PickupState.Carried,"Two-slot person carried");
            Check(light[0].GetComponentInChildren<SpriteRenderer>().sprite.rect.width>light[0].GetComponentInChildren<SpriteRenderer>().sprite.rect.height,"Person carried horizontally");
            Place(p,new Vector3(10,map.ActiveArea.GroundY,0));var carryStart=p.transform.position;yield return KeyPress(Key.D);Check(p.transform.position.x>carryStart.x+.2f&&p.Inventory.Used==2,"Actual keyboard movement while carrying");
            float baseline=p.transform.position.y;for(int i=0;i<200;i++)p.Move(Vector2.right,.02f);Check(p.transform.position.x>map.ActiveArea.Right-1&&p.transform.position.y==baseline,"Walks in front of all tables without getting trapped");
            TravelTo(p,map.Corridor);yield return new WaitForSeconds(.1f);Check(p.Inventory.Used==2&&light[0].transform.IsChildOf(p.transform)&&light[0].GetComponentInChildren<SpriteRenderer>().color.a==1,"Door preserves held person and visibility");
            Pick(p,medium);Check(p.Inventory.Used==2&&medium.State==PickupState.Available,"Capacity overflow still rejected");Pick(p,light[1]);Check(p.Inventory.Used==4,"Two people fill four slots");TravelTo(p,map.Corridor);
            Place(p,s.Bunker.LandingPosition);p.Interact();Check(p.Inventory.Used==0&&s.PeopleSaved==2,"Bunker takes all carried people");
            yield return new WaitForSeconds(s.Rules.ThrowDuration+s.Rules.FadeDuration*.5f);float alpha=light[0].GetComponentInChildren<SpriteRenderer>().color.a;Check(alpha>0&&alpha<1,"Deposit fades out");yield return new WaitForSeconds(s.Rules.FadeDuration);Check(light.All(x=>x.State==PickupState.Deposited&&!x.gameObject.activeSelf),"Deposited people disappear");
            Pick(p,heavy);Check(p.Inventory.Used==4,"Four-slot person can be carried");TravelTo(p,map.Corridor);Place(p,new Vector3(13,0,0));p.Move(Vector2.right,.01f);p.Interact();
            Check(heavy.State==PickupState.InFlight&&p.Inventory.Used==0,"Empty corridor interaction throws held target");
            var second=map.Doors.First(d=>d.Area==map.Corridor&&d!=first);Door(p,second);yield return new WaitForSeconds(s.Rules.ThrowDuration+.1f);
            Check(heavy.State==PickupState.Available&&map.AreaAt(heavy.transform.position)==map.Corridor,"Thrown target stays in source area when player changes rooms");
            Check(!map.CanReach(p.transform.position,heavy.transform.position),"Cannot interact with another area");Door(p,second.Target);Pick(p,heavy);Place(p,s.Bunker.LandingPosition);p.Interact();Check(s.PeopleSaved==3,"Dropped person can be retrieved and deposited");
            var items=s.Pickups.Where(x=>x.Record.Type==FarmingObjectType.Item).ToArray();foreach(var item in items.Where(x=>x.Record.Weight==1).Take(2).Concat(items.Where(x=>x.Record.Weight==2).Take(1)))Pick(p,item);
            Check(p.Inventory.Used==4,"Item combination fills four slots");TravelTo(p,map.Corridor);Check(p.Inventory.Used==4,"Door preserves full item inventory");Place(p,s.Bunker.LandingPosition);p.Interact();Check(s.ItemsSaved==3,"Items reach bunker");
            Door(p,first);Check(light.All(x=>x.State==PickupState.Deposited)&&s.Pickups.Count==placedCount,"Re-entering room does not respawn looted targets");Pick(p,medium);int completed=0;s.Completed+=_=>completed++;
            // Keep the real timer running, but prevent unrelated keyboard input from
            // changing the expected inventory during the long deadline observation.
            waitingPlayer=p;waitingPlayerEnabled=p.enabled;p.enabled=false;
            while(s.Phase==FarmingPhase.Running)yield return null;
            p.enabled=waitingPlayerEnabled;waitingPlayer=null;
            float elapsed = Time.time - started;
            Check(elapsed >= duration - 1 && elapsed < duration + 2 && s.Remaining == 0,
                $"Configured {duration}s flow finishes at zero after {elapsed:F2}s");
            var end=p.transform.position;p.Move(Vector2.right,1);p.Interact();s.Tick(1);Check(p.transform.position==end&&completed==1&&p.Inventory.Used==3,"Deadline freezes movement/interactions and excludes unbanked carry");
            Check(!map.Traverse(first.Target),"Doors disabled after deadline");
            Check(rulesBefore==EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingRules>(FarmingCsvImporter.RulesPath))&&dbBefore==EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath)),"Authoritative farming data unchanged");
        }
    }
}
