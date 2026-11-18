using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Shelter;

public static class ShelterMovementChecks
{
    [MenuItem("Tools/Shelter/Validate Click Navigation")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run layout checks outside Play mode.");
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var passed = new List<string>();
        try
        {
            var graph = new GameObject("Test graph").AddComponent<ShelterNavigation>();
            var commander = graph.gameObject.AddComponent<ShelterCommander>();
            commander.navigation = graph;
            var a = Area("A", 0f, 0f, 4f);
            var corridor = Area("Passage", 3f, 0f, 2f);
            corridor.kind = ShelterArea.AreaKind.Corridor;
            var b = Area("B", 6f, 0f, 4f);
            var c = Area("C", 6f, 4f, 4f);
            var d = Area("D", 0f, 4f, 4f);
            Area("Upper passage", 3f, 4f, 2f);
            var ladder = Ladder(6f, 0f, 4f);
            var actor = Actor(graph, "One", new Vector2(-1f, 0f));
            var other = Actor(graph, "Two", new Vector2(1f, 0f));
            graph.Refresh();
            Check(!commander.ClickWorld(new Vector2(6f, 6.8f)), "Room click requires a selected character", passed);
            Check(commander.ClickWorld(new Vector2(-1f, 1f)) && commander.Selected == actor,
                "Character selection takes priority over room click", passed);
            Check(commander.ClickWorld(new Vector2(6f, 6.8f)) && actor.Destination == c,
                "Room click issues an automatic route", passed);
            Advance(actor, 15f);
            Check(At(actor, c) && !actor.IsMoving, "Cross room, corridor and ladder to arrive upstairs", passed);
            Check(Vector2.Distance(other.transform.position, new Vector2(1f, 0f)) < 0.001f,
                "Uncommanded character stays still", passed);
            Check(actor.MoveTo(a), "Downward route exists", passed);
            Check(commander.ClickWorld(new Vector2(1f, 1f)) && commander.Selected == other && other.MoveTo(d),
                "Switch selection and give independent orders", passed);
            for (int i = 0; i < 1200; i++) { actor.Tick(1f / 60f); other.Tick(1f / 60f); }
            Check(At(actor, a) && At(other, d), "Two characters finish different routes simultaneously", passed);

            var isolated = Area("Disconnected", 20f, 4f, 4f);
            Check(!actor.MoveTo(isolated), "Disconnected room is rejected", passed);
            corridor.accessible = false;
            Check(!actor.MoveTo(b), "Closing a corridor blocks the connection", passed);
            corridor.accessible = true;
            Check(actor.MoveTo(b), "Reopening the corridor restores routing without manual links", passed);
            corridor.transform.position += Vector3.right;
            graph.Refresh();
            actor.Tick(1f / 60f);
            Check(!actor.IsMoving && actor.Status == "NO ROUTE", "Moving a corridor away cancels the broken route", passed);
            corridor.transform.position -= Vector3.right;
            Check(actor.MoveTo(b), "Moving a corridor back reconnects it", passed);
            ladder.transform.position += Vector3.right * 10f;
            Check(!actor.MoveTo(c), "A misplaced ladder cannot connect floors", passed);
            ladder.transform.position -= Vector3.right * 10f;
            Check(actor.MoveTo(c), "Repositioned ladder auto-connects its ends", passed);

            actor.transform.position = new Vector3(6f, 0f);
            actor.Stop();
            Check(actor.MoveTo(c), "Start ascent", passed);
            Advance(actor, 0.5f);
            Vector3 halfway = actor.transform.position;
            Check(actor.IsClimbing && halfway.y > 0f && halfway.y < 4f, "Climb animation state on vertical route", passed);
            Check(actor.MoveTo(b) && Vector3.Distance(actor.transform.position, halfway) < 0.0001f,
                "Change destination mid-ladder without teleporting", passed);
            Advance(actor, 5f);
            Check(At(actor, b), "Reverse midway and finish downstairs", passed);
            actor.MoveTo(c);
            Advance(actor, 0.5f);
            actor.Stop();
            halfway = actor.transform.position;
            Advance(actor, 1f);
            Check(Vector3.Distance(actor.transform.position, halfway) < 0.0001f && actor.MoveTo(c),
                "Stopped climber can resume a new route", passed);
            ladder.accessible = false;
            graph.Refresh();
            actor.Tick(1f / 60f);
            Check(!actor.IsMoving && Vector3.Distance(actor.transform.position, halfway) < 0.0001f,
                "Removed ladder stops a climber safely", passed);
            ladder.accessible = true;

            var shortLadder = Ladder(0f, 0f, 4f);
            actor.transform.position = Vector3.zero;
            actor.Stop();
            Check(graph.TryFindPath(Vector2.zero, null, d, 0f, 3.2f, 2.2f, out var shortest) &&
                shortest.Any(p => p.ladder == shortLadder) && !shortest.Any(p => p.ladder == ladder),
                "Choose the shortest travel-time route among multiple ladders", passed);
            shortLadder.gameObject.SetActive(false);

            var translated = Area("Translated room", 30f, 10f, 4f);
            var translatedTop = Area("Translated upper room", 30f, 14f, 4f);
            Ladder(30f, 10f, 4f);
            Check(graph.TryFindPath(new Vector2(30f, 10f), null, translatedTop, 0f, 3.2f, 2.2f, out _),
                "Arbitrary world positions and floor heights work", passed);
            translated.transform.localScale = new Vector3(2f, 1f, 1f);
            Check(Mathf.Abs(translated.Left - 26f) < 0.001f && Mathf.Abs(translated.Right - 34f) < 0.001f,
                "Scaled room uses transformed walkable bounds", passed);

            foreach (int fps in new[] { 30, 60, 144 })
            {
                var timed = Actor(graph, "Frame check", Vector2.zero);
                timed.MoveTo(b);
                for (int i = 0; i < fps; i++) timed.Tick(1f / fps);
                Check(Mathf.Abs(timed.transform.position.x - timed.walkSpeed) < 0.002f,
                    $"Automatic walking speed at {fps} FPS", passed);
            }
            return $"PASS: {passed.Count} click/navigation checks\n" + string.Join("\n", passed);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    private static ShelterArea Area(string name, float x, float y, float width)
    {
        var a = new GameObject(name).AddComponent<ShelterArea>();
        a.transform.position = new Vector3(x, y);
        a.width = width;
        a.height = 4f;
        return a;
    }
    private static ShelterLadder Ladder(float x, float y, float height)
    {
        var l = new GameObject("Test ladder").AddComponent<ShelterLadder>();
        l.transform.position = new Vector3(x, y);
        l.height = height;
        return l;
    }
    private static ShelterWalker Actor(ShelterNavigation graph, string name, Vector2 point)
    {
        var a = new GameObject(name).AddComponent<ShelterWalker>();
        a.transform.position = point;
        a.navigation = graph;
        return a;
    }
    private static void Advance(ShelterWalker a, float seconds)
    { for (int i = 0; i < Mathf.RoundToInt(seconds * 60f); i++) a.Tick(1f / 60f); }
    private static bool At(ShelterWalker a, ShelterArea area) => Vector2.Distance(a.transform.position, area.ArrivalPoint(a.arrivalOffset)) < 0.002f;
    private static void Check(bool ok, string name, List<string> passed)
    { if (!ok) throw new InvalidOperationException("FAIL: " + name); passed.Add(name); }
}
