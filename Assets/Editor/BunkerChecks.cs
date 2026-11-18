using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using ShelterRun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            so.Update(); so.FindProperty("workEveryTicks").intValue = 0; so.ApplyModifiedPropertiesWithoutUndo();
            rejected = false; try { rules.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Zero interval rejected", checks);
            Check(original.SecondsPerTick == 1 && original.WorkEveryTicks == 1, "Original SO unchanged by tests", checks);
        }
        finally { UnityEngine.Object.DestroyImmediate(rules); }
        return string.Join("\n", checks);
    }
    public static async Task<string> RunPlay()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play mode required.");
        var checks = new List<string>();
        float previousScale = Time.timeScale; Time.timeScale = 0;
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            var load = SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            while (!load.isDone) await Task.Yield();
            var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
            await sim.InitializeAsync();
            var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>();
            Check(sim.IsReady && roster.Characters.Count == 3, "Addressables settings, character catalog and scene wiring loaded", checks);
            var shared = await GameData.LoadSettingsAsync<BunkerRules>(BunkerRules.Address);
            Check(ReferenceEquals(shared, sim.Rules), "Common loader shares settings instance", checks);
            var rooms = UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Where(a => a.kind == ShelterArea.AreaKind.Room).ToArray();
            var prog = rooms.Single(a => a.roomType == ShelterRoomType.Programming);
            var art = rooms.Single(a => a.roomType == ShelterRoomType.Graphics);
            var plan = rooms.Single(a => a.roomType == ShelterRoomType.Planning);
            var actor = roster.Characters[0]; var walker = actor.GetComponent<ShelterWalker>();
            Check(walker.MoveTo(prog) && walker.CurrentRoom == null, "Moving order clears membership immediately", checks);
            sim.Advance(sim.Rules.SecondsPerTick);
            Check(dev.Progress.Programming == 0, "Destination alone produces no work", checks);
            walker.Tick(100);
            Check(walker.CurrentRoom == prog && walker.CanWorkInCurrentRoom, "Arrival establishes work membership", checks);
            dev.RestartDevelopment();
            Check(sim.CurrentTick == 0, "Development restart resets clock", checks);
            dev.SetProgress(GameDevField.Planning, sim.Rules.PlanningUnlockPercent);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(dev.Progress.Programming == 0, "Planning exactly at threshold keeps programming locked", checks);
            dev.SetProgress(GameDevField.Planning, sim.Rules.PlanningUnlockPercent + 0.1f);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Programming, actor.Definition.Programming * sim.Rules.PercentPerAbility), "Programming ability contributes proportional percentage", checks);
            walker.MoveTo(art); walker.Tick(100); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Graphics, actor.Definition.Art * sim.Rules.PercentPerAbility), "Graphics uses art ability", checks);
            dev.SetProgress(GameDevField.Planning, 0);
            walker.MoveTo(plan); walker.Tick(100); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Planning, actor.Definition.Planning * sim.Rules.PercentPerAbility), "Planning uses planning ability", checks);
            var second = roster.Characters[1].GetComponent<ShelterWalker>(); second.MoveTo(plan); second.Tick(100);
            dev.RestartDevelopment(); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            float expected = (actor.Definition.Planning + roster.Characters[1].Definition.Planning) * sim.Rules.PercentPerAbility;
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Multiple members sum once per actor", checks);
            plan.accessible = false; sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Closed room excluded", checks); plan.accessible = true;
            actor.gameObject.SetActive(false); second.gameObject.SetActive(false);
            sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(Mathf.Approximately(dev.Progress.Planning, expected), "Inactive actors excluded", checks);
            actor.gameObject.SetActive(true); second.gameObject.SetActive(true);
            foreach (var type in new[] { ShelterRoomType.Common, ShelterRoomType.Sleeping, ShelterRoomType.ZombieInvasion })
            {
                plan.roomType = type; sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
                Check(Mathf.Approximately(dev.Progress.Planning, expected), "Non-work room excluded: " + type, checks);
            }
            plan.roomType = ShelterRoomType.Planning;
            dev.RestartDevelopment();
            int random = 0, invasion = 0, work = 0, end = 0;
            sim.RandomEventDue += _ => random++; sim.ZombieInvasionDue += _ => invasion++;
            sim.WorkProcessed += _ => work++; sim.DurationEnded += () => end++;
            sim.Paused = true; sim.Advance(1000); Check(sim.CurrentTick == 0, "Simulation pause", checks); sim.Paused = false;
            sim.Advance((double)sim.Rules.SecondsPerTick * sim.Rules.TotalTicks * 2);
            Check(sim.CurrentTick == sim.Rules.TotalTicks && end == 1, "Ends at N exactly once", checks);
            Check(random == sim.Rules.TotalTicks / sim.Rules.RandomEventEveryTicks && invasion == sim.Rules.TotalTicks / sim.Rules.InvasionEveryTicks && work == sim.Rules.TotalTicks / sim.Rules.WorkEveryTicks, "Independent event/work periods including final tick", checks);
            Check(dev.Phase == GameDevPhase.Ended && dev.TotalScore.HasValue && Mathf.Approximately(dev.SubmittedProgress.Planning, Mathf.Min(100, expected * work)), "Final tick work submitted, evaluated and clamped through existing manager", checks);
            sim.Advance(1000); Check(end == 1 && sim.RemainingTicks == 0, "No progress after end", checks);
            Check(sim.Rules.TotalTicks == 120 && sim.Rules.PercentPerAbility == 0.1f, "Runtime preserves settings", checks);
        }
        finally { Time.timeScale = previousScale; }
        string result = string.Join("\n", checks);
        System.IO.File.WriteAllText("Docs/Bunker-play.txt", result);
        return result;
    }
}
