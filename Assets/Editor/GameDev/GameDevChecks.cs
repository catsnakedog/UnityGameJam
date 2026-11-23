using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using GameJam.Data.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.GameDev;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

public static class GameDevChecks
{
    [MenuItem("Tools/Game Dev/Validate Ending Rules and UI")]
    public static void RunMenu() => Debug.Log(RunEdit());

    public static string RunEdit()
    {
        var checks = new List<string>();
        var p = new GameDevProgress(-5, 47.5f, 123);
        Check(p.Planning == 0 && p.Programming == 47.5f && p.Graphics == 100, "Clamp each discipline to 0..100 independently", checks);
        Check(p.With(GameDevField.Planning, 30).Programming == 47.5f && p.Planning == 0, "Progress snapshots remain immutable", checks);
        Reject(() => new GameDevProgress(float.NaN, 0, 0), "Reject NaN progress", checks);
        Reject(() => new GameDevProgress(0, float.PositiveInfinity, 0), "Reject infinite progress", checks);
        Reject(() => p.Get((GameDevField)99), "Reject unknown discipline", checks);
        Check(!new GameDevScoreCalculator().TryCalculate(default, out _), "Deferred calculator does not invent a score", checks);

        var catalog = new GameDevEndingCatalog(new[] { Ending("high", 200), Ending("off", 150, false), Ending("low", 0), Ending("mid", 100) });
        Check(catalog.Select(0).Id == "low" && catalog.Select(99.999).Id == "low", "Baseline and below-threshold scores", checks);
        Check(catalog.Select(100).Id == "mid" && catalog.Select(199.999).Id == "mid", "Inclusive minimum and disabled row excluded", checks);
        Check(catalog.FormatScore(99.9999) == 100d.ToString("0.000") + " 미만" &&
            catalog.FormatScore(100) == 100d.ToString("0.000"), "Rounded display distinguishes a near miss from an exact threshold", checks);
        Check(catalog.Select(200).Id == "high" && catalog.Select(double.MaxValue).Id == "high", "Highest ending includes boundary and scores above final threshold", checks);
        Reject(() => catalog.Select(-1), "Reject negative total", checks);
        Reject(() => catalog.Select(double.NaN), "Reject NaN total", checks);
        Reject(() => catalog.Select(double.PositiveInfinity), "Reject infinite total", checks);
        Reject(() => new GameDevEndingCatalog(new[] { Ending("x", 10) }), "Require active zero-score baseline", checks);
        Reject(() => new GameDevEndingCatalog(new[] { Ending("x", 0, false) }), "Reject all-disabled catalog", checks);
        Reject(() => new GameDevEndingCatalog(new[] { Ending("a", 0), Ending("b", 0) }), "Reject ambiguous active thresholds", checks);
        Reject(() => new GameDevEndingCatalog(new[] { Ending("a", 0), Ending("a", 100) }), "Reject duplicate IDs", checks);
        Reject(() => new GameDevEnding("a", " ", "body", 0, true), "Reject missing ending title", checks);
        Reject(() => Ending("a", -1), "Reject negative threshold", checks);

        var plan = DataImporter.Preview(DataToolSettings.instance.csvFolder);
        var actual = GameDevEndingCatalog.FromTable(plan.Tables.Single(t => t.Name == GameDevKeys.EndingsTable));
        var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataImporter.DatabasePath);
        Check(database != null && database.SourceHash == plan.SourceHash, "CSV and generated DB fingerprints match", checks);
        database.Initialize();
        Check(database.Tables.Count == plan.Tables.Count && plan.Tables.All(t => database.GetTable(t.Name).Count == t.Count), "Shared database matches every current CSV table", checks);
        var compiled = GameDevEndingCatalog.FromTable(database.GetTable(GameDevKeys.EndingsTable));
        Check(compiled.FormatScore(0.9999) == 1d.ToString("0.000") + " 미만" &&
            compiled.FormatScore(1) == 1d.ToString("0.000"), "Only complete development displays an exact 1.000", checks);
        var scoring = new GameDevFinalScoreCalculator();
        float maximum = GameDevProgress.MaximumPercent;
        Check(scoring.TryCalculate(new GameDevProgress(maximum, maximum, maximum), out double maxScore) &&
            maxScore == 1 && compiled.Select(maxScore).Id == compiled.Active.Last().Id, "Full development earns one point and the highest ending", checks);
        foreach (var ending in actual.Active)
            Check(compiled.Select(ending.MinimumScore).Id == ending.Id, "Imported boundary: " + ending.Id, checks);
        var edited = CsvTableReader.Read("GameDevEndings.csv", "id:string,displayName:string,description:string,minimumScore:double,enabled:bool\na,가,설명,0,true\nb,나,설명,125,true\n");
        Check(GameDevEndingCatalog.FromTable(edited).Select(124).Id == "a" && GameDevEndingCatalog.FromTable(edited).Select(125).Id == "b", "Changed CSV threshold changes branch without code changes (in-memory fixture)", checks);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevSetup.PrefabPath);
        Check(prefab != null && prefab.GetComponent<DevGageUI>() != null, "Serialized UI prefab exists outside Play", checks);
        Check(prefab.GetComponentsInChildren<Image>(true).All(i => i.sprite != null), "Every UI Image has a real Sprite", checks);
        Check(prefab.GetComponentsInChildren<TMP_Text>(true).All(t => t.font != null), "All labels have the Korean font", checks);
        Check(prefab.GetComponentsInChildren<Image>(true).Count(i => i.type == Image.Type.Filled && i.fillMethod == Image.FillMethod.Horizontal) == 3, "Three stored horizontal progress fills", checks);
        Check(prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts in prefab", checks);
        return Report("Edit", checks);
    }

    public static string CheckShelterHud()
    {
        var checks = new List<string>();
        var hud = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevSetup.PrefabPath);
        var viewport = hud.transform.Find("Viewport 1920x1080");
        var panel = viewport.Find("Development panel");
        var fields = new[] { "Planning", "Programming", "Graphics" }
            .Select(name => (RectTransform)panel.Find(name)).ToArray();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var fill = field.Find("Track/Fill").GetComponent<Image>();
            Check(fill.sprite && fill.type == Image.Type.Filled && fill.fillMethod == Image.FillMethod.Horizontal,
                field.name + " has its own horizontal fill", checks);
            if (i > 0)
                Check(field.anchoredPosition.y == fields[0].anchoredPosition.y &&
                    field.anchoredPosition.x > fields[i - 1].anchoredPosition.x + fields[i - 1].rect.width,
                    field.name + " shares the row with a gap", checks);
        }

        var dial = viewport.Find("Shelter clock");
        var arc = dial.Find("Elapsed day").GetComponent<Image>();
        var time = dial.Find("Time").GetComponent<TMP_Text>();
        Check(time.rectTransform.rect.size == ((RectTransform)dial).rect.size && time.alignment == TextAlignmentOptions.Center,
            "Time label is centred within the clock face", checks);
        var ui = new SerializedObject(hud.GetComponent<DevGageUI>());
        Check(arc.sprite && arc.type == Image.Type.Filled && arc.fillMethod == Image.FillMethod.Radial360 &&
            arc.fillOrigin == (int)Image.Origin360.Top && arc.fillClockwise && ui.FindProperty("clockFill").objectReferenceValue == arc &&
            time.font && ui.FindProperty("clockTime").objectReferenceValue == time, "Clock artwork, radial fill and time are bound", checks);
        var face = (RectTransform)dial;
        var legend = (RectTransform)dial.Find("Event legend");
        var marker = (Image)ui.FindProperty("clockMarkerPrefab").objectReferenceValue;
        var presentation = (Sprite)ui.FindProperty("presentationMarkerSprite").objectReferenceValue;
        Check(face.rect.size == new Vector2(176, 176) && arc.rectTransform.rect.size == face.rect.size,
            "The enlarged clock and radial fill share a 176px face", checks);
        Check(marker && PrefabUtility.IsPartOfPrefabAsset(marker) && marker.sprite && presentation && marker.sprite != presentation &&
            !marker.raycastTarget && legend.Find("Invasion mark").GetComponent<Image>().sprite == marker.sprite &&
            legend.Find("Presentation mark").GetComponent<Image>().sprite == presentation &&
            legend.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget),
            "Two distinct event symbols match the click-through legend and stored marker prefab", checks);
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ShelterCharacterCard.prefab");
        var card = (RectTransform)cardPrefab.transform.Find("Viewport 1920x1080/Character card");
        Check(legend.anchoredPosition.x >= face.rect.width &&
            face.anchoredPosition.x + legend.anchoredPosition.x + legend.rect.width < ((RectTransform)panel).anchoredPosition.x &&
            -card.anchoredPosition.y >= -face.anchoredPosition.y + face.rect.height + 16,
            "Legend, development gauges and character card all clear the clock in the shared 16:9 viewport", checks);

        var survivor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Shelter/Survivor.prefab");
        var identity = survivor.GetComponent<CharacterIdentity>();
        var mental = (Image)new SerializedObject(identity).FindProperty("mentalFill").objectReferenceValue;
        Check(mental && mental.sprite && mental.type == Image.Type.Filled && mental.fillMethod == Image.FillMethod.Horizontal &&
            mental.GetComponentInParent<Canvas>(true) is Canvas canvas && canvas.renderMode == RenderMode.WorldSpace,
            "Survivor world-space mental gauge is bound", checks);
        return string.Join("\n", checks);
    }

    // Check the rendered positions and visibility at any stage, including a paused event on its due tick.
    public static string CheckClockScheduleInPlay()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter shelter Play mode first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<DevGageUI>();
        var simulation = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
        if (!ui || !simulation || !simulation.IsReady) throw new InvalidOperationException("Wait for the shelter simulation.");
        ui.SendMessage("LateUpdate");
        Canvas.ForceUpdateCanvases();
        var bound = new SerializedObject(ui);
        var face = ((Image)bound.FindProperty("clockFill").objectReferenceValue).rectTransform;
        var waves = ((Image)bound.FindProperty("clockMarkerPrefab").objectReferenceValue).sprite;
        var presentations = (Sprite)bound.FindProperty("presentationMarkerSprite").objectReferenceValue;
        var rules = simulation.Rules;
        int visible = 0;
        void Verify(string kind, int tick, Sprite expectedSprite, int lastDispatched)
        {
            var expected = tick > lastDispatched;
            var mark = face.Find(kind + " " + tick) as RectTransform;
            if (!mark || mark.GetComponent<Image>().sprite != expectedSprite || mark.gameObject.activeSelf != expected)
                throw new InvalidOperationException($"Wrong {kind} marker state at scheduled tick {tick}, last dispatched {lastDispatched}.");
            var direction = (Vector2)face.InverseTransformPoint(mark.TransformPoint(mark.rect.center)) - face.rect.center;
            float angle = Vector2.SignedAngle(direction, Vector2.up);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 360f * tick / rules.TotalTicks)) > 0.1f)
                throw new InvalidOperationException($"Wrong clock angle for {kind} at tick {tick}.");
            if (expected) visible++;
        }
        for (long tick = rules.FirstInvasionTick; tick < rules.TotalTicks; tick += rules.InvasionEveryTicks)
            Verify("Invasion", (int)tick, waves, simulation.LastWaveTick);
        foreach (int tick in rules.PresentationTicks)
            if (tick > 0 && tick < rules.TotalTicks) Verify("Presentation", tick, presentations, simulation.LastPresentationTick);
        var active = face.Cast<RectTransform>().Where(mark => mark.gameObject.activeSelf).ToArray();
        if (active.Length != visible) throw new InvalidOperationException("Unexpected clock marker count.");
        for (int i = 0; i < active.Length; i++)
        {
            var first = RectTransformUtility.CalculateRelativeRectTransformBounds(face, active[i]);
            for (int j = i + 1; j < active.Length; j++)
                if (first.Intersects(RectTransformUtility.CalculateRelativeRectTransformBounds(face, active[j])))
                    throw new InvalidOperationException($"Clock markers overlap: {active[i].name}, {active[j].name}.");
        }
        return $"PASS tick {simulation.CurrentTick}: {visible} upcoming markers, wave dispatched {simulation.LastWaveTick}, presentation dispatched {simulation.LastPresentationTick}.";
    }

    public static async Task<string> RunPlay()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var checks = new List<string>();
        var manager = UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
        if (!manager) throw new InvalidOperationException("Open ShelterPrototype before Play checks.");
        await manager.InitializeAsync();
        var simulation = UnityEngine.Object.FindFirstObjectByType<UnityGameJam.Shelter.BunkerSimulation>();
        if (simulation) await simulation.InitializeAsync();
        manager.SetScoreCalculator(new GameDevScoreCalculator()); // Explicitly test the external/deferred score contract.
        manager.RestartDevelopment();
        Check(manager.Phase == GameDevPhase.Developing && manager.Progress.Planning == 0 && !manager.TotalScore.HasValue, "Addressable catalog loaded and fresh run starts at zero", checks);
        Check(!manager.SubmitFinalScore(123), "Reject score before development closes", checks);
        manager.AddProgress(GameDevField.Planning, 22.5f);
        manager.SetProgress(GameDevField.Programming, 61);
        manager.SetProgress(GameDevField.Graphics, 99.999f);
        var ui = UnityEngine.Object.FindFirstObjectByType<DevGageUI>();
        ui.Bind(manager); ui.Bind(manager);
        Check(manager.Progress.Planning == 22.5f && manager.Progress.Programming == 61 && manager.Progress.Graphics > 99, "Independent field API updates", checks);
        Check(ui.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Percentage" && t.text == "99.9%"), "UI does not round unfinished work up to 100%", checks);
        int endings = 0;
        Action<GameDevEnding, double> onEnding = (_, __) => endings++;
        manager.EndingReached += onEnding;
        try
        {
            Check(manager.FinishDevelopment() && manager.Phase == GameDevPhase.AwaitingScore && !manager.TotalScore.HasValue, "Deferred formula waits without a fabricated zero-score ending", checks);
            Check(!manager.SetProgress(GameDevField.Planning, 100) && manager.SubmittedProgress.Planning == 22.5f, "Submission freezes progress", checks);
            Check(!manager.FinishDevelopment(), "Repeated finish is ignored", checks);
            Reject(() => manager.SubmitFinalScore(double.NaN), "Invalid score preserves pending state", checks);
            double threshold = GameDevEndingCatalog.FromTable(GameData.GetTable(GameDevKeys.EndingsTable)).Active.Last().MinimumScore;
            Check(manager.SubmitFinalScore(threshold) && manager.Phase == GameDevPhase.Ended && manager.TotalScore == threshold, "External total resolves actual CSV ending", checks);
            Check(endings == 1 && !manager.SubmitFinalScore(0) && endings == 1, "Ending fires once; duplicate result is ignored", checks);
            Check(ui.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Status" && t.text.Contains(manager.Ending.DisplayName)), "Ending title and total reach the UI", checks);
            manager.RestartDevelopment();
            Check(manager.Ending == null && !manager.TotalScore.HasValue && manager.Progress.Graphics == 0, "Restart clears result and all progress", checks);
            manager.SetProgress(GameDevField.Planning, 100);
            manager.SetProgress(GameDevField.Programming, 100);
            Check(manager.Phase == GameDevPhase.Developing, "Two completed fields do not finish the project", checks);
            manager.AddProgress(GameDevField.Graphics, float.MaxValue);
            Check(manager.Phase == GameDevPhase.Developing && manager.Progress.IsComplete && !manager.TotalScore.HasValue && manager.Ending == null,
                "All fields at 100% still wait for an explicit development-end rule", checks);
            Check(manager.FinishDevelopment() && manager.Phase == GameDevPhase.AwaitingScore,
                "Explicit finish hook submits full progress for evaluation", checks);
            manager.SetScoreCalculator(new TestCalculator());
            Check(manager.TryEvaluateScore() && manager.TotalScore == 123, "Replacement score calculator receives the submission", checks);
            manager.SetScoreCalculator(new GameDevScoreCalculator());
            manager.RestartDevelopment();
            ui.enabled = false;
            manager.SetProgress(GameDevField.Planning, 17);
            ui.enabled = true;
            Check(ui.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Percentage" && t.text == "17%"), "Re-enabled UI refreshes missed changes", checks);
        }
        finally
        {
            manager.EndingReached -= onEnding;
            manager.SetScoreCalculator(simulation
                ? (IGameDevScoreCalculator)new GameDevFinalScoreCalculator()
                : new GameDevScoreCalculator());
            manager.RestartDevelopment();
            ui.Bind(manager);
        }
        return Report("Play", checks);
    }

    public static string CheckSpriteReplacement()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        const string path = "Assets/Art/UI/Farming/gauge.png";
        byte[] original = File.ReadAllBytes(path);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId);
        var texture = new Texture2D(2, 2);
        try
        {
            texture.LoadImage(original);
            texture.SetPixel(0, 0, Color.magenta);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var updated = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(updated, out string updatedGuid, out long updatedId);
            if (guid != updatedGuid || localId != updatedId) throw new InvalidOperationException("Sprite identity changed.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevSetup.PrefabPath);
            if (prefab.GetComponentsInChildren<Image>(true).Where(i => i.type == Image.Type.Filled && i.fillMethod == Image.FillMethod.Horizontal).Any(i => i.sprite != updated))
                throw new InvalidOperationException("A progress fill lost its Sprite reference.");
        }
        finally
        {
            File.WriteAllBytes(path, original);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        return "PASS: actual PNG byte replacement/reimport preserved GUID " + guid + " / local ID " + localId + "; restored original bytes.";
    }

    sealed class TestCalculator : IGameDevScoreCalculator
    {
        public bool TryCalculate(GameDevProgress progress, out double totalScore)
        {
            if (!progress.IsComplete) throw new InvalidOperationException("Expected full submitted snapshot.");
            totalScore = 123; return true;
        }
    }
    static GameDevEnding Ending(string id, double score, bool enabled = true) => new GameDevEnding(id, id, "test description", score, enabled);
    static void Check(bool condition, string description, List<string> checks)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        checks.Add(description);
    }
    static void Reject(Action action, string description, List<string> checks)
    {
        try { action(); }
        catch (ArgumentException) { checks.Add(description); return; }
        catch (FormatException) { checks.Add(description); return; }
        throw new InvalidOperationException("FAIL: " + description);
    }
    static string Report(string mode, List<string> checks)
    {
        string report = "PASS: " + checks.Count + " " + mode + " checks\n" + string.Join("\n", checks);
        Directory.CreateDirectory("Docs/GameDev");
        File.WriteAllText("Docs/GameDev/Verification-" + mode.ToLowerInvariant() + ".txt", report);
        return report;
    }
}
