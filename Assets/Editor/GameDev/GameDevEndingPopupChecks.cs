using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;
using UnityGameJam.Events;
using UnityGameJam.Zombies;

public static class GameDevEndingPopupChecks
{
    static void Check(bool pass, string message, List<string> results)
    { if (!pass) throw new InvalidOperationException(message); results.Add("PASS: " + message); }

    public static string RunEdit()
    {
        var results = new List<string>();
        var rules = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        rules.Validate();
        int tick = 24;
        var calculator = new GameDevScoreCalculator(rules.EvaluationTargetProgress, rules.EvaluationReferenceTicks, () => tick);
        Check(calculator.TryCalculate(new GameDevProgress(32,32,32), out double score) && Math.Abs(score-1) < 1e-12, "96 progress / 24 ticks = 1 without x100", results);
        tick = 120;
        Check(calculator.TryCalculate(new GameDevProgress(100,100,100),out score) && score == 0.625, "300 progress / 120 ticks = 0.625", results);
        Check(calculator.TryCalculate(default,out score) && score == 0, "Zero progress is a valid baseline score after time passes", results);
        tick = 0;
        Check(!calculator.TryCalculate(new GameDevProgress(100,100,100),out _), "Tick zero defers evaluation without division by zero", results);
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevEndingPopupSetup.PrefabPath);
        var popup = root.GetComponent<GameDevEndingPopup>();
        var serialized = new SerializedObject(popup);
        foreach (string field in new[] { "panel", "title", "body", "score", "confirm", "scroll" })
            Check(serialized.FindProperty(field).objectReferenceValue != null, "Stored popup reference: " + field, results);
        Check(root.GetComponentsInChildren<TMP_Text>(true).All(t => t.font && !t.richText), "Korean font and literal data text", results);
        var button = root.GetComponentInChildren<Button>(true);
        Check(button.transition == Selectable.Transition.SpriteSwap && button.spriteState.highlightedSprite && button.spriteState.pressedSprite, "Real button state sprites", results);
        Check(root.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing prefab scripts", results);
        return string.Join("\n",results);
    }

    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play mode required.");
        var results = new List<string>();
        var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
        var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
        var popup = UnityEngine.Object.FindFirstObjectByType<GameDevEndingPopup>();
        var commander = UnityEngine.Object.FindFirstObjectByType<ShelterCommander>();
        var events = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
        var zombies = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>();
        var roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>();
        float timeScale = Time.timeScale;
        bool eventsEnabled = events && events.enabled, zombiesEnabled = zombies && zombies.enabled;
        var disabled = new List<GameObject>();
        Time.timeScale = 0;
        try
        {
            await sim.InitializeAsync();
            if (zombies) await zombies.InitializeAsync();
            if (events) events.enabled = false;
            if (zombies) zombies.enabled = false;
            var catalog = GameDevEndingCatalog.FromTable(GameData.GetTable(GameDevKeys.EndingsTable));
            dev.SetScoreCalculator(new GameDevScoreCalculator());
            foreach (var ending in catalog.Active)
            {
                dev.RestartDevelopment();
                Check(!popup.IsOpen, "Restart hides prior ending", results);
                dev.FinishDevelopment();
                Check(!popup.IsOpen, "Pending score does not invent an ending", results);
                dev.SubmitFinalScore(ending.MinimumScore);
                Check(popup.IsOpen && dev.Ending.Id == ending.Id, "Boundary opens " + ending.Id, results);
                var text = popup.GetComponentsInChildren<TMP_Text>();
                Check(text.Any(t => t.name == "Title" && t.text == ending.DisplayName) && text.Any(t => t.name == "Content" && t.text == ending.Description), "Popup uses actual catalog text", results);
                Check(sim.IsPaused && commander.InputBlocked && !commander.ClickWorld(Vector2.zero), "Popup blocks simulation and world commands", results);
                Check(!dev.FinishDevelopment() && !dev.SubmitFinalScore(0), "Repeated completion is ignored", results);
                await Task.Delay(40);
                popup.GetComponentInChildren<Button>().onClick.Invoke();
                await Task.Delay(40);
                Check(!popup.IsOpen && !sim.IsPaused && !commander.InputBlocked, "Confirm closes and releases owned locks", results);
                popup.enabled = false; popup.enabled = true;
                Check(!popup.IsOpen, "Confirmed result stays dismissed after re-enable", results);
            }
            foreach (var actor in roster.Characters)
                if (actor && actor.gameObject.activeSelf) { disabled.Add(actor.gameObject); actor.gameObject.SetActive(false); }
            dev.SetScoreCalculator(new GameDevScoreCalculator(sim.Rules.EvaluationTargetProgress,sim.Rules.EvaluationReferenceTicks,() => sim.CurrentTick));
            dev.RestartDevelopment();
            dev.SetProgress(GameDevField.Planning,80); dev.SetProgress(GameDevField.Programming,80); dev.SetProgress(GameDevField.Graphics,80);
            int reached = 0;
            Action<GameDevEnding,double> listener = (_,__) => reached++;
            dev.EndingReached += listener;
            try
            {
                sim.Advance((double)sim.Rules.SecondsPerTick * sim.Rules.TotalTicks);
                Check(sim.CurrentTick == sim.Rules.TotalTicks && dev.Phase == GameDevPhase.Ended && dev.TotalScore == 0.5 && popup.IsOpen, "Actual final bunker tick evaluates and opens popup automatically", results);
                sim.Advance(1000);
                Check(reached == 1, "Final ending emitted once", results);
                popup.enabled = false;
                Check(!sim.IsPaused, "Disable releases popup pause", results);
                popup.enabled = true;
                Check(popup.IsOpen, "Unconfirmed result restores after re-enable", results);
            }
            finally { dev.EndingReached -= listener; }
        }
        finally
        {
            foreach (var actor in disabled) if (actor) actor.SetActive(true);
            if (events) events.enabled = eventsEnabled;
            if (zombies) zombies.enabled = zombiesEnabled;
            Time.timeScale = timeScale;
        }
        return string.Join("\n",results);
    }
}
