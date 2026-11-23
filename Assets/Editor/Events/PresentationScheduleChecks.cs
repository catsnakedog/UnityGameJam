using System;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

public static class PresentationScheduleChecks
{
    static readonly int[] rounds = { 240, 480, 720 };

    static void Check(bool condition, string message, List<string> report)
    {
        if (!condition) throw new InvalidOperationException(message);
        report.Add("PASS " + message);
    }

    public static string RunEdit()
    {
        var rules = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        rules.Validate();
        var report = new List<string>();
        Check(rules.PresentationTicks.SequenceEqual(rounds) && rules.FirstInvasionTick == 20 &&
            rules.InvasionEveryTicks == 80 && rules.RandomEventEveryTicks == 40 && rules.TotalTicks == 960 &&
            rules.EvaluationTargetProgress == 96, "Authored daily schedule and presentation target", report);
        void CheckDifficulty(BunkerDifficulty difficulty, float seconds, int events, int first, int invasion)
        {
            var profile = rules.CreateDifficultyOverride(difficulty);
            try
            {
                profile.Validate();
                Check(profile.SecondsPerTick == seconds && profile.RandomEventEveryTicks == events &&
                    profile.FirstInvasionTick == first && profile.InvasionEveryTicks == invasion &&
                    profile.TotalTicks == rules.TotalTicks && profile.PresentationTicks.SequenceEqual(rounds),
                    difficulty + " clock and schedule", report);
            }
            finally { Object.DestroyImmediate(profile); }
        }
        CheckDifficulty(BunkerDifficulty.Easy, 1f, 60, 80, 120);
        CheckDifficulty(BunkerDifficulty.Medium, .5f, 48, 40, 96);
        Check(rules.SecondsPerTick == .33f && rules.FirstInvasionTick == 20 &&
            rules.RandomEventEveryTicks == 40 && rules.InvasionEveryTicks == 80,
            "Difficulty selection preserves the shared Hard settings", report);
        void Grade(double score, string label, int mental)
        {
            var grade = rules.GradePresentation(score);
            Check(grade.Label == label && grade.MentalDelta == mental, $"At {score}, {label} changes mental by {mental}", report);
        }
        Grade(0, "망겜", -15);
        Grade(.699999, "망겜", -15);
        Grade(.7, "흠", 0);
        Grade(.999999, "흠", 0);
        Grade(1, "굿", 10);
        Grade(1.199999, "굿", 10);
        Grade(1.2, "갓겜", 20);
        return string.Join("\n", report);
    }

    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode before running the shelter schedule.");
        var report = new List<string>();
        float timeScale = Time.timeScale;
        Time.timeScale = 0;
        BunkerSimulation simulation = null;
        GameDevManager development = null;
        var ordinary = new List<int>();
        var waves = new List<int>();
        var completed = new List<int>();
        var presentations = new List<(int tick, double score, BunkerRules.PresentationGrade grade)>();
        void OnPresentation(int tick, double score, BunkerRules.PresentationGrade grade) => presentations.Add((tick, score, grade));
        try
        {
            FarmingRunTransfer.Capture(new[] { "person_01", "person_02", "person_03" },
                AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            var zombies = Object.FindFirstObjectByType<ZombieInvasionController>();
            await zombies.InitializeAsync();
            await Object.FindFirstObjectByType<ShelterTransit>().InitializeAsync();
            simulation = zombies.Simulation;
            development = Object.FindFirstObjectByType<GameDevManager>();
            var events = Object.FindFirstObjectByType<EventPopupController>();
            for (int i = 0; i < 200 && (!events.IsReady || simulation.IsPaused); i++) await Task.Delay(25);
            Check(events.IsReady && !simulation.IsPaused && zombies.Roster.Characters.Count == 3, "Shelter scene and three-person roster loaded", report);
            development.RestartDevelopment();
            var common = zombies.SceneRooms().Single(room => room.roomType == ShelterRoomType.Common);
            var actors = zombies.Roster.Characters;
            foreach (var actor in actors) ShelterElevatorChecks.Move(actor.GetComponent<ShelterWalker>(), common);

            simulation.RandomEventDue += ordinary.Add;
            simulation.ZombieInvasionDue += waves.Add;
            simulation.PresentationDue += OnPresentation;
            simulation.TickOccurred += completed.Add;
            var expectedScores = new[] { .5, 1.0, 1.25 };
            var expectedDeltas = new[] { -15, 10, 20 };
            var mental = new[] { 5, 50, 95 };
            report.Add(GameDevChecks.CheckClockScheduleInPlay());
            for (int tick = 1; tick <= 960; tick++)
            {
                int round = Array.IndexOf(rounds, tick);
                if (round >= 0)
                {
                    for (int i = 0; i < actors.Count; i++) actors[i].SetMental(mental[i]);
                    // Three distinct fields sum to 12, 48 and 90 across the three rounds.
                    var totalProgress = (float)(expectedScores[round] * 96 * tick / 960);
                    development.SetProgress(GameDevField.Planning, 2);
                    development.SetProgress(GameDevField.Programming, 4);
                    development.SetProgress(GameDevField.Graphics, totalProgress - 6);
                }
                simulation.Advance(simulation.Rules.SecondsPerTick);
                if (simulation.CurrentTick != tick) throw new InvalidOperationException($"Clock stopped before tick {tick}.");
                if (tick == 20) report.Add(GameDevChecks.CheckClockScheduleInPlay());
                if (round >= 0)
                {
                    Check(presentations.Count == round + 1 && presentations[round].tick == tick &&
                        Math.Abs(presentations[round].score - expectedScores[round]) < 1e-9 &&
                        presentations[round].grade.MentalDelta == expectedDeltas[round] && simulation.LastPresentationTick == tick,
                        $"Round {round + 1} dispatched its score and grade once at tick {tick}", report);
                    var after = actors.Select((actor, i) =>
                    {
                        int multiplier = actor.Hunger == 0 && expectedDeltas[round] < 0 ? simulation.Rules.EmptyHungerMentalLossMultiplier : 1;
                        return Mathf.Clamp(mental[i] + expectedDeltas[round] * multiplier, 0, simulation.Rules.MentalMaximum);
                    }).ToArray();
                    Check(actors.Select(actor => actor.CurrentMental).SequenceEqual(after) && !completed.Contains(tick),
                        $"All three mental values applied before confirming round {round + 1}", report);
                    report.Add(GameDevChecks.CheckClockScheduleInPlay());
                    report.Add(await EventPopupChecks.CheckPresentationInPlay(pressEnter: round == 1));
                    simulation.Advance(0);
                    simulation.Advance(0);
                    Check(completed.Count(t => t == tick) == 1 && presentations.Count == round + 1 &&
                        actors.Select(actor => actor.CurrentMental).SequenceEqual(after),
                        $"Continuing round {round + 1} finished its tick once and preserved mental", report);
                }
                else if (events.Popup.IsOpen)
                {
                    if (events.LastEventTick != tick || events.Popup.ShowingPresentation)
                        throw new InvalidOperationException($"Unexpected popup at tick {tick}.");
                    events.Popup.Close();
                }
            }
            Check(ordinary.SequenceEqual(Enumerable.Range(1, 23).Select(hour => hour * 40).Except(rounds)),
                "All 20 ordinary hourly events and no random event on a presentation or ending", report);
            Check(waves.SequenceEqual(Enumerable.Range(0, 12).Select(index => 20 + index * 80)) && zombies.Wave == 12,
                "All 12 invasions spawned from tick 20 through 900", report);
            Check(presentations.Count == 3 && completed.SequenceEqual(Enumerable.Range(1, 960)) &&
                development.Phase == GameDevPhase.Ended && !events.Popup.IsOpen, "All 960 ticks completed once with the final ending", report);
            development.RestartDevelopment();
            Check(simulation.CurrentTick == 0 && simulation.LastPresentationTick == 0 && simulation.LastWaveTick == 0 &&
                events.LastEventTick == 0 && zombies.Wave == 0 && !events.Popup.IsOpen &&
                actors.All(actor => actor.CurrentMental == actor.Definition.Mental), "Restart clears rounds, waves, popups and mental effects", report);
            report.Add(GameDevChecks.CheckClockScheduleInPlay());
            return string.Join("\n", report);
        }
        finally
        {
            if (simulation)
            {
                simulation.RandomEventDue -= ordinary.Add;
                simulation.ZombieInvasionDue -= waves.Add;
                simulation.PresentationDue -= OnPresentation;
                simulation.TickOccurred -= completed.Add;
            }
            if (development && development.Phase != GameDevPhase.Loading) development.RestartDevelopment();
            Time.timeScale = timeScale;
        }
    }
}
