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
        ShelterElevatorChecks.Move(walker, room);
    }
    static void Advance(ZombieInvasionController ctrl, float dt)
    {
        foreach (var lift in ctrl.Navigation.Elevators) lift.Advance(dt);
        ctrl.AdvanceMovement(dt);
    }
    // Traffic-only fixtures drive the approach clock without scheduling additional waves.
    public static void AdvanceApproachTicks(ZombieInvasionController ctrl, ref double elapsed, float seconds)
    {
        if (!ctrl.CanAdvance) return;
        double duration = ctrl.Simulation.Rules.SecondsPerTick;
        int first = (int)(elapsed / duration) + 1;
        elapsed += seconds;
        for (int tick = first; tick <= (int)(elapsed / duration); tick++)
            foreach (var zombie in ctrl.Zombies.ToArray())
                if (zombie.State == ZombieState.Approaching) zombie.ProcessTick(tick);
    }
    static Func<int,int> Outcome(bool human)
    { int count = 0; return sides => (++count % 2 == 0) == human ? sides : 1; }
    static Queue<int> ScriptRolls(ZombieInvasionController ctrl, params int[] dice)
    {
        var rolls = new Queue<int>(dice);
        ctrl.RollDie = _ => rolls.Dequeue();
        return rolls;
    }
    static async Task CheckSharedOccupation(ZombieInvasionController ctrl, ShelterArea guard, ShelterArea room, CharacterIdentity[] actors)
    {
        foreach (var actor in actors) actor.SetMental(0);
        // Make both invaders choose the same reachable end room, regardless of the random seed.
        var otherRooms = ctrl.SceneRooms().Where(r => r != guard && r != room).ToDictionary(r => r, r => r.roomType);
        ZombieAgent first, second;
        try
        {
            foreach (var other in otherRooms.Keys) other.roomType = ShelterRoomType.ZombieInvasion;
            first = ctrl.SpawnWave();
            second = ctrl.SpawnWave();
            double elapsed = 0;
            for (int i = 0; i < 3000 && second.State != ZombieState.Occupying; i++)
            {
                AdvanceApproachTicks(ctrl, ref elapsed, 0.05f);
                Advance(ctrl, 0.05f);
            }
        }
        finally { foreach (var pair in otherRooms) pair.Key.roomType = pair.Value; }
        Check(first.State == ZombieState.Occupying && second.State == ZombieState.Occupying && first.TargetRoom == room && second.TargetRoom == room, "Two live invaders occupy the same room");
        foreach (var actor in actors) { actor.ResetMental(); Move(actor, room); }
        Advance(ctrl, 0.05f);
        int secondHp = second.Hp;
        var dice = ScriptRolls(ctrl, 1, 6, 6, 6, 6);
        ctrl.Simulation.Advance(ctrl.Data.CombatEveryTicks * ctrl.Simulation.Rules.SecondsPerTick);
        await Frame(); // Check Unity objects and their renderers after deferred Destroy has executed.
        Check(!first && dice.Count == 0 && ctrl.Zombies.SequenceEqual(new[] { second }) && second.isActiveAndEnabled &&
            second.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled) && second.Hp == secondHp && second.ResolvedCombats == 0,
            "Four attacks kill only the engaged occupant; the other remains visible with its own HP and no attack spillover");
        Check(room.IsOccupied && !room.IsOperational, "The surviving invader retains its occupation");
        dice = ScriptRolls(ctrl, 1, 6, 6, 6, 6);
        ctrl.Simulation.Advance(ctrl.Data.CombatEveryTicks * ctrl.Simulation.Rules.SecondsPerTick);
        Check(dice.Count == 0 && ctrl.Zombies.Count == 0 && room.IsOperational, "The group can fight the second invader on the next round and liberate the room");
    }
    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play required");
        checks.Clear(); float oldScale = Time.timeScale; Time.timeScale = 0;
        ZombieInvasionController ctrl = null;
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03", "person_04" }, AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            ctrl = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>(); await ctrl.InitializeAsync(); await UnityEngine.Object.FindFirstObjectByType<ShelterTransit>().InitializeAsync(); await Frame();
            var sim = ctrl.Simulation; var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            for (int i = 0; !events.IsReady && i < 200; i++) await Task.Delay(20);
            events.enabled = false;
            var actors = ctrl.Roster.Characters.ToArray();
            var guard = ctrl.SceneRooms().Single(a => a.roomType == ShelterRoomType.ZombieInvasion);
            var common = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Common);
            var sleeping = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Sleeping);
            var plan = ctrl.SceneRooms().First(a => a.roomType == ShelterRoomType.Planning);
            Check(ctrl.IsReady && actors.Length == 4 && !sim.IsPaused, "Built Addressables + four rescued survivors + scene wiring");
            Check(ReferenceEquals(ctrl.Data, await GameData.LoadSettingsAsync<ZombieData>(ZombieData.Address)), "Shared settings loader cache");
            void Reset()
            {
                dev.RestartDevelopment();
                foreach (var actor in actors) { actor.ResetMental(); Move(actor, common); }
                Advance(ctrl, 0);
            }
            Reset();
            var commander = UnityEngine.Object.FindFirstObjectByType<ShelterCommander>();
            var landing = ctrl.Navigation.Areas.First(r => r.kind == ShelterArea.AreaKind.Corridor && Mathf.Approximately(r.FloorY, guard.FloorY));
            Vector2 overlap = new Vector2((Mathf.Max(guard.Left, landing.Left) + Mathf.Min(guard.Right, landing.Right)) / 2, guard.FloorY + 1);
            Check(commander.ClickWorld(actors[0].transform.position + Vector3.up), "Select a guard for the landing command");
            var firstGuard = commander.Selected;
            Check(commander.ClickWorld(overlap) && firstGuard.Destination == guard && !commander.Selected, "Clicking the shared landing and guard-room area assigns the guard room and clears selection");
            ShelterElevatorChecks.Move(firstGuard, guard);
            foreach (var actor in actors)
                if (actor.GetComponent<ShelterWalker>() != firstGuard) Move(actor, guard);
            int period = ctrl.Data.CombatEveryTicks;
            int volley = sim.Rules.GuardVolleyEveryTicks;
            int travel = sim.Rules.InvasionApproachTicks;
            void Ticks(int n) => sim.Advance(n * sim.Rules.SecondsPerTick);
            var z = ctrl.SpawnWave();
            var entry = z.Doorway;
            float speed = ctrl.Data.WalkSpeed;
            Check(travel == 1 && volley == 2 && period == 5 && Mathf.Abs(Mathf.Abs(z.transform.position.x - entry.x) - travel * speed * sim.Rules.SecondsPerTick) < 0.001f,
                "Spawn distance is one walking tick, ranged interval two, melee interval five");
            var spawnX = z.transform.position.x;
            var camera = UnityEngine.Object.FindFirstObjectByType<ShelterCamera>();
            camera.Pan(Vector2.right * Mathf.Sign(spawnX - entry.x), 100f);
            float spawnViewportX = camera.GetComponent<Camera>().WorldToViewportPoint(z.transform.position).x;
            Check(spawnViewportX >= 0 && spawnViewportX <= 1, "Camera can pan to the outdoor spawn");
            sim.Advance(0.5 * sim.Rules.SecondsPerTick); Advance(ctrl, 0.5f * sim.Rules.SecondsPerTick);
            Check(Mathf.Abs(z.transform.position.x - Mathf.MoveTowards(spawnX, entry.x, speed * sim.Rules.SecondsPerTick * 0.5f)) < 0.001f, "Zombie walks smoothly between clock ticks");
            sim.AcquirePause(checks); var pausedAt = z.transform.position; Ticks(10); Advance(ctrl, 10); sim.ReleasePause(checks);
            Check(z.transform.position == pausedAt && z.ResolvedCombats == 0, "Pause freezes approach and attacks");
            sim.Advance(0.5 * sim.Rules.SecondsPerTick);
            Check(z.State == ZombieState.Approaching && z.ResolvedCombats == 1 && !guard.IsOccupied &&
                z.Hp == z.InitialHp - ctrl.Data.DamageToZombie, "Exactly one guard hits on arrival without breaching");
            var rolls = ScriptRolls(ctrl); // Any door roll would fail this fixture.
            Ticks(volley);
            Check(z.State == ZombieState.Dead && ctrl.Zombies.Count == 0 && !guard.IsOccupied,
                "Multiple guards share the hit limit and a lethal door hit prevents entry");
            Reset(); Move(actors[0], guard);
            for (int wave = 1; wave < 4; wave++) { ctrl.SpawnWave(); Ticks(travel + volley); }
            actors[0].ResetMental();
            z = ctrl.SpawnWave(); ctrl.RollDie = Outcome(false);
            Ticks(travel + volley);
            Check(z.ResolvedCombats == 2 && z.State == ZombieState.Approaching && !guard.IsOccupied &&
                z.Hp == 2 && Mathf.Abs(z.transform.position.x-z.Doorway.x)<.001f, "A strong zombie waits at the door after two fixed hits");
            Ticks(volley);
            Check(z.State == ZombieState.Defending && z.Hp == 1 && guard.IsOccupied && z.ResolvedCombats == 4,
                "Three door hits followed immediately by a melee exchange");
            Ticks(period - 1);
            Check(z.ResolvedCombats == 4, "Melee does not repeat at the shorter ranged interval");
            Ticks(1);
            Check(z.ResolvedCombats == 5, "Melee repeats five ticks after the breach");
            actors[0].SetMental(2); Ticks(period);
            Check(!actors[0].CanAct && z.State == ZombieState.Invading && z.Hp == 1, "A surviving invader continues after its last defender faints");
            var fallen = actors[0];
            var faintLines = GameData.GetTable("ShelterDialogue").Rows
                .Where(row => row.Get<string>("role") == fallen.Definition.Role && row.Get<string>("situation") == "faint")
                .Select(row => row.Get<string>("line")).ToHashSet();
            var speech = UnityEngine.Object.FindFirstObjectByType<ShelterSpeech>();
            var faintBubble = speech.GetComponentsInChildren<TMPro.TMP_Text>().Single(text => faintLines.Contains(text.text));
            ShelterSpeech.Say(fallen.transform, fallen.Definition.Role, "melee_loss");
            sim.AcquirePause(checks);
            try
            {
                Time.timeScale = 4;
                float deadline = Time.time + 3f;
                while (faintBubble.gameObject.activeInHierarchy && Time.time < deadline) await Frame();
                Check(!faintBubble.gameObject.activeInHierarchy && faintLines.Contains(faintBubble.text),
                    "A fainted defender's last bubble expires without playing queued combat dialogue");
            }
            finally { Time.timeScale = 0; sim.ReleasePause(checks); }
            for (int i = 0; i < 41; i++) actors[0].AdvanceMentalTick();
            Check(actors[0].IsFainted && actors[0].CurrentMental == 0, "Fainted guard waits for rescue beyond the former timer");

            Reset(); Move(actors[0], guard); Ticks(7);
            Move(actors[0], sleeping); Ticks(1); Move(actors[0], guard); Ticks(7);
            Check(actors[0].CurrentMental == actors[0].Definition.Mental - 14 * sim.Rules.GuardMentalLossPerTick, "Fourteen guard ticks drain mental; waiting outside a tent gives no recovery");
            Ticks(1);
            Check(actors[0].CurrentMental == actors[0].Definition.Mental - 15 * sim.Rules.GuardMentalLossPerTick && actors[1].CurrentMental == 60, "Guard duty drains while no zombies are present");
            Reset(); Move(actors[0], guard); z = ctrl.SpawnWave(); Ticks(travel);
            actors[0].SetMental(0); Advance(ctrl, .01f);
            Check(z.State == ZombieState.Invading && z.Hp == z.InitialHp - ctrl.Data.DamageToZombie,
                "Losing the last defender opens the door immediately");
            Reset(); z = ctrl.SpawnWave(); Ticks(travel);
            Check(z.State == ZombieState.Invading && actors.All(a => a.CurrentMental == 58), "An undefended room is breached on arrival and charges one breach");
            foreach (var actor in actors) actor.transform.position = z.transform.position - Vector3.right * 0.3f;
            Advance(ctrl, 0.04f);
            Check(z.State == ZombieState.Encounter, "Invader intercepts survivors on its indoor route");
            rolls = ScriptRolls(ctrl, 6, 1, 1, 1, 1); Ticks(period);
            Check(rolls.Count == 0 && z.State == ZombieState.Encounter && actors.Count(a => a.CurrentMental == 56) == 1, "All four route contacts fight and one takes damage; melee continues");
            foreach (var actor in actors) actor.SetMental(0);
            Advance(ctrl, 0.04f);
            Check(z.State == ZombieState.Invading, "Road movement resumes only when no active fighters remain");

            Reset(); z = ctrl.SpawnWave(); Ticks(travel);
            var corridor = ctrl.Navigation.Areas.Where(a => a.kind == ShelterArea.AreaKind.Corridor &&
                Mathf.Approximately(a.FloorY, common.FloorY) && a.Right <= common.Left + 0.01f)
                .OrderBy(a => a.Right - a.Left).First();
            var invader = z.GetComponent<ShelterWalker>();
            invader.Stop(); z.transform.position = corridor.Centre;
            Check(invader.MoveTo(common), "Invader has a route through the corridor");
            var zombieScale = z.transform.localScale;
            var developerScales = actors.Select(a => a.transform.localScale).ToArray();
            actors[0].transform.position = corridor.Centre; Advance(ctrl, 0); Advance(ctrl, 0.01f);
            Check(z.State == ZombieState.Encounter && corridor.ContainsFeet(z.transform.position) &&
                z.transform.position.x < corridor.Centre.x, "First contact stages the zombie on the left of the corridor");
            foreach (var reinforcement in actors.Skip(1)) reinforcement.transform.position = corridor.ArrivalPoint(corridor.width);
            Advance(ctrl, 0.01f);
            var slots = actors.Select(a => a.transform.position.x).ToArray();
            Check(actors.All(a => corridor.ContainsFeet(a.transform.position)) && slots[0] > corridor.Centre.x &&
                slots.Zip(slots.Skip(1), (a, b) => b > a).All(inOrder => inOrder),
                "Reinforcements take distinct right-hand slots on the same corridor floor");
            Check(z.transform.localScale == zombieScale && actors.Select(a => a.transform.localScale).SequenceEqual(developerScales),
                "A crowded corridor preserves the original scale of the zombie and every developer");

            Reset(); foreach (var actor in actors) actor.SetMental(0);
            z = ctrl.SpawnWave(); Ticks(travel); var target = z.TargetRoom;
            Check(z.State == ZombieState.Invading && target && target != guard, "Empty combat room chooses connected noncombat target");
            bool climbed = false;
            for (int i = 0; i < 1500 && z.State != ZombieState.Occupying; i++)
            { Advance(ctrl, 0.05f); climbed |= z.GetComponent<ShelterWalker>().IsInElevator; }
            Check(climbed && z.State == ZombieState.Occupying && target.IsOccupied && target.IsWalkable && !target.IsOperational, "Elevator graph reaches target and occupies without blocking entry");
            actors[0].ResetMental(); Move(actors[0], target);
            Check(!actors[0].GetComponent<ShelterWalker>().CanWorkInCurrentRoom, "Occupied room stops work");
            ctrl.RollDie = Outcome(true); Advance(ctrl, 0.05f);
            Check(z.State == ZombieState.Encounter, "Entering occupied room automatically starts combat");
            Check(z.transform.position.x < target.Centre.x && actors[0].transform.position.x > target.Centre.x &&
                target.ContainsFeet(z.transform.position) && target.ContainsFeet(actors[0].transform.position),
                "Occupant and developer face off in opposite halves of the occupied room");
            sim.AcquirePause(checks); var position = z.transform.position; int hp = z.Hp; z.ProcessTick(100); Advance(ctrl, 10);
            Check(z.Hp == hp && z.transform.position == position, "Popup-style simulation pause freezes combat and movement"); sim.ReleasePause(checks);
            Ticks(period); Ticks(period);
            Check(z.State == ZombieState.Dead && !target.IsOccupied && target.IsOperational && actors[0].GetComponent<ShelterWalker>().CanWorkInCurrentRoom, "Clearing occupant restores room functionality");
            Check(actors[0].CurrentMental == 65, "Invasion kill rewards combat participant once");
            object a = new object(), b = new object(); target.SetOccupied(a, true); target.SetOccupied(b, true); target.SetOccupied(a, false);
            Check(target.IsOccupied, "One removal cannot clear another occupier"); target.SetOccupied(b, false);
            Check(!target.IsOccupied, "Last occupier restores room");

            Reset(); await CheckSharedOccupation(ctrl, guard, plan, actors);

            Reset(); Move(actors[0], plan); float progress = dev.Progress.Planning;
            plan.SetOccupied(checks, true); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(dev.Progress.Planning == progress, "Real work tick produces no progress in occupied planning room");
            plan.SetOccupied(checks, false); sim.Advance(sim.Rules.SecondsPerTick * sim.Rules.WorkEveryTicks);
            Check(dev.Progress.Planning > progress, "Real work tick resumes after liberation");

            Reset(); sim.SetPendingWaveHpModifier(4); Ticks(sim.Rules.FirstInvasionTick - 1);
            Check(ctrl.Wave == 0 && sim.PendingWaveHpModifier == 4, "No early invasion or early HP consumption");
            Ticks(1);
            Check(ctrl.Zombies.Count == 1 && ctrl.Zombies[0].Hp == 6 && sim.PendingWaveHpModifier == 0, "Scheduled wave consumes existing NEXT_WAVE_HP exactly once");
            Ticks(sim.Rules.InvasionEveryTicks - 1);
            Check(ctrl.Wave == 1, "The second invasion waits the full interval after the first");
            Ticks(1);
            Check(ctrl.Zombies.Count == 2 && ctrl.Zombies[1].InitialHp == 2, "Next wave returns to catalog HP and independent agents coexist");
            dev.RestartDevelopment();
            Check(ctrl.Zombies.Count == 0 && ctrl.Wave == 0 && ctrl.SceneRooms().All(r => !r.IsOccupied) && actors.All(c => c.CurrentMental == 60), "Restart clears zombies, occupation, locks, wave and mental state");

            Reset(); var ladder = ctrl.Navigation.Elevators.OrderByDescending(l => l.Top.y).First(); ladder.gameObject.SetActive(false); ctrl.Navigation.Refresh();
            z = ctrl.SpawnWave(); Ticks(travel); Check(z.State == ZombieState.WaitingForRoute, "Disconnected layout waits without teleporting");
            ladder.gameObject.SetActive(true); ctrl.Navigation.Refresh(); Advance(ctrl, 0.05f);
            Check(z.State == ZombieState.Invading && z.TargetRoom, "Reconnecting designer elevator automatically restores route");
            Reset();
            sim.Advance((sim.Rules.TotalTicks - travel - volley) * sim.Rules.SecondsPerTick);
            ctrl.ResetRun();
            foreach (var actor in actors) { actor.ResetMental(); Move(actor, guard); }
            z = ctrl.SpawnWave(); var follower = ctrl.SpawnWave();
            ScriptRolls(ctrl, 1, 6, 6, 6, 6);
            sim.Advance((travel + volley) * sim.Rules.SecondsPerTick); await Frame();
            Check(!z && ctrl.Zombies.SequenceEqual(new[] { follower }) && follower.isActiveAndEnabled && follower.Hp == follower.InitialHp && !ctrl.CanAdvance && actors.All(c => !c.GetComponent<ShelterWalker>().IsMovementLocked), "A kill on the final game tick freezes the surviving zombie in place");
            dev.RestartDevelopment();
            Check(ctrl.Zombies.Count == 0 && ctrl.SceneRooms().All(r => !r.IsOccupied), "Restart clears the surviving end-of-run zombie");
            ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1);
            foreach (var actor in actors) Move(actor, common);
            events.enabled = true;
            return Save("play");
        }
        catch (Exception e) { checks.Add(e.ToString()); Save("play"); throw; }
        finally { if (ctrl) ctrl.RollDie = sides => UnityEngine.Random.Range(1, sides + 1); Time.timeScale = oldScale; }
    }
}
