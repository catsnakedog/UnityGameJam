using System;
using System.Collections.Generic;
using System.IO;
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
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var passed = new List<string>();
        void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL " + label); passed.Add("PASS " + label); }
        try
        {
            var rules = AssetDatabase.LoadAssetAtPath<ElevatorRules>(ShelterElevatorSetup.RulesPath); rules.Validate();
            var graph = new GameObject("Graph").AddComponent<ShelterNavigation>();
            var commander = graph.gameObject.AddComponent<ShelterCommander>(); commander.navigation = graph;
            var lower = Area("Lower", 0, 0, 12); var middle = Area("Middle", 0, 4, 12); var upper = Area("Upper", 0, 8, 12);
            var lift = new GameObject("Lift").AddComponent<ShelterElevator>(); lift.height = 8;
            graph.Refresh(); lift.Configure(rules, null, graph); graph.Refresh();
            var a = Actor(graph, "A", new Vector2(-4, 0)); var b = Actor(graph, "B", new Vector2(4, 8)); var c = Actor(graph, "C", new Vector2(-5, 0));
            var actors = new[] { a, b, c };
            void Step(float dt = 0.02f) { lift.Advance(dt); foreach (var actor in actors) if (actor.isActiveAndEnabled) actor.Tick(dt); }
            void Until(Func<bool> done, string label) { for (int i = 0; i < 3000 && !done(); i++) Step(); Check(done(), label); }
            void Reset() { lift.ResetService(); foreach (var actor in actors) actor.Stop(); a.transform.position = new Vector3(-4, 0); b.transform.position = new Vector3(4, 8); c.transform.position = new Vector3(-5, 0); }
            Check(lift.Stops.SequenceEqual(new[] { 0f, 4f, 8f }), "Three floors auto-discovered from area placement");
            Check(!commander.ClickWorld(new Vector2(0, 10)), "Room command needs selected character");
            Check(commander.ClickWorld(new Vector2(-4, 1)) && commander.Selected == a, "Character selection precedes room selection");
            var movementLock = new object();
            a.AcquireMovementLock(movementLock);
            Check(!commander.ClickWorld(new Vector2(0, 10)) && commander.Selected == a, "Rejected move keeps the survivor selected");
            a.ReleaseMovementLock(movementLock);
            Check(commander.ClickWorld(new Vector2(0, 10)) && a.Destination == upper && !commander.Selected,
                "Accepted room move starts the elevator route and clears selection");
            Check(lift.Owner == a && !lift.TryBoard(b), "An actor without a request cannot board");
            b.MoveTo(lower); c.MoveTo(middle);
            Check(lift.QueueCount == 3 && lift.Owner == a, "Opposite-floor callers queue behind first owner");
            int version = graph.Version;
            Until(() => lift.Phase == ElevatorPhase.Travelling, "Owner boards cabin and begins trip");
            Check(lift.PassengerCount == 2 && c.IsInElevator && !b.IsInElevator &&
                Mathf.Abs(a.transform.position.x - c.transform.position.x) > 0.5f, "Two waiters from the same side take separate seats");
            for (int i = 0; i < 25; i++) { b.MoveTo(lower); graph.Refresh(); Step(); }
            Check(lift.Owner == a && lift.TripDestination == 4 && graph.Version == version, "Serve the on-board intermediate stop before an outside call");
            Check(a.IsInElevator && !a.IsClimbing && Mathf.Abs(a.transform.position.y - lift.CabinY) < 0.001f, "Passenger carried inside cabin without climb state");
            Check(!a.CanWorkInCurrentRoom, "Passenger cannot perform room work");
            Until(() => !c.IsInElevator, "Companion gets off at the intermediate floor");
            Check(a.IsInElevator && lift.TripDestination == 8 && !b.IsInElevator, "First rider continues to own floor while other callers wait");
            Until(() => lift.Owner == b, "Oldest outstanding caller gets the next pickup");
            Check(!a.IsInElevator && Mathf.Abs(a.transform.position.y - 8) < 0.001f && !c.IsInElevator, "Previous owner exits before next passenger boards");
            Until(() => actors.All(x => !x.IsMoving), "All concurrent routes finish without call ping-pong");
            Check(a.CurrentRoom == upper && b.CurrentRoom == lower && c.CurrentRoom == middle, "Independent destination rooms retained");
            Reset();
            b.transform.position = lift.CallPoint(0, 1);
            a.MoveTo(upper); c.MoveTo(upper); b.MoveTo(upper);
            Until(() => lift.Phase == ElevatorPhase.Travelling, "Three same-floor requests start a shared trip");
            Check(a.IsInElevator && c.IsInElevator && !b.IsInElevator && lift.HasRequest(b), "Third waiter keeps their place for the next trip");
            Until(() => actors.All(x => !x.IsMoving), "All three same-floor callers reach the upper room");
            Reset();
            b.transform.position = lift.CallPoint(0, 1);
            var across = Area("Across", 8, 0, 4);
            a.MoveTo(upper); b.MoveTo(upper); c.MoveTo(across);
            Until(() => lift.PassengerCount == 2, "Waiters on opposite sides can share the cabin");
            Check(!lift.HasRequest(c) && !c.RequestedElevator, "Same-floor passer has no elevator reservation");
            Until(() => c.CurrentRoom == across, "Passer walks across the occupied shaft");
            Check(a.IsInElevator && b.IsInElevator && c.transform.position.x > lift.Bottom.x, "Horizontal passage is independent of cabin travel");
            a.gameObject.SetActive(false);
            Until(() => b.CurrentRoom == upper, "Removing one rider in transit still delivers the other");
            a.gameObject.SetActive(true);
            Reset();
            a.transform.position = lift.CallPoint(4, -1); b.transform.position = lift.CallPoint(4, 1);
            a.MoveTo(upper); b.MoveTo(lower);
            Until(() => lift.Phase == ElevatorPhase.Travelling, "Opposite destinations can share a pickup floor");
            Check(a.IsInElevator && b.IsInElevator && lift.TripDestination == 8, "Opposite-direction pair visits first caller's floor first");
            Until(() => !a.IsMoving && !b.IsMoving, "Opposite-direction pair reaches both floors");
            Check(a.CurrentRoom == upper && b.CurrentRoom == lower, "Direction change preserves each rider's destination");
            Reset();
            a.transform.position = lift.CallPoint(0, -1); b.transform.position = across.ArrivalPoint(0);
            a.MoveTo(upper); b.MoveTo(upper);
            for (int i = 0; i < 200 && lift.Phase != ElevatorPhase.Travelling; i++) { lift.Advance(0.02f); a.Tick(0.02f); }
            Check(lift.Phase == ElevatorPhase.Travelling && lift.PassengerCount == 1 && lift.HasRequest(b), "A distant reservation never holds a solo departure");
            Reset(); b.MoveTo(lower);
            Check(lift.Phase == ElevatorPhase.Calling && !lift.TryBoard(b) && !b.IsInElevator, "Wrong-floor cabin cannot be boarded");
            Until(() => b.IsInElevator, "Empty cabin arrives at caller floor before boarding");
            Until(() => !b.IsMoving, "Recalled cabin completes downward trip");
            Reset(); a.MoveTo(upper); Until(() => lift.Phase == ElevatorPhase.Travelling, "Start destination-change fixture");
            Vector3 before = a.transform.position; a.MoveTo(middle);
            Check(a.transform.position == before && lift.TripDestination == 8, "Midride command does not teleport or reverse cabin");
            bool reachedOriginal = false;
            for (int i = 0; i < 3000 && a.IsMoving; i++) { Step(); reachedOriginal |= Mathf.Abs(a.transform.position.y - 8) < 0.001f; }
            Check(reachedOriginal && a.CurrentRoom == middle && Vector2.Distance(a.transform.position, middle.ArrivalPoint(a.arrivalOffset)) < 0.002f, "New destination applied after original unloading");
            Reset(); a.MoveTo(upper); Until(() => lift.Phase == ElevatorPhase.Travelling, "Start stop fixture"); a.Stop();
            Until(() => !a.IsInElevator, "Stop command safely completes current ride");
            Check(!a.IsMoving && Mathf.Abs(a.transform.position.y - 8) < 0.001f && lift.QueueCount == 0, "Stopped passenger released at destination floor");
            Reset(); a.MoveTo(upper); b.MoveTo(lower); object gate = new object(); a.AcquireMovementLock(gate); Step();
            Check(lift.Owner == b && !lift.HasRequest(a), "Locked waiting owner releases control to next caller"); a.ReleaseMovementLock(gate);
            a.Stop(); b.Stop(); Reset(); a.MoveTo(upper); b.MoveTo(lower); a.gameObject.SetActive(false); Step();
            Check(lift.Owner == b, "Disabled caller cannot hold elevator forever"); a.gameObject.SetActive(true);
            Reset(); a.MoveTo(upper); Until(() => lift.Phase == ElevatorPhase.Travelling, "Start in-flight lock fixture"); a.AcquireMovementLock(gate);
            Until(() => !a.IsInElevator, "In-flight lock still permits safe unloading"); a.ReleaseMovementLock(gate);
            Reset(); a.MoveTo(upper); Until(() => lift.Phase == ElevatorPhase.Travelling, "Start disconnected lift fixture"); Step(); lift.gameObject.SetActive(false); graph.Refresh();
            Check(!a.IsInElevator && !a.IsMoving && graph.AreaAt(a.transform.position), "Disabled cabin releases passenger onto connected floor");
            Check(!a.MoveTo(upper), "No remaining ladder fallback across disconnected floors"); lift.gameObject.SetActive(true); graph.Refresh();
            Check(a.MoveTo(upper), "Re-enabled elevator reconnects automatically"); Until(() => !a.IsMoving, "Route works after reconnect");
            var isolated = Area("Isolated", 30, 8, 4); Check(!a.MoveTo(isolated), "Disconnected room rejected");
            lift.ResetService(); middle.transform.position = new Vector3(0, 5); graph.Refresh();
            Check(lift.Stops.SequenceEqual(new[] { 0f, 5f, 8f }), "Moved floor changes stops automatically");
            foreach (int fps in new[] { 30, 60, 144 })
            { var timed = Actor(graph, "Speed", new Vector2(-5, 0)); timed.MoveTo(lower); for (int i = 0; i < fps; i++) timed.Tick(1f / fps); Check(Mathf.Abs(timed.transform.position.x + 5 - timed.walkSpeed) < 0.002f, "Walking speed at " + fps + " FPS"); }
            var invalid = UnityEngine.Object.Instantiate(rules);
            try { var so = new SerializedObject(invalid); so.FindProperty("travelSpeed").floatValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); bool rejected = false; try { invalid.Validate(); } catch (InvalidOperationException) { rejected = true; } Check(rejected, "Zero elevator speed rejected"); }
            finally { UnityEngine.Object.DestroyImmediate(invalid); }
            string report = string.Join("\n", passed); File.WriteAllText("Docs/Elevators-checks-edit.txt", report); return report;
        }
        catch (Exception e) { File.WriteAllText("Docs/Elevators-checks-edit.txt", string.Join("\n", passed) + "\n" + e); throw; }
        finally { EditorSceneManager.CloseScene(scene, true); if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
    }
    static ShelterArea Area(string name, float x, float y, float width) { var a = new GameObject(name).AddComponent<ShelterArea>(); a.transform.position = new Vector3(x, y); a.width = width; a.height = 3; return a; }
    static ShelterWalker Actor(ShelterNavigation graph, string name, Vector2 point) { var a = new GameObject(name).AddComponent<ShelterWalker>(); a.navigation = graph; a.characterName = name; a.transform.position = point; return a; }
}
