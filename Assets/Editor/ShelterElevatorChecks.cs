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

public static class ShelterElevatorChecks
{
    public static void Move(ShelterWalker actor, ShelterArea room)
    {
        if (!actor.MoveTo(room)) throw new Exception("No route to " + room.name);
        var lifts = actor.navigation.Elevators;
        for (int i = 0; i < 4000 && actor.IsMoving; i++) { foreach (var lift in lifts) lift.Advance(0.02f); actor.Tick(0.02f); }
        actor.ResolveInitialRoom();
        if (actor.IsMoving || actor.CurrentRoom != room) throw new Exception("Did not arrive: " + actor.Status + " / " + actor.transform.position + " / " + room.name);
    }
    public static void Step(ZombieInvasionController ctrl, float dt)
    {
        if (ctrl.Simulation.IsPaused) return;
        foreach (var lift in ctrl.Navigation.Elevators) lift.Advance(dt);
        foreach (var actor in ctrl.Roster.Characters) actor.GetComponent<ShelterWalker>().Tick(dt);
        ctrl.AdvanceMovement(dt);
    }
    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required");
        var checks = new List<string>(); float scale = Time.timeScale; Time.timeScale = 0;
        void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks.Add("PASS " + label); }
        ZombieInvasionController ctrl = null;
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            ctrl = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>(); await ctrl.InitializeAsync();
            var transit = UnityEngine.Object.FindFirstObjectByType<ShelterTransit>(); await transit.InitializeAsync();
            var events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            for (int i = 0; i < 200 && !events.IsReady; i++) await Task.Delay(20);
            events.enabled = false; await Task.Delay(30);
            var nav = ctrl.Navigation; var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var humans = ctrl.Roster.Characters.ToArray(); var actors = humans.Select(c => c.GetComponent<ShelterWalker>()).ToArray();
            var rooms = ctrl.SceneRooms().ToArray();
            var guard = rooms.Single(r => r.roomType == ShelterRoomType.ZombieInvasion);
            var common = rooms.Single(r => r.roomType == ShelterRoomType.Common);
            var lower = rooms.Single(r => r.roomType == ShelterRoomType.Programming);
            var sleeping = rooms.Single(r => r.roomType == ShelterRoomType.Sleeping);
            var upperLift = nav.Elevators.OrderByDescending(l => l.Top.y).First();
            Check(transit.IsReady && nav.Elevators.Length == 2 && !ctrl.Simulation.IsPaused, "Built elevator Addressables load and scene contains two usable cabins");
            Check(UnityEngine.Object.FindObjectsByType<ShelterLadder>(FindObjectsSortMode.None).Length == 0, "No active ladder remains in actual scene");
            foreach (var from in rooms) foreach (var to in rooms) Check(nav.TryFindElevatorPath(from.ArrivalPoint(0), to, 0, 3.2f, out _), "Connected rooms " + from.displayName + " -> " + to.displayName);
            Move(actors[0], lower); Check(actors[0].CurrentRoom == lower, "Rescued character reaches lower floor by elevator");
            Move(actors[0], guard); Check(actors[0].CurrentRoom == guard, "Two-elevator transfer reaches combat floor");
            Move(actors[1], lower); actors[0].MoveTo(lower); actors[1].MoveTo(guard); actors[2].MoveTo(guard);
            for (int i = 0; i < 8000 && actors.Any(a => a.IsMoving); i++) Step(ctrl, 0.02f);
            Check(actors[0].CurrentRoom == lower && actors[1].CurrentRoom == guard && actors[2].CurrentRoom == guard, "Three opposite routes through two cabins complete without deadlock");
            Move(actors[0], sleeping); Move(actors[1], sleeping);
            Check(!actors[2].MoveTo(sleeping), "Third survivor cannot enter the occupied sleeping room");
            Move(actors[2], common);
            var boarding = actors.OrderBy(a => Mathf.Abs(a.transform.position.x - upperLift.Bottom.x)).ToArray();
            var firstRider = boarding.First(); var secondRider = boarding.Last();
            // Call from farther away first so the companion reaches the queue before boarding completes.
            secondRider.MoveTo(guard); firstRider.MoveTo(guard);
            for (int i = 0; i < 2000 && upperLift.Phase != ElevatorPhase.Travelling; i++) Step(ctrl, 0.02f);
            Check(firstRider.CurrentElevator == upperLift && secondRider.CurrentElevator == upperLift && upperLift.PassengerCount == 2,
                "Two survivors board the same visible cabin in the shelter scene");
            var p = firstRider.transform.position; var companion = secondRider.transform.position;
            float y = upperLift.CabinY; object pause = new object(); ctrl.Simulation.AcquirePause(pause);
            upperLift.Advance(10); Step(ctrl, 10);
            Check(firstRider.transform.position == p && secondRider.transform.position == companion && upperLift.CabinY == y, "Popup pause freezes cabin and both passengers"); ctrl.Simulation.ReleasePause(pause);
            firstRider.GetComponent<UnityGameJam.Characters.CharacterIdentity>().SetMental(0);
            for (int i = 0; i < 2000 && firstRider.IsInElevator; i++) Step(ctrl, 0.02f);
            Check(!firstRider.IsInElevator && firstRider.IsMovementLocked && Mathf.Abs(firstRider.transform.position.y - guard.FloorY) < 0.001f, "Fainted passenger unloads safely and remains unable to walk");
            for (int i = 0; i < 2000 && secondRider.IsMoving; i++) Step(ctrl, 0.02f);
            Check(secondRider.CurrentRoom == guard, "Companion reaches destination independently of fainted rider");
            dev.RestartDevelopment(); foreach (var h in humans) h.ResetMental();
            Move(actors[0], sleeping); Move(actors[1], sleeping); Move(actors[2], common);
            foreach (var h in humans) h.SetMental(0);
            var zombies = Enumerable.Range(0, 6).Select(_ => ctrl.SpawnWave()).ToArray();
            var entered = new List<int>(); int peakPassengers = 0; float groundGap = float.PositiveInfinity;
            double elapsed = 0;
            for (int i = 0; i < 12000 && entered.Count < zombies.Length; i++)
            {
                ZombieChecks.AdvanceApproachTicks(ctrl, ref elapsed, 0.02f);
                Step(ctrl, 0.02f);
                var ground = zombies.Where(z => !z.GetComponent<ShelterWalker>().IsInElevator).ToArray();
                for (int a = 0; a < ground.Length; a++) for (int b = a + 1; b < ground.Length; b++) groundGap = Mathf.Min(groundGap, Vector2.Distance(ground[a].transform.position, ground[b].transform.position));
                foreach (var lift in nav.Elevators)
                    peakPassengers = Mathf.Max(peakPassengers, zombies.Count(z => z.GetComponent<ShelterWalker>().CurrentElevator == lift));
                for (int j = 0; j < zombies.Length; j++) if (zombies[j].transform.position.y < guard.FloorY - 0.1f && !entered.Contains(j)) entered.Add(j);
            }
            Check(peakPassengers == 2, "Zombie queue shares cabins with a peak of two riders");
            Check(entered.SequenceEqual(Enumerable.Range(0, 6)), "Zombie descent follows spawn order [" + string.Join(", ", entered) + "]");
            Check(zombies.Select(z => z.GetInstanceID()).Distinct().Count() == 6 && zombies.All(z => z.Hp == z.InitialHp), "Elevator transport preserves six independent HP/state instances");
            Check(groundGap >= ctrl.Data.MinimumSpacing - 0.005f, "Ground zombie queue preserves spacing at elevator landings; min=" + groundGap);
            for (int i = 0; i < 3000; i++) Step(ctrl, 0.02f);
            Check(zombies.Any(z => z.State == ZombieState.Occupying), "Elevator-delivered invader still occupies and disables a room");
            ctrl.ResetRun(); transit.ResetService();
            var waiter = ctrl.SpawnWave().GetComponent<ShelterWalker>();
            var passer = ctrl.SpawnWave();
            var passerWalker = passer.GetComponent<ShelterWalker>();
            waiter.transform.position = upperLift.CallPoint(common.FloorY, -1);
            waiter.MoveTo(guard); waiter.transform.position = upperLift.WaitingPoint(waiter);
            var start = (Vector2)waiter.transform.position - Vector2.right * ctrl.Data.MinimumSpacing;
            passerWalker.transform.position = start;
            passerWalker.MoveTo(guard);
            Check(waiter.RequestedElevator == upperLift && passerWalker.RequestedElevator == upperLift &&
                ctrl.MovementFraction(passer, start, waiter.transform.position) < 0.01f, "Another boarding zombie keeps its queue spacing");
            float previousOffset = passerWalker.arrivalOffset;
            passerWalker.arrivalOffset = waiter.transform.position.x - common.Centre.x;
            Check(passerWalker.MoveTo(common) && !passerWalker.RequestedElevator &&
                ctrl.MovementFraction(passer, start, waiter.transform.position) < 0.01f, "A zombie cannot finish its ground route on top of a waiting caller");
            passerWalker.arrivalOffset = previousOffset;
            passerWalker.MoveTo(sleeping);
            for (int i = 0; i < 3000 && passerWalker.IsMoving; i++)
                passerWalker.Tick(0.02f, (from, to) => ctrl.MovementFraction(passer, from, to));
            Check(passerWalker.CurrentRoom == sleeping && !passerWalker.RequestedElevator && upperLift.QueueCount == 1,
                "Same-floor zombie walks past a waiting caller without joining or blocking the queue");
            ctrl.ResetRun(); transit.ResetService(); foreach (var h in humans) h.ResetMental();
            Check(nav.Elevators.All(l => !l.Owner && !l.Passenger && l.QueueCount == 0), "Restart removes zombie requests and elevator ownership");
            foreach (var actor in actors) Move(actor, common);
            events.enabled = true;
            string report = string.Join("\n", checks); File.WriteAllText("Docs/Elevators-checks-play.txt", report); return report;
        }
        catch (Exception e) { File.WriteAllText("Docs/Elevators-checks-play.txt", string.Join("\n", checks) + "\n" + e); throw; }
        finally { Time.timeScale = scale; }
    }
}
