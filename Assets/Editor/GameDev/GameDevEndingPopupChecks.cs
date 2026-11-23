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
using ShelterRun;
using UnityEngine.SceneManagement;


public static class GameDevEndingPopupChecks
{
    static void Check(bool pass, string message, List<string> results)
    { if (!pass) throw new InvalidOperationException(message); results.Add("PASS: " + message); }

    public static string RunEdit()
    {
        var results = new List<string>();
        var rules = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        rules.Validate();
        int tick = rules.EvaluationReferenceTicks;
        var calculator = new GameDevScoreCalculator(rules.EvaluationTargetProgress, rules.EvaluationReferenceTicks, () => tick);
        Check(calculator.TryCalculate(new GameDevProgress(32,32,32), out double score) && Math.Abs(score-1) < 1e-12, "96 progress at the reference tick = 1 without x100", results);
        var final = new GameDevFinalScoreCalculator();
        Check(final.TryCalculate(new GameDevProgress(90,0,0),out score) && score == 0.3, "90 progress at submission = 0.3", results);
        Check(final.TryCalculate(new GameDevProgress(100,50,0),out score) && score == 0.5, "150 progress at submission = 0.5", results);
        Check(final.TryCalculate(new GameDevProgress(100,100,100),out score) && score == 1, "Full completion = 1", results);
        Check(final.TryCalculate(default,out score) && score == 0, "Zero progress = 0", results);
        tick = 0;
        Check(!calculator.TryCalculate(new GameDevProgress(100,100,100),out _), "Tick zero defers evaluation without division by zero", results);
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevEndingPopupSetup.PrefabPath);
        var popup = root.GetComponent<GameDevEndingPopup>();
        var serialized = new SerializedObject(popup);
        foreach (string field in new[] { "panel", "title", "body", "score", "confirm", "retry", "blackout", "endReasonLabel", "scroll" })
            Check(serialized.FindProperty(field).objectReferenceValue != null, "Stored popup reference: " + field, results);
        Check(root.GetComponentsInChildren<TMP_Text>(true).All(t => t.font && !t.richText), "Korean font and literal data text", results);
        var buttons = root.GetComponentsInChildren<Button>(true);
        Check(buttons.Length == 2 && buttons.All(b => b.transition == Selectable.Transition.ColorTint &&
            b.targetGraphic && b.colors.normalColor != b.colors.highlightedColor && b.colors.normalColor != b.colors.pressedColor),
            "Both choices show focus and press feedback on the report", results);
        Check(root.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing prefab scripts", results);
        return string.Join("\n",results);
    }

    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play mode required.");
        var results = new List<string>();
        await GameData.InitializeAsync();
        await CheckEarlyEndings(results);
        var catalog = GameDevEndingCatalog.FromTable(GameData.GetTable(GameDevKeys.EndingsTable));
        foreach (var ending in catalog.Active)
        {
            FarmingRunTransfer.BeginRun("person_02");
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
            await sim.InitializeAsync();
            var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var popup = UnityEngine.Object.FindFirstObjectByType<GameDevEndingPopup>();
            dev.SetScoreCalculator(new GameDevScoreCalculator());
            dev.FinishDevelopment();
            Check(!popup.IsOpen && dev.Phase == GameDevPhase.AwaitingScore, "Wait for a score", results);
            dev.SubmitFinalScore(ending.MinimumScore);
            Check(!popup.IsOpen && sim.IsPaused, "Hold results during fade", results);
            var blackout = (CanvasGroup)new SerializedObject(popup).FindProperty("blackout").objectReferenceValue;
            var lines = new HashSet<string>();
            var linesWhileWalking = new HashSet<string>();
            float deadline = Time.realtimeSinceStartup + 40;
            while (!popup.IsOpen || blackout.gameObject.activeSelf)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Ending playback: " + ending.Id);
                var stage = UnityEngine.Object.FindFirstObjectByType<EndingCutscene>();
                if (stage && stage.bubble.gameObject.activeInHierarchy)
                {
                    lines.Add(stage.dialogue.text);
                    if (stage.bunker.activeSelf && stage.zombies.All(z => Mathf.Abs(z.Velocity.x) > .01f &&
                        Mathf.Approximately(z.transform.position.y, stage.doorway.position.y)))
                        linesWhileWalking.Add(stage.dialogue.text);
                }
                await Task.Delay(50);
            }
            var branch = UnityEngine.Object.FindFirstObjectByType<EndingCutscene>();
            bool good = ending.Id == "ending_breakthrough";
            bool bad = ending.Id == "ending_unfinished";
            var situation = good ? "ending_satisfied" : bad ? "ending_bad" : "invade";
            var expected = GameData.GetTable("ShelterDialogue").Rows.Where(r => r.Get<string>("situation") == situation)
                .Select(r => r.Get<string>("line"));
            Check(branch.zombies.Length >= 12 && linesWhileWalking.Count > 0 && linesWhileWalking.SetEquals(expected),
                "Crowd speaks while walking on the doorway floor", results);
            Check(branch && branch.farmingRoom.activeSelf == good && lines.Count >= 3, "Played " + ending.Id, results);
            float leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, .5f, 10)).x;
            Check(branch.zombies.All(z => bad || good ? z.transform.position.x < leftEdge :
                z.transform.position.x > branch.doorway.position.x), "Crowd reached its destination", results);
            var text = popup.GetComponentsInChildren<TMP_Text>();
            Check(text.Any(t => t.text == ending.DisplayName) && text.Any(t => t.text == ending.Description) &&
                text.Any(t => t.text.Contains(ending.MinimumScore.ToString("0.000"))), "Catalog result and score", results);
            bool replay = good;
            var button = (Button)new SerializedObject(popup).FindProperty(replay ? "retry" : "confirm").objectReferenceValue;
            button.onClick.Invoke();
            string target = replay ? "Farming" : TitleMenu.SceneName;
            deadline = Time.realtimeSinceStartup + 30;
            while (SceneManager.GetActiveScene().name != target)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(target);
                await Task.Delay(50);
            }
            Check(FarmingRunTransfer.SelectedStarter == (replay ? "person_02" : null), "Reached " + target + " with fresh run state", results);
        }
        return string.Join("\n", results);
    }

    public static async Task CheckEarlyEndings(List<string> results)
    {
        foreach (var (complete, exhaust) in new[] { (false, true), (true, false), (true, true) })
        {
            FarmingRunTransfer.BeginRun("person_02");
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
            await sim.InitializeAsync();
            var dev = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>().Characters;
            var popup = UnityEngine.Object.FindFirstObjectByType<GameDevEndingPopup>();
            var label = (TMP_Text)new SerializedObject(popup).FindProperty("endReasonLabel").objectReferenceValue;
            var curtain = label.GetComponentInParent<CanvasGroup>(true);
            Check(roster.Count > 1, "Multi-survivor test roster", results);
            int tick = sim.CurrentTick, submissions = 0;
            dev.EndingReached += (_, __) => submissions++;
            sim.AcquirePause(results);
            for (int i = 1; i < roster.Count; i++) roster[i].SetMental(0);
            dev.SetProgress(GameDevField.Planning, 100);
            dev.SetProgress(GameDevField.Graphics, 100);
            dev.SetProgress(GameDevField.Programming, 99);
            sim.Advance(0);
            Check(dev.Phase == GameDevPhase.Developing && !label.gameObject.activeSelf, "One awake survivor and one incomplete gauge keep playing", results);
            if (exhaust)
            {
                if (complete) dev.SetProgress(GameDevField.Programming, 100);
                roster[0].SetMental(0);
                sim.Advance(0);
            }
            else
            {
                sim.ReleasePause(results);
                sim.WorkProcessed += _ => dev.SetProgress(GameDevField.Programming, 100);
                tick += sim.Rules.WorkEveryTicks - tick % sim.Rules.WorkEveryTicks;
            }
            sim.Advance(sim.Rules.TotalTicks * sim.Rules.SecondsPerTick);
            var reason = complete ? BunkerEndReason.GameCompleted : BunkerEndReason.AllFainted;
            double expectedScore = complete ? 1d : 299d / 300d;
            var scoreLabel = (TMP_Text)new SerializedObject(popup).FindProperty("score").objectReferenceValue;
            Check(dev.TotalScore == expectedScore && dev.FormattedScore == expectedScore.ToString("0.000") &&
                scoreLabel.text == $"평가 점수  {dev.FormattedScore}", "Submitted score and ending display: " + reason, results);
            Check(submissions == 1 && sim.EndReason == reason && dev.Phase == GameDevPhase.Ended &&
                sim.CurrentTick == tick && sim.RemainingTicks == 0, "Immediate single submission: " + reason, results);
            Check(label.gameObject.activeSelf && label.text == (complete ? "게임 개발 완료" : "전원 탈진") &&
                label.alignment == TextAlignmentOptions.Center && label.rectTransform.anchoredPosition == Vector2.zero,
                "Centered notice: " + reason, results);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (curtain.alpha < 1f)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Notice fade");
                await Task.Yield();
            }
            Check(label.gameObject.activeInHierarchy, "Notice visible on opaque curtain", results);
            while (popup.gameObject.scene.name != EndingCutscene.SceneName || label.gameObject.activeSelf)
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Early ending scene");
                await Task.Yield();
            }
            Check(!label.gameObject.activeSelf, "Cutscene takes over after notice", results);
        }
    }
}
