using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using ShelterRun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Characters;
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ZombieChecks
{
    static readonly List<string> checks = new List<string>();
    static void Check(bool condition, string label)
    { if (!condition) throw new Exception("FAIL " + label); checks.Add("PASS " + label); }
    static string Save(string mode) { string report = string.Join("\n", checks); Directory.CreateDirectory("Docs/Zombies"); File.WriteAllText("Docs/Zombies/Verification-" + mode + ".txt", report); return report; }
    public static string RunEdit()
    {
        checks.Clear();
        var data = AssetDatabase.LoadAssetAtPath<ZombieData>(ZombieSetup.DataPath); data.Validate();
        Check(Enumerable.Range(1, 12).Select(i => data.ForWave(i).Id).SequenceEqual(Enumerable.Range(0, 12).Select(i => "zombie_" + (i / 3 + 1))), "Four Notion tiers at waves 1/4/7/10");
        Check(data.ForWave(999).Id == "zombie_4", "Final tier extends to custom longer runs");
        var rolls = new Queue<int>(new[] { 6, 1, 1, 6, 3, 3 });
        Check(ZombieCombat.Roll(data, 1, 1, _ => rolls.Dequeue()).Outcome == CombatOutcome.SurvivorWins, "Human winning dice");
        Check(ZombieCombat.Roll(data, 1, 1, _ => rolls.Dequeue()).Outcome == CombatOutcome.ZombieWins, "Zombie winning dice");
        Check(ZombieCombat.Roll(data, 1, 1, _ => rolls.Dequeue()).Outcome == CombatOutcome.Tie, "Tie remains unresolved");
        var invalid = UnityEngine.Object.Instantiate(data);
        try
        {
            var so = new SerializedObject(invalid); so.FindProperty("entries").GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "zombie_1"; so.ApplyModifiedPropertiesWithoutUndo();
            bool rejected = false; try { invalid.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Duplicate zombie ID rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(invalid); }
        return Save("edit");
    }
    static async Task Frame() { int frame = Time.frameCount; while (Time.frameCount == frame) await Task.Delay(20); }
    static void Move(CharacterIdentity actor, ShelterArea room)
    {
        var walker = actor.GetComponent<ShelterWalker>();
        if (!walker.MoveTo(room)) throw new Exception("Could not move " + actor.Id + " to " + room.displayName);
        walker.Tick(100); walker.ResolveInitialRoom();
    }
    static Func<int,int> Outcome(bool human)
    { int count = 0; return sides => (++count % 2 == 1) == human ? sides : 1; }
    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required");
        checks.Clear(); float oldScale = Time.timeScale; Time.timeScale = 0;
        ZombieInvasionController ctrl = null;
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            ctrl = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>(); await ctrl.InitializeAsync(); await Frame();
            var sim = ctrl.Simulation; var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            for (int i = 0; !events.IsReady && i < 200; i++) await Task.Delay(20);
            events.enabled = false;
            var actors = ctrl.Roster.Characters.ToArray();
            var guard = ctrl.SceneRooms().Single(a => a.roomType == ShelterRoomType.ZombieInvasion);
            var common = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Common);
            var sleeping = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Sleeping);
            var plan = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Planning);
            Check(ctrl.IsReady && actors.Length == 3 && !sim.IsPaused, "Built Addressables + rescued roster + scene wiring");
            Check(ReferenceEquals(ctrl.Data, await GameData.LoadSettingsAsync<ZombieData>(ZombieData.Address)), "Shared settings loader cache");
            void Reset()
            {
                dev.RestartDevelopment();
                foreach (var actor in actors) { actor.ResetMental(); Move(actor, sleeping); }
                ctrl.AdvanceMovement(0);
            }
            Reset(); Move(actors[0], guard); Move(actors[1], guard);
            ctrl.RollDie = Outcome(true); var z = ctrl.SpawnWave();
            Check(z.State == ZombieState.Defending && z.DefenseBudget == 6, "Two guards snapshot six resolved battles");
            Check(z.Fighter.GetComponent<ShelterWalker>().IsMovementLocked && !z.Fighter.GetComponent<ShelterWalker>().MoveTo(common), "Current fighter movement locked");
            z.ProcessTick(4); Check(z.Hp == 2 && z.DefenseAttempts == 0, "No combat before fifth tick");
            z.ProcessTick(5); Check(z.Hp == 1 && z.DefenseAttempts == 1, "Human win deals one HP");
            z.ProcessTick(10); Check(z.State == ZombieState.Dead && ctrl.Zombies.Count == 0, "Zombie death ends event");
            Check(actors[0].CurrentMental == 65 && actors[1].CurrentMental == 65 && actors[2].CurrentMental == 60, "Defense reward +5 only current guard room occupants");
            Check(actors.All(a => !a.GetComponent<ShelterWalker>().IsMovementLocked), "Death releases locks");

            Reset(); Move(actors[0], guard); ctrl.RollDie = Outcome(false); z = ctrl.SpawnWave(); z.ProcessTick(5);
            ctrl.RollDie = Outcome(true); z.ProcessTick(10); z.ProcessTick(15);
            Check(z.State == ZombieState.Dead && z.DefenseAttempts == 3 && ctrl.Zombies.Count == 0, "Kill on final allowed roll wins before breach check");

            Reset(); Move(actors[0], guard); Move(actors[1], guard); actors[0].SetMental(2); actors[1].SetMental(2);
            ctrl.RollDie = Outcome(false); z = ctrl.SpawnWave(); z.ProcessTick(5);
            Check(actors[0].CurrentMental == 0 && z.State == ZombieState.Defending, "One exhausted defender does not breach while another remains");
            z.ProcessTick(10); Check(actors[1].CurrentMental == 0 && z.State != ZombieState.Defending && z.Hp == 2, "All defenders at zero breach with remaining HP");
            Check(!actors[0].GetComponent<ShelterWalker>().MoveTo(common), "Zero mental cannot move");
            for (int i = 0; i < 39; i++) actors[0].AdvanceMentalTick();
            Check(!actors[0].CanAct, "Faint lasts full 40 ticks"); actors[0].AdvanceMentalTick();
            Check(actors[0].CurrentMental == 30 && actors[0].CanAct, "Wake restores 30 mental");
            actors[0].SetMental(999); Check(actors[0].CurrentMental == 100, "Shared mental maximum applied");

            Reset(); Move(actors[0], guard); ctrl.RollDie = _ => 3; z = ctrl.SpawnWave(); z.ProcessTick(5);
            Check(z.State == ZombieState.Defending && z.DefenseAttempts == 0 && z.Hp == 2, "Tie spends no attempt or HP");
            for (int i = 0; i < 20; i++) ctrl.AdvanceMovement(0.01f);
            Check(z.DefenseAttempts == 0, "Always-tied RNG yields each frame without hanging");
            ctrl.RollDie = Outcome(false); ctrl.AdvanceMovement(0.01f);
            Check(z.DefenseAttempts == 1, "Pending tie resolves exactly once");
            z.ProcessTick(5); Check(z.DefenseAttempts == 2 && z.State == ZombieState.Defending, "Still defending before n times three");
            z.ProcessTick(10); Check(z.DefenseAttempts == 3 && z.State != ZombieState.Defending, "Breach immediately at n times three, no extra fourth roll");

            Reset(); Move(actors[0], guard); ctrl.RollDie = Outcome(true); z = ctrl.SpawnWave(); z.ProcessTick(5);
            ctrl.RollDie = Outcome(false); z.ProcessTick(10); z.ProcessTick(15);
            Check(z.State == ZombieState.Invading && z.Hp == 1, "Partial damage survives defense-to-invasion transition");
            var fighter = actors[0]; fighter.SetMental(60);
            fighter.GetComponent<ShelterWalker>().MoveTo(common);
            fighter.GetComponent<ShelterWalker>().Tick(1.3f);
            var existingDestination = fighter.GetComponent<ShelterWalker>().Destination;
            for (int i = 0; i < 300 && z.State != ZombieState.Encounter; i++) ctrl.AdvanceMovement(0.05f);
            Check(z.State == ZombieState.Encounter && z.Fighter == fighter, "Invader intercepts physical guard in its route");
            int beforeMental = fighter.CurrentMental; z.ProcessTick(30);
            Check(z.State == ZombieState.Invading && fighter.CurrentMental == beforeMental - 2, "Forced battle resolves once then resumes invasion");
            Check(!fighter.GetComponent<ShelterWalker>().IsMovementLocked && fighter.GetComponent<ShelterWalker>().Destination == existingDestination, "Encounter releases actor without discarding its order");

            Reset(); foreach (var actor in actors) actor.SetMental(0);
            z = ctrl.SpawnWave(); var target = z.TargetRoom;
            Check(z.State == ZombieState.Invading && target && target != guard, "Empty combat room chooses connected noncombat target");
            bool climbed = false;
            for (int i = 0; i < 1500 && z.State != ZombieState.Occupying; i++)
            { ctrl.AdvanceMovement(0.05f); climbed |= z.GetComponent<ShelterWalker>().IsClimbing; }
            Check(climbed && z.State == ZombieState.Occupying && target.IsOccupied && target.IsWalkable && !target.IsOperational, "Existing ladder graph reaches target and occupies without blocking entry");
            actors[0].SetMental(60); Move(actors[0], target);
            Check(!actors[0].GetComponent<ShelterWalker>().CanWorkInCurrentRoom, "Occupied room stops work");
            ctrl.RollDie = Outcome(true); ctrl.AdvanceMovement(0.05f);
            Check(z.State == ZombieState.Encounter, "Entering occupied room automatically starts combat");
            sim.AcquirePause(checks); var position = z.transform.position; int hp = z.Hp; z.ProcessTick(100); ctrl.AdvanceMovement(10);
            Check(z.Hp == hp && z.transform.position == position, "Popup-style simulation pause freezes combat and movement"); sim.ReleasePause(checks);
            z.ProcessTick(5); z.ProcessTick(10);
            Check(z.State == ZombieState.Dead && !target.IsOccupied && target.IsOperational && actors[0].GetComponent<ShelterWalker>().CanWorkInCurrentRoom, "Clearing occupant restores room functionality");
            Check(actors[0].CurrentMental == 65, "Invasion kill rewards combat participant once");
            object a = new object(), b = new object(); target.SetOccupied(a, true); target.SetOccupied(b, true); target.SetOccupied(a, false);
            Check(target.IsOccupied, "One removal cannot clear another occupier"); target.SetOccupied(b, false);
            Check(!target.IsOccupied, "Last occupier restores room");

            Reset(); Move(actors[0], plan); float progress = dev.Progress.Planning;
            plan.SetOccupied(checks, true); sim.Advance(sim.Rules.SecondsPerTick);
            Check(dev.Progress.Planning == progress, "Real work tick produces no progress in occupied planning room");
            plan.SetOccupied(checks, false); sim.Advance(sim.Rules.SecondsPerTick);
            Check(dev.Progress.Planning > progress, "Real work tick resumes after liberation");

            Reset(); sim.SetPendingWaveHpModifier(4); sim.Advance(sim.Rules.InvasionEveryTicks * sim.Rules.SecondsPerTick);
            Check(ctrl.Zombies.Count == 1 && ctrl.Zombies[0].Hp == 6 && sim.PendingWaveHpModifier == 0, "Scheduled wave consumes existing NEXT_WAVE_HP exactly once");
            sim.Advance(sim.Rules.InvasionEveryTicks * sim.Rules.SecondsPerTick);
            Check(ctrl.Zombies.Count == 2 && ctrl.Zombies[1].InitialHp == 2, "Next wave returns to catalog HP and independent agents coexist");
            dev.RestartDevelopment();
            Check(ctrl.Zombies.Count == 0 && ctrl.Wave == 0 && ctrl.SceneRooms().All(r => !r.IsOccupied) && actors.All(c => c.CurrentMental == 60), "Restart clears zombies, occupation, locks, wave and mental state");

            Reset(); var ladder = ctrl.Navigation.Ladders.OrderByDescending(l => l.Top.y).First(); ladder.gameObject.SetActive(false); ctrl.Navigation.Refresh();
            z = ctrl.SpawnWave(); Check(z.State == ZombieState.WaitingForRoute, "Disconnected layout waits without teleporting");
            ladder.gameObject.SetActive(true); ctrl.Navigation.Refresh(); ctrl.AdvanceMovement(0.05f);
            Check(z.State == ZombieState.Invading && z.TargetRoom, "Reconnecting designer ladder automatically restores route");
            dev.FinishDevelopment(); Check(ctrl.Zombies.Count == 0 && actors.All(c => !c.GetComponent<ShelterWalker>().IsMovementLocked), "Ending clears live invasion safely");
            dev.RestartDevelopment();
            ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1);
            foreach (var actor in actors) Move(actor, common);
            events.enabled = true;
            return Save("play");
        }
        catch (Exception e) { checks.Add(e.ToString()); Save("play"); throw; }
        finally { if (ctrl) ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1); Time.timeScale = oldScale; }
    }
}
