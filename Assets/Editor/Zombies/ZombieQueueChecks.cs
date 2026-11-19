using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShelterRun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ZombieQueueChecks
{
    static readonly List<string> results = new List<string>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); results.Add("PASS " + label); }
    static string Save(string mode) { string report = string.Join("\n", results); File.WriteAllText("Docs/Zombies/Verification-queue-" + mode + ".txt", report); return report; }
    static float Gap(ZombieInvasionController ctrl)
    {
        float min = float.PositiveInfinity;
        var alive = ctrl.Zombies.Where(z => z && z.isActiveAndEnabled && z.State != ZombieState.Dead && !z.GetComponent<ShelterWalker>().IsInElevator).ToArray();
        for (int i = 0; i < alive.Length; i++) for (int j = i + 1; j < alive.Length; j++) min = Mathf.Min(min, Vector2.Distance(alive[i].transform.position, alive[j].transform.position));
        return min;
    }
    public static string RunEdit()
    {
        results.Clear();
        Check(Mathf.Abs(ZombieTraffic.SafeFraction(Vector2.zero, Vector2.right * 10, Vector2.right * 5, 2) - 0.3f) < 0.0001f, "Horizontal sweep clips before another zombie");
        Check(Mathf.Abs(ZombieTraffic.SafeFraction(Vector2.zero, Vector2.up * 10, Vector2.up * 5, 2) - 0.3f) < 0.0001f, "Vertical sweep keeps clearance");
        Check(ZombieTraffic.SafeFraction(Vector2.zero, Vector2.left * 10, Vector2.right * 2, 2) == 1, "Touching zombie may move away");
        Check(ZombieTraffic.SafeFraction(Vector2.zero, Vector2.right * 1000, Vector2.right * 2, 2) == 0, "Large step cannot tunnel through touching blocker");
        Check(ZombieTraffic.SafeFraction(Vector2.zero, Vector2.right * 10, Vector2.up * 3, 2) == 1, "Unrelated lane does not block");
        var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ZombieData>(ZombieSetup.DataPath));
        try
        {
            var so = new SerializedObject(data); so.FindProperty("minimumSpacing").floatValue = 0; so.ApplyModifiedPropertiesWithoutUndo();
            bool rejected = false; try { data.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Zero spacing data rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
        return Save("edit");
    }
    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required.");
        results.Clear(); float scale = Time.timeScale; Time.timeScale = 0;
        ZombieInvasionController ctrl = null;
        var originalTypes = new Dictionary<ShelterArea, ShelterRoomType>();
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            ctrl = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>(); await ctrl.InitializeAsync(); await UnityEngine.Object.FindFirstObjectByType<ShelterTransit>().InitializeAsync();
            var events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            for (int i = 0; i < 200 && !events.IsReady; i++) await Task.Delay(20);
            events.enabled = false; await Task.Delay(30);
            var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var hero = ctrl.Roster.Characters.Single(); var human = hero.GetComponent<ShelterWalker>();
            var guard = ctrl.SceneRooms().Single(r => r.roomType == ShelterRoomType.ZombieInvasion);
            var common = ctrl.SceneRooms().Single(r => r.roomType == ShelterRoomType.Common);
            void Move(ShelterArea room) { if (!human.MoveTo(room)) throw new Exception("Human route failed"); ShelterElevatorChecks.Move(human, room); }
            void Reset() { dev.RestartDevelopment(); hero.ResetMental(); Move(common); ctrl.AdvanceMovement(0); }
            Reset(); Move(guard);
            var z = Enumerable.Range(0, 3).Select(_ => ctrl.SpawnWave()).ToArray();
            Check(z.Distinct().Count() == 3 && z.All(a => a.Hp == 2), "Three separate instances with independent HP");
            Check(z.All(a => a.State == ZombieState.Approaching) && Gap(ctrl) >= ctrl.Data.MinimumSpacing - 0.001f, "Three approaching zombies spawn in a spaced queue");
            var positions = z.Select(a => a.transform.position.x).ToArray();
            var outside = Mathf.Sign(positions[0] - z[0].Doorway.x);
            int die = 0; ctrl.RollDie = sides => ++die % 2 == 0 ? sides : 1;
            int period = ctrl.Simulation.Rules.GuardVolleyEveryTicks;
            int travel = ctrl.Simulation.Rules.InvasionApproachTicks;
            ctrl.Simulation.Advance(travel * ctrl.Simulation.Rules.SecondsPerTick);
            for (int i = 0; i < z.Length; i++)
                Check(Mathf.Abs(outside * (positions[i] - z[i].transform.position.x) - travel * ctrl.Simulation.Rules.SecondsPerTick * ctrl.Data.WalkSpeed) < 0.001f, "Every approaching zombie walks at the same speed");
            Check(z[0].Hp == 1 && z[1].Hp == 2 && z[2].Hp == 2 && hero.CurrentMental == hero.Definition.Mental - travel * ctrl.Simulation.Rules.GuardMentalLossPerTick, "A guard shoots only the front target, and waiting zombies do not retaliate");
            ctrl.Simulation.Advance(period * ctrl.Simulation.Rules.SecondsPerTick);
            Check(ctrl.Zombies.SequenceEqual(z.Skip(1)) && z[1].Hp == 2 && z[2].Hp == 2, "Killing the front never spills that shot into the queue");

            Reset(); hero.SetMental(0);
            // All six share one route. Same-floor passers are allowed to cross an unrelated elevator queue.
            var destination = ctrl.SceneRooms().Single(r => r.roomType == ShelterRoomType.Planning);
            foreach (var room in ctrl.SceneRooms().Where(r => r != guard && r != destination))
            {
                originalTypes.Add(room, room.roomType);
                room.roomType = ShelterRoomType.ZombieInvasion;
            }
            z = Enumerable.Range(0, 6).Select(_ => ctrl.SpawnWave()).ToArray();
            var entered = new List<int>(); float minimum = Gap(ctrl);
            double elapsed = 0;
            void Step(float dt)
            {
                ZombieChecks.AdvanceApproachTicks(ctrl, ref elapsed, dt);
                foreach (var lift in ctrl.Navigation.Elevators) lift.Advance(dt); ctrl.AdvanceMovement(dt); minimum = Mathf.Min(minimum, Gap(ctrl));
                for (int i = 0; i < z.Length; i++)
                    if (z[i] && z[i].transform.position.y < guard.FloorY - 0.1f && !entered.Contains(i)) entered.Add(i);
            }
            for (int i = 0; i < 500 && !z[0].GetComponent<ShelterWalker>().RequestedElevator; i++) Step(0.04f);
            Check(z[0].GetComponent<ShelterWalker>().RequestedElevator && !z[0].GetComponent<ShelterWalker>().IsInElevator, "First invader calls elevator before boarding");
            object stop = new object(); z[0].GetComponent<ShelterWalker>().AcquireMovementLock(stop);
            var front = z[0].transform.position;
            for (int i = 0; i < 400; i++) Step(0.04f);
            Check(z[0].transform.position == front && z.Skip(1).All(a => outside * (a.transform.position.x - front.x) > 0), "Locked front blocks followers at call pad without overtaking");
            Check(minimum >= ctrl.Data.MinimumSpacing - 0.002f, "Six zombies maintain minimum spacing through spawn and elevator call pad");
            z[0].GetComponent<ShelterWalker>().ReleaseMovementLock(stop);
            for (int i = 0; i < 2500 && entered.Count < z.Length; i++) Step(0.04f);
            Check(entered.SequenceEqual(Enumerable.Range(0, z.Length)), "All six enter elevator shaft in original spawn order");
            for (int i = 0; i < 600; i++) Step(0.04f);
            Check(ctrl.Zombies.Count == 6 && z.All(a => a.Hp == a.InitialHp), "Six independent zombies persist after navigation");
            Check(minimum >= ctrl.Data.MinimumSpacing - 0.002f && z.Any(a => a.State == ZombieState.Occupying), "Occupied rooms and ground queues remain separated; minimum=" + minimum);
            Step(10); Check(Gap(ctrl) >= ctrl.Data.MinimumSpacing - 0.002f, "Large frame catch-up cannot merge agents");
            var before = z.Select(a => a.transform.position).ToArray(); ctrl.Simulation.AcquirePause(stop); Step(10);
            Check(z.Select(a => a.transform.position).SequenceEqual(before), "Pause freezes entire queue"); ctrl.Simulation.ReleasePause(stop);
            hero.ResetMental(); Move(z[0].TargetRoom); ctrl.AdvanceMovement(0.04f);
            die = 0; z[0].ProcessTick(5); z[0].ProcessTick(10);
            Check(z[0].State == ZombieState.Dead && ctrl.Zombies.Count == 5 && ctrl.Zombies.All(a => a.Hp == a.InitialHp), "Killing one occupant leaves five separate intact zombies");
            Step(2); Check(Gap(ctrl) >= ctrl.Data.MinimumSpacing - 0.002f, "Queue resumes safely when a blocker dies");
            foreach (var pair in originalTypes) pair.Key.roomType = pair.Value;
            Reset(); var first = ctrl.SpawnWave();
            Check(ctrl.Zombies.Count == 1 && first.State == ZombieState.Approaching && ctrl.Wave == 1, "Restart removes old queue and traffic blockers");
            ctrl.ResetRun(); hero.ResetMental(); ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1); events.enabled = true;
            return Save("play");
        }
        catch (Exception e) { results.Add(e.ToString()); Save("play"); throw; }
        finally
        {
            foreach (var pair in originalTypes) if (pair.Key) pair.Key.roomType = pair.Value;
            if (ctrl) ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1);
            Time.timeScale = scale;
        }
    }
}

