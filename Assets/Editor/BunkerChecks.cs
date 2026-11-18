using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using ShelterRun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class BunkerChecks
{
    static void Check(bool condition, string label, List<string> checks)
    { if (!condition) throw new Exception(label); checks.Add("PASS " + label); }
    public static string RunEdit()
    {
        var checks = new List<string>();
        var original = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        var originalSettings = EditorJsonUtility.ToJson(original);
        var rules = UnityEngine.Object.Instantiate(original);
        try
        {
            var clock = new BunkerTickClock(rules);
            var ticks = new List<int>(); clock.Tick += ticks.Add;
            clock.Advance(rules.SecondsPerTick * 0.5);
            Check(ticks.Count == 0, "No early tick", checks);
            clock.Paused = true; clock.Advance(999);
            Check(ticks.Count == 0, "Pause discards elapsed time", checks);
            clock.Paused = false; clock.Advance(rules.SecondsPerTick * 0.5);
            Check(ticks.SequenceEqual(new[] { 1 }), "Fractional time retained", checks);
            clock.Advance((double)rules.SecondsPerTick * rules.TotalTicks * 2);
            Check(ticks.SequenceEqual(Enumerable.Range(1, rules.TotalTicks)), "Catch-up exact ordered ticks, no overrun", checks);
            clock.Advance(999); Check(ticks.Count == rules.TotalTicks, "Completion emitted once", checks);
            bool rejected = false;
            try { clock.Advance(double.NaN); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Non-finite time rejected", checks);
            var so = new SerializedObject(rules); so.FindProperty("secondsPerTick").floatValue = 0.25f; so.ApplyModifiedPropertiesWithoutUndo();
            clock = new BunkerTickClock(rules); clock.Advance(1);
            Check(clock.CurrentTick == 4, "SO tempo changes ticks without code edits", checks);
            var cost = so.FindProperty("mentalCostPerWork");
            cost.intValue = -1; so.ApplyModifiedPropertiesWithoutUndo();
            rejected = false; try { rules.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Negative work mental cost rejected", checks);
            cost.intValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); rules.Validate();
            Check(rules.MentalCostPerWork == 0, "Zero work mental cost supported", checks);
            so.Update(); so.FindProperty("workEveryTicks").intValue = 0; so.ApplyModifiedPropertiesWithoutUndo();
            rejected = false; try { rules.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Zero interval rejected", checks);
            Check(EditorJsonUtility.ToJson(original) == originalSettings, "Original SO unchanged by tests", checks);
        }
        finally { UnityEngine.Object.DestroyImmediate(rules); }
        return string.Join("\n", checks);
    }
    static void CheckRescue(BunkerSimulation sim, ShelterRoster roster, ShelterArea[] rooms, List<string> checks)
    {
        var patient = roster.Characters[0];
        var fallen = patient.GetComponent<ShelterWalker>();
        var carrier = roster.Characters[1].GetComponent<ShelterWalker>();
        var guard = rooms.Single(room => room.roomType == ShelterRoomType.ZombieInvasion);
        var bed = rooms.Single(room => room.roomType == ShelterRoomType.Sleeping);
        var lower = rooms.Single(room => room.roomType == ShelterRoomType.Programming);
        var commander = UnityEngine.Object.FindFirstObjectByType<ShelterCommander>();
        ShelterElevatorChecks.Move(fallen, guard);
        ShelterElevatorChecks.Move(carrier, lower);
        patient.SetMental(0);
        for (int i = 0; i < 41; i++) patient.AdvanceMentalTick();
        patient.SetMental(sim.Rules.WakeMental);
        Check(!patient.CanAct && fallen.NeedsRescue, "Elapsed ticks and external mental gain cannot replace a rescue", checks);
        patient.SetMental(0);
        Check(commander.ClickWorld(carrier.transform.position + Vector3.up) && commander.Selected == carrier &&
            commander.ClickWorld(fallen.transform.position + Vector3.up) && carrier.IsRescuing, "Selecting a survivor and clicking the patient starts a rescue", checks);
        var spare = roster.Characters[2].GetComponent<ShelterWalker>();
        Check(!spare.TryRescue(fallen) && !carrier.MoveTo(lower), "Only one rescuer can claim the patient", checks);
        bool carriedInLift = false;
        for (int i = 0; i < 6000 && carrier.IsRescuing; i++)
        {
            foreach (var lift in carrier.navigation.Elevators) lift.Advance(.02f);
            carrier.Tick(.02f);
            if (fallen.IsBeingCarried && carrier.IsInElevator && carrier.CurrentElevator.PassengerCount == 2 && Vector3.Distance(fallen.transform.position, carrier.transform.position) < 1f)
                carriedInLift = true;
        }
        Check(carriedInLift && patient.IsRestingAfterRescue && !patient.CanAct && fallen.CurrentRoom == bed && carrier.CurrentRoom == bed,
            "The rescuer picks up the patient, carries them in the elevator and delivers them to the sleeping room", checks);
        sim.AcquirePause(checks);
        sim.Advance(sim.Rules.SecondsPerTick);
        sim.ReleasePause(checks);
        Check(patient.CurrentMental == 0, "Bed rest observes the shared pause", checks);
        int ticksToWake = Mathf.CeilToInt((float)sim.Rules.WakeMental / sim.Rules.SleepMentalPerTick);
        for (int i = 1; i < ticksToWake; i++)
        {
            sim.Advance(sim.Rules.SecondsPerTick);
            Check(!patient.CanAct && patient.CurrentMental == i * sim.Rules.SleepMentalPerTick, "Still unconscious after bed tick " + i, checks);
        }
        sim.Advance(sim.Rules.SecondsPerTick);
        Check(patient.CanAct && !patient.IsRestingAfterRescue && patient.CurrentMental >= sim.Rules.WakeMental,
            "The delivered survivor regains control at the threshold", checks);
    }

    static void CheckRoomCapacity(BunkerSimulation sim, ShelterRoster roster, ShelterArea[] rooms, List<string> checks)
    {
        var common = rooms.Single(room => room.roomType == ShelterRoomType.Common);
        var bed = rooms.Single(room => room.roomType == ShelterRoomType.Sleeping);
        var guard = rooms.Single(room => room.roomType == ShelterRoomType.ZombieInvasion);
        var walkers = roster.Characters.Select(actor => actor.GetComponent<ShelterWalker>()).ToArray();
        var carrier = walkers[0]; var patient = walkers[1]; var resident = walkers[2];
        foreach (var walker in walkers) ShelterElevatorChecks.Move(walker, common);
        var positions = walkers.Select(w => w.transform.position.x).OrderBy(x => x).ToArray();
        Check(positions.Skip(1).Zip(positions, (right, left) => right - left).All(gap => gap > .96f),
            "Three survivors stand at separately clickable positions in one room", checks);
        Check(sim.Rules.SleepingRoomCapacity == 2 && resident.MoveTo(bed) && patient.MoveTo(bed) && !carrier.MoveTo(bed),
            "Two simultaneous sleeping-room orders reserve the beds before arrival", checks);
        patient.Stop();
        Check(carrier.MoveTo(bed), "Stopping a trip immediately releases its sleeping-room place", checks);
        carrier.Stop();
        ShelterElevatorChecks.Move(resident, bed);
        ShelterElevatorChecks.Move(patient, guard);
        ShelterElevatorChecks.Move(carrier, common);
        roster.Characters[1].SetMental(0);
        Check(carrier.TryRescue(patient), "A rescue can use the single remaining bed", checks);
        carrier.Stop();
        Check(carrier.MoveTo(bed), "Cancelling pickup releases the patient's reservation", checks);
        ShelterElevatorChecks.Move(carrier, common);
        Check(carrier.TryRescue(patient), "The cancelled rescue can be requested again", checks);
        for (int i = 0; i < 6000 && (carrier.IsRescuing || carrier.IsMoving); i++)
        {
            foreach (var lift in carrier.navigation.Elevators) lift.Advance(.02f);
            carrier.Tick(.02f);
        }
        Check(patient.CurrentRoom == bed && resident.CurrentRoom == bed && carrier.CurrentRoom == common &&
            roster.Characters[1].IsRestingAfterRescue && !carrier.MoveTo(bed) &&
            Mathf.Abs(patient.transform.position.x - resident.transform.position.x) > .96f,
            "Last bed takes the patient beside the resident and the carrier returns to their original room", checks);
        roster.Characters[1].ResetMental();
        foreach (var walker in walkers) ShelterElevatorChecks.Move(walker, common);
    }

    public static async Task<string> RunPlay()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play mode required.");
        var checks = new List<string>();
        float previousScale = Time.timeScale; Time.timeScale = 0;
        EventPopupController events = null;
        try
        {
            await GameData.InitializeAsync();
            var shared = await GameData.LoadSettingsAsync<BunkerRules>(BunkerRules.Address);
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            var load = SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            while (!load.isDone) await Task.Yield();
            var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
            await sim.InitializeAsync();
            await UnityEngine.Object.FindFirstObjectByType<ShelterTransit>().InitializeAsync();
            events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            events.enabled = false; // Drive ticks without a random event waiting for player input.
            await UnityEngine.Object.FindFirstObjectByType<UnityGameJam.Zombies.ZombieInvasionController>().InitializeAsync();
            var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>();
            Check(sim.IsReady && roster.Characters.Count == 3, "Addressables settings, character catalog and scene wiring loaded", checks);
            Check(ReferenceEquals(shared, sim.Rules), "Common loader shares settings instance", checks);
            var rooms = UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Where(a => a.kind == ShelterArea.AreaKind.Room).ToArray();
            var prog = rooms.Single(a => a.roomType == ShelterRoomType.Programming);
            var art = rooms.Single(a => a.roomType == ShelterRoomType.Graphics);
            var plan = rooms.Single(a => a.roomType == ShelterRoomType.Planning);
            var actor = roster.Characters[0]; var walker = actor.GetComponent<ShelterWalker>();
            int initialMental = actor.CurrentMental;
            int cost = sim.Rules.MentalCostPerWork;
            double workSeconds = (double)sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks;
            Check(walker.MoveTo(prog) && walker.CurrentRoom == null, "Moving order clears membership immediately", checks);
            sim.Advance(sim.Rules.SecondsPerTick);
            Check(dev.Progress.Programming == 0 && actor.CurrentMental == initialMental, "Moving produces no work or mental cost", checks);
            ShelterElevatorChecks.Move(walker, prog);
            Check(walker.CurrentRoom == prog && walker.CanWorkInCurrentRoom, "Arrival establishes work membership", checks);
            dev.RestartDevelopment();
            Check(sim.CurrentTick == 0, "Development restart resets clock", checks);
            dev.SetProgress(GameDevField.Planning, sim.Rules.PlanningUnlockPercent);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(dev.Progress.Programming == 0 && actor.CurrentMental == initialMental, "Locked programming costs no mental", checks);
            dev.SetProgress(GameDevField.Planning, sim.Rules.PlanningUnlockPercent + 0.1f);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Programming, actor.Definition.Programming * sim.Rules.PercentPerAbility), "Programming ability contributes proportional percentage", checks);
            Check(actor.CurrentMental == Mathf.Max(0, initialMental - cost), "Programming charges its worker once", checks);
            ShelterElevatorChecks.Move(walker, art);
            int beforeArt = actor.CurrentMental;
            sim.Advance(workSeconds / 2);
            Check(actor.CurrentMental == beforeArt, "No cost before the next work interval", checks);
            sim.Advance(workSeconds / 2);
            Check(Mathf.Approximately(dev.Progress.Graphics, actor.Definition.Art * sim.Rules.PercentPerAbility), "Graphics uses art ability", checks);
            Check(actor.CurrentMental == Mathf.Max(0, beforeArt - cost), "Art charges the same flat cost", checks);
            dev.SetProgress(GameDevField.Graphics, GameDevProgress.MaximumPercent);
            beforeArt = actor.CurrentMental;
            sim.Advance(workSeconds);
            Check(actor.CurrentMental == beforeArt, "Completed field costs no mental", checks);
            dev.SetProgress(GameDevField.Planning, 0);
            ShelterElevatorChecks.Move(walker, plan); sim.Advance(workSeconds);
            Check(Mathf.Approximately(dev.Progress.Planning, actor.Definition.Planning * sim.Rules.PercentPerAbility), "Planning uses planning ability", checks);
            var second = roster.Characters[1].GetComponent<ShelterWalker>(); ShelterElevatorChecks.Move(second, plan);
            dev.RestartDevelopment(); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            float expected = (actor.Definition.Planning + roster.Characters[1].Definition.Planning) * sim.Rules.PercentPerAbility;
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Multiple members sum once per actor", checks);
            Check(roster.Characters.Take(2).All(a => a.CurrentMental == Mathf.Max(0, a.Definition.Mental - cost)) &&
                roster.Characters[2].CurrentMental == roster.Characters[2].Definition.Mental, "Only the two workers pay", checks);
            int beforeIdle = actor.CurrentMental;
            plan.SetOccupied(checks, true); sim.Advance(workSeconds); plan.SetOccupied(checks, false);
            Check(actor.CurrentMental == beforeIdle && Mathf.Approximately(dev.Progress.Planning, expected), "Occupied room suspends work and cost", checks);
            plan.accessible = false; sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Closed room excluded", checks); plan.accessible = true;
            actor.gameObject.SetActive(false); second.gameObject.SetActive(false);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Inactive actors excluded", checks);
            actor.gameObject.SetActive(true); second.gameObject.SetActive(true);
            foreach (var type in new[] { ShelterRoomType.Common, ShelterRoomType.Sleeping, ShelterRoomType.ZombieInvasion })
            {
                int beforeRoom = actor.CurrentMental;
                plan.roomType = type;
                sim.Advance(sim.Rules.SecondsPerTick);
                int roomDelta = type == ShelterRoomType.Sleeping ? sim.Rules.SleepMentalPerTick :
                    type == ShelterRoomType.ZombieInvasion ? -sim.Rules.GuardMentalLossPerTick : 0;
                Check(Mathf.Approximately(dev.Progress.Planning, expected) && actor.CurrentMental == actor.ClampMental(beforeRoom + roomDelta),
                    "Room changes mental without producing development: " + type, checks);
            }
            plan.roomType = ShelterRoomType.Planning;
            if (cost > 0)
            {
                actor.SetMental(1);
                sim.Advance(workSeconds);
                Check(actor.IsFainted && !actor.CanAct && actor.CurrentMental == 0, "Last work interval can exhaust a worker", checks);
                float beforeFaint = dev.Progress.Planning;
                sim.Advance(sim.Rules.SecondsPerTick);
                float partnerGain = sim.Rules.WorkEveryTicks == 1 ? roster.Characters[1].Definition.Planning * sim.Rules.PercentPerAbility : 0;
                Check(!actor.CanAct && Mathf.Approximately(dev.Progress.Planning, beforeFaint + partnerGain), "Fainted worker contributes no progress", checks);
            }
            dev.RestartDevelopment();
            Check(actor.CanAct && actor.CurrentMental == initialMental, "Restart restores initial mental", checks);
            await Task.Yield(); // Let the input block from the old event controller finish this frame.
            CheckRescue(sim, roster, rooms, checks);
            CheckRoomCapacity(sim, roster, rooms, checks);
            ShelterElevatorChecks.Move(walker, plan);
            ShelterElevatorChecks.Move(second, plan);
            dev.RestartDevelopment();
            int random = 0, invasion = 0, presentation = 0, work = 0, end = 0;
            sim.RandomEventDue += _ => random++; sim.ZombieInvasionDue += _ => invasion++;
            sim.PresentationDue += (_, score, grade) => presentation++;
            sim.WorkProcessed += _ => work++; sim.DurationEnded += () => end++;
            sim.Paused = true; sim.Advance(1000); Check(sim.CurrentTick == 0 && actor.CurrentMental == initialMental, "Pause suspends cost and ticks", checks); sim.Paused = false;
            sim.Advance((double)sim.Rules.SecondsPerTick * (sim.Rules.TotalTicks - 1));
            actor.ResetMental(); roster.Characters[1].ResetMental();
            float finalGain = sim.Rules.TotalTicks % sim.Rules.WorkEveryTicks == 0 ? expected : 0;
            float finalPlanning = Mathf.Min(GameDevProgress.MaximumPercent, dev.Progress.Planning + finalGain);
            sim.Advance((double)sim.Rules.SecondsPerTick * sim.Rules.TotalTicks * 2);
            Check(sim.CurrentTick == sim.Rules.TotalTicks && end == 1, "Ends at N exactly once", checks);
            var playableTicks = Enumerable.Range(1, sim.Rules.TotalTicks - 1);
            Check(random == playableTicks.Count(t => t % sim.Rules.RandomEventEveryTicks == 0 && !sim.Rules.IsPresentationTick(t)) &&
                presentation == playableTicks.Count(sim.Rules.IsPresentationTick) && invasion == playableTicks.Count(sim.Rules.IsInvasionTick) &&
                work == sim.Rules.TotalTicks / sim.Rules.WorkEveryTicks,
                "Presentations replace random events, scheduled popups and waves stop before the final work tick", checks);
            Check(dev.Phase == GameDevPhase.Ended && dev.TotalScore.HasValue && Mathf.Approximately(dev.SubmittedProgress.Planning, finalPlanning), "Final tick work submitted, evaluated and clamped through existing manager", checks);
            sim.Advance(1000); Check(end == 1 && sim.RemainingTicks == 0, "No progress after end", checks);
            var source = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
            Check(sim.Rules.TotalTicks == source.TotalTicks && sim.Rules.WorkEveryTicks == source.WorkEveryTicks &&
                sim.Rules.PercentPerAbility == source.PercentPerAbility && sim.Rules.MentalCostPerWork == source.MentalCostPerWork &&
                sim.Rules.GuardMentalLossPerTick == source.GuardMentalLossPerTick && sim.Rules.SleepMentalPerTick == source.SleepMentalPerTick &&
                sim.Rules.WakeMental == source.WakeMental, "Built work and room mental settings match the authored SO", checks);
        }
        finally { if (events) events.enabled = true; Time.timeScale = previousScale; }
        string result = string.Join("\n", checks);
        System.IO.File.WriteAllText("Docs/Bunker-play.txt", result);
        return result;
    }
}
