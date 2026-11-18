using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShelterRun.Editor
{
    // Run on a fresh Farming Play session, after Begin and at least one player frame.
    public static class FarmingMovementVerification
    {
        public static string Run()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<FarmingSession>();
            if (!EditorApplication.isPlaying || !session || !session.CanAct || session.Player.Inventory.Used != 0)
                throw new InvalidOperationException("Start a fresh Farming run before checking movement.");
            var player = session.Player;
            var feet = player.GetComponent<CircleCollider2D>();
            var checks = new List<string>();
            var obstacles = UnityEngine.Object.FindObjectsByType<FarmingObstacle2D>(FindObjectsSortMode.None)
                .Select(x => x.GetComponent<Collider2D>()).ToArray();
            bool playerEnabled = player.enabled, sessionEnabled = session.enabled;
            player.enabled = false; session.enabled = false;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks.Add("PASS: " + message);
            }
            void Place(Vector2 logical)
            {
                player.transform.position = FarmingMap2D.Project(logical);
                Physics2D.SyncTransforms();
            }
            bool Clear()
            {
                Physics2D.SyncTransforms();
                return FarmingMap2D.Instance.IsFloor(player.transform.position) &&
                    obstacles.All(x => feet.Distance(x).distance >= -.0001f);
            }
            void FourDirections(string label)
            {
                foreach (var direction in new[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right })
                {
                    Place(new Vector2(3, 10));
                    Vector3 before = player.transform.position;
                    player.Move(direction, .08f);
                    Check(Vector2.Dot((Vector2)(player.transform.position - before), direction) > .4f && Clear(), label + " moves " + direction);
                }
            }
            void CarryAndRecover(FarmingPickup[] items, string label)
            {
                foreach (var item in items)
                {
                    player.transform.position = item.transform.position;
                    Physics2D.SyncTransforms();
                    player.Interact();
                    Check(item.State == PickupState.Carried, label + " picks up " + item.RecordId);
                }
                Check(player.Inventory.Used == items.Sum(x => x.Record.Weight), label + " preserves capacity accounting");
                Place(new Vector2(-5, 6));
                Check(!Clear(), label + " starts overlapping table");
                player.Move(Vector2.up, .02f);
                Check(Clear() && items.All(x => x.State == PickupState.Carried), label + " escapes table while carrying");
                FourDirections(label);
                player.transform.position = session.Bunker.LandingPosition;
                Physics2D.SyncTransforms(); player.Interact();
                Check(player.Inventory.Used == 0, label + " deposits normally");
            }
            try
            {
                FourDirections("Empty hands");
                foreach (var point in new[] {
                    new Vector2(-5,6), new Vector2(-11,6), new Vector2(-17,6),
                    new Vector2(-5,12), new Vector2(-11,12), new Vector2(-17,12),
                    new Vector2(-.1f,12), new Vector2(-21.95f,10),
                    new Vector2(-21.95f,2.05f), new Vector2(5.95f,10) })
                {
                    Place(point); Check(!Clear(), "Reproduces overlap at " + point);
                    player.Move(Vector2.zero, 0);
                    Check(Clear(), "Recovers without input at " + point);
                }
                Place(new Vector2(3,10)); player.Move(Vector2.zero,0);
                var safe = player.transform.position;
                Place(new Vector2(6.2f,10)); player.Move(Vector2.right,.02f);
                Check(Clear() && player.transform.position == safe, "Outer-wall penetration returns to last valid floor position");

                Vector2 intoRoom = ((Vector2)FarmingMap2D.Project(Vector2.left)).normalized;
                Place(new Vector2(1.5f,12)); player.Move(intoRoom,1);
                Check(FarmingMap2D.Unproject(player.transform.position).x >= 0 && Clear(), "Large step cannot cross front wall");
                Vector3 atWall = player.transform.position;
                player.Move(-intoRoom,.1f);
                Check(Vector2.Dot((Vector2)(player.transform.position-atWall),-intoRoom) > .5f && Clear(), "Can immediately back away from wall contact");
                Place(new Vector2(-2,12)); player.Move(intoRoom,1);
                Check(FarmingMap2D.Unproject(player.transform.position).x > -3.45f && Clear(), "Large step cannot cross table");
                Vector3 atTable = player.transform.position;
                player.Move(-intoRoom,.1f);
                Check(Vector2.Dot((Vector2)(player.transform.position-atTable),-intoRoom) > .5f && Clear(), "Can immediately back away from table contact");

                var people = session.Pickups.Where(x => x.Record.Type == FarmingObjectType.Person).ToArray();
                foreach (int weight in new[] {2,3,4})
                    CarryAndRecover(new[] {people.First(x => x.Record.Weight == weight)}, weight + "-slot person");
                var items = session.Pickups.Where(x => x.Record.Type == FarmingObjectType.Item).ToArray();
                CarryAndRecover(new[] {items.First(x => x.Record.Weight == 1)}, "One-slot item");
                CarryAndRecover(new[] {items.First(x => x.Record.Weight == 2)}, "Two-slot item");
                CarryAndRecover(items.Where(x => x.State == PickupState.Available && x.Record.Weight == 1).Take(2)
                    .Concat(items.Where(x => x.State == PickupState.Available && x.Record.Weight == 2).Take(1)).ToArray(), "Full item inventory");

                var random = new System.Random(325);
                for (int region=0;region<3;region++)
                {
                    Place(region==0?new Vector2(-3,6):region==1?new Vector2(.5f,8.5f):new Vector2(5.5f,10));
                    for (int step=0;step<600;step++)
                    {
                        float angle = (float)(random.NextDouble()*Math.PI*2);
                        player.Move(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)), step%20==0?.35f:.016f);
                        if (!Clear()) throw new InvalidOperationException("Penetration during repeated contacts, region " + region + " step " + step);
                    }
                }
                Check(true,"1800 varied movement steps near tables, doorway and corridor wall stay on floor without penetration");
                Place(new Vector2(3,6));
                var report = DateTimeOffset.Now.ToString("O") + "\nPassed\n" + string.Join("\n",checks);
                File.WriteAllText("Docs/Farming/Verification-Movement.txt", report);
                return checks.Count + " movement checks passed";
            }
            catch(Exception error)
            {
                File.WriteAllText("Docs/Farming/Verification-Movement.txt",DateTimeOffset.Now.ToString("O")+"\nFailed\n"+string.Join("\n",checks)+"\nFAIL: "+error);
                throw;
            }
            finally { player.enabled = playerEnabled; session.enabled = sessionEnabled; }
        }
    }
}
