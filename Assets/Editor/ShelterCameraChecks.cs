using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShelterRun;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityGameJam.Common;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ShelterCameraChecks
{
    const string ScenePath = "Assets/Scenes/ShelterPrototype.unity";
    static readonly List<string> results = new List<string>();
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        results.Add("PASS: " + message);
    }
    static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.001f;
    static async Task WaitFor(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!ready()) { if (DateTime.UtcNow > deadline) throw new TimeoutException(); await Task.Delay(25); }
    }
    static async Task<ShelterCamera> Load()
    {
        await SceneManager.LoadSceneAsync(ScenePath);
        await WaitFor(() => UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>()?.IsReady == true);
        UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>().Paused = true;
        var camera = UnityEngine.Object.FindFirstObjectByType<ShelterCamera>();
        camera.edgePan = false;
        await WaitFor(() => camera.commander && !camera.commander.InputBlocked);
        return camera;
    }

    public static async Task<string> RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play first.");
        results.Clear();
        float previousScale = Time.timeScale;
        var previousKeyboard = Keyboard.current;
        var previousMouse = Mouse.current;
        Keyboard keyboard = null;
        Mouse mouse = null;
        GameObject field = null;
        try
        {
            Time.timeScale = 0;
            FarmingRunTransfer.Reset();
            var camera = await Load();
            Check(camera.roster.Characters.Count == 0 && !camera.PressCharacterSlot(1), "Direct shelter entry has no invented characters");
            var database = AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset");
            FarmingRunTransfer.Capture(new[] { "person_06", "person_04", "person_02", "person_05", "person_01", "person_03" }, database);
            camera = await Load();
            var view = camera.GetComponent<Camera>();
            Check(camera.roster.Characters.First().Id == "person_06", "Fixture uses non-slot rescue order");
            Check(Mathf.Abs(view.aspect - 16f / 9f) < 0.001f && view.orthographicSize == camera.viewHalfHeight, "16:9 and inspector view size applied");
            var rooms = UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None);
            float mapWidth = rooms.Max(a => a.Right) - rooms.Min(a => a.Left);
            float mapHeight = rooms.Max(a => a.FloorY + a.height) - rooms.Min(a => a.FloorY);
            Check(camera.minHalfHeight * 2 < mapHeight && camera.minHalfHeight * 2 * view.aspect < mapWidth,
                "Close zoom can frame part of the bunker on both axes");

            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Focus();
            void KeyPress(Key key)
            {
                gameView.Focus();
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
                camera.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            }
            void Wheel(float delta, Vector2 position)
            {
                mouse.MakeCurrent();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position, scroll = Vector2.up * delta });
                InputSystem.Update();
                camera.SendMessage("Update");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
            }
            Vector3 Centre(int slot)
            {
                var point = (Vector2)camera.GetCharacterSlot(slot).transform.position + camera.characterOffset;
                return new Vector3(point.x, point.y, camera.transform.position.z);
            }

            var card = UnityEngine.Object.FindFirstObjectByType<ShelterCharacterCard>();
            for (int slot = 1; slot <= 6; slot++)
            {
                var expected = camera.GetCharacterSlot(Math.Min(slot, 4));
                camera.Unlock();
                KeyPress((Key)((int)Key.Digit1 + slot - 1));
                Check(!camera.IsFollowing && Near(camera.transform.position, Centre(slot)), "Digit " + slot + " centres existing person ID without locking");
                Check(camera.commander.Selected == expected.GetComponent<ShelterWalker>() && card.SelectedCharacter == expected,
                    "Digit " + slot + " leaves movement selection and card on " + expected.Id);
                KeyPress((Key)((int)Key.Digit1 + slot - 1));
                Check(camera.IsFollowing && camera.FollowTarget.Id == "person_" + slot.ToString("00"), "Digit " + slot + " double tap locks correct character");
                KeyPress(Key.Y);
                Check(!camera.IsFollowing, "Y unlocks slot " + slot);
                if (slot <= 4) camera.Pan(Vector2.right, 0.1f);
                KeyPress((Key)((int)Key.Numpad1 + slot - 1));
                Check(!camera.IsFollowing && Near(camera.transform.position, Centre(slot)), "Numpad " + slot + " centres correct character");
                Check(camera.commander.Selected == expected.GetComponent<ShelterWalker>() && card.SelectedCharacter == expected,
                    "Numpad " + slot + " leaves movement selection and card on " + expected.Id);
            }
            camera.Unlock(); KeyPress(Key.Digit1); KeyPress(Key.Digit2);
            Check(!camera.IsFollowing && Near(camera.transform.position, Centre(2)), "Different digits do not count as a double tap");
            camera.Unlock(); KeyPress(Key.Digit1);
            await Task.Delay((int)(camera.doubleTapSeconds * 1000) + 120);
            KeyPress(Key.Digit1);
            Check(!camera.IsFollowing, "Slow repeated digit stays unlocked even at timeScale zero");
            KeyPress(Key.Digit1);
            Check(camera.IsFollowing, "Fast second press locks at timeScale zero");
            var target = camera.FollowTarget;
            var originalTargetPosition = target.transform.position;
            target.transform.position += new Vector3(2, 3, 0);
            camera.SendMessage("LateUpdate");
            Check(Near(camera.transform.position, Centre(1)), "Locked camera tracks target on both axes");
            var lockedPosition = camera.transform.position;
            Wheel(1, view.pixelRect.center);
            Check(view.orthographicSize < camera.viewHalfHeight && camera.IsFollowing && Near(camera.transform.position, lockedPosition),
                "Wheel up zooms in while keeping the followed survivor centred");
            Wheel(-1, view.pixelRect.center);
            Check(Mathf.Approximately(view.orthographicSize, camera.viewHalfHeight), "Wheel down reverses a wheel-up step");
            KeyPress(Key.RightArrow);
            Check(Near(camera.transform.position, lockedPosition) && camera.IsFollowing, "Arrow panning cannot break follow lock");
            KeyPress(Key.Y);
            target.transform.position = originalTargetPosition;
            camera.SendMessage("LateUpdate");
            Check(Near(camera.transform.position, lockedPosition), "Y keeps current view when character moves away");
            KeyPress(Key.RightArrow);
            var selectedWalker = target.GetComponent<ShelterWalker>();
            Check(camera.transform.position.x > lockedPosition.x && camera.commander.Selected == selectedWalker && card.SelectedCharacter == target,
                "Arrow input moves free camera while keeping the survivor and card selected");
            foreach (var input in new[] { (Key.W, Vector2.up), (Key.A, Vector2.left), (Key.S, Vector2.down), (Key.D, Vector2.right) })
            {
                camera.transform.position = new Vector3(0, 9, -10);
                var before = camera.transform.position;
                KeyPress(input.Item1);
                Check(Vector2.Dot(camera.transform.position - before, input.Item2) > 0, input.Item1 + " moves free camera");
            }
            camera.transform.position = new Vector3(0, 9, -10);
            camera.Pan(new Vector2(1, 1), 0.1f);
            Check(Mathf.Abs(Vector3.Distance(camera.transform.position, new Vector3(0, 9, -10)) - camera.panSpeed * 0.1f) < 0.001f, "Diagonal pan speed is normalized");

            foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 1024), new Vector2Int(2560, 1080) })
            {
                var rect = FixedAspectCamera.CalculateViewport(resolution.x, resolution.y);
                rect = new Rect(rect.x * resolution.x, rect.y * resolution.y, rect.width * resolution.x, rect.height * resolution.y);
                Check(Mathf.Abs(rect.width / rect.height - 16f / 9f) < 0.001f, resolution + " maintains 16:9");
                Check(camera.EdgeDirection(new Vector2(rect.xMin + 1, rect.center.y), rect) == Vector2.left &&
                    camera.EdgeDirection(new Vector2(rect.center.x, rect.yMax - 1), rect) == Vector2.up, resolution + " pans at rendered viewport edges");
                Check(camera.EdgeDirection(rect.center, rect) == Vector2.zero &&
                    camera.EdgeDirection(new Vector2(rect.xMin - 1, rect.center.y), rect) == Vector2.zero, resolution + " ignores centre/outside/letterbox pointer");
            }

            EventSystem.current.SetSelectedGameObject(null);
            camera.edgePan = true;
            var beforeEdge = camera.transform.position;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(view.pixelRect.xMin + 1, view.pixelRect.center.y) }); InputSystem.Update();
            camera.SendMessage("Update");
            Check(camera.transform.position.x < beforeEdge.x, "Mouse input pans left at viewport edge");
            camera.edgePan = false;
            var block = new object();
            camera.commander.AcquireInputBlock(block);
            var blockedPosition = camera.transform.position;
            float blockedSize = view.orthographicSize;
            Wheel(1, view.pixelRect.center);
            Check(view.orthographicSize == blockedSize, "Modal input block suppresses wheel zoom");
            KeyPress(Key.Digit3); KeyPress(Key.RightArrow);
            Check(Near(camera.transform.position, blockedPosition) && !camera.IsFollowing && camera.commander.Selected == selectedWalker,
                "Modal input block suppresses shortcuts, selection and panning");
            camera.commander.ReleaseInputBlock(block);
            await Task.Delay(60);
            field = new GameObject("Temporary camera input check", typeof(RectTransform), typeof(TMP_InputField));
            EventSystem.current.SetSelectedGameObject(field);
            blockedPosition = camera.transform.position;
            KeyPress(Key.Digit3); KeyPress(Key.RightArrow);
            Check(Near(camera.transform.position, blockedPosition), "Text field focus suppresses camera input");
            EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.Object.Destroy(field); field = null;

            camera.Unlock(); KeyPress(Key.Digit1); KeyPress(Key.Digit1);
            target.gameObject.SetActive(false);
            camera.SendMessage("LateUpdate");
            Check(!camera.IsFollowing && camera.FollowTarget == null, "Inactive follow target releases lock");
            target.gameObject.SetActive(true);
            var absent = camera.GetCharacterSlot(6);
            absent.gameObject.SetActive(false);
            camera.Unlock(); KeyPress(Key.Digit1); KeyPress(Key.Digit1);
            var beforeMissing = camera.transform.position;
            KeyPress(Key.Digit6);
            Check(camera.IsFollowing && camera.FollowTarget == target && Near(camera.transform.position, beforeMissing), "Unavailable slot preserves camera and existing lock");
            absent.gameObject.SetActive(true);
            KeyPress(Key.Y);
            camera.Pan(Vector2.one, 10000);
            Check(camera.transform.position.x <= rooms.Max(a => a.Right) + camera.boundaryPadding + 0.001f &&
                camera.transform.position.y <= rooms.Max(a => a.FloorY + a.height) + camera.boundaryPadding + 0.001f, "Camera cannot escape upper map bounds");
            camera.Pan(-Vector2.one, 10000);
            Check(camera.transform.position.x >= rooms.Min(a => a.Left) - camera.boundaryPadding - 0.001f &&
                camera.transform.position.y >= rooms.Min(a => a.FloorY) - camera.boundaryPadding - 0.001f, "Camera cannot escape lower map bounds");
            float size = camera.viewHalfHeight;
            camera.viewHalfHeight = size * 0.8f; camera.SendMessage("LateUpdate");
            Check(Mathf.Abs(view.orthographicSize - size * 0.8f) < 0.001f, "Inspector size change updates the view live");
            camera.viewHalfHeight = size; camera.SendMessage("LateUpdate");
            Wheel(1, view.pixelRect.min - Vector2.one);
            Check(Mathf.Approximately(view.orthographicSize, size), "Wheel outside the viewport is ignored");
            Wheel(100, view.pixelRect.center);
            Check(Mathf.Approximately(view.orthographicSize, camera.minHalfHeight), "Zoom in stops at the configured minimum");
            Wheel(-100, view.pixelRect.center);
            Check(Mathf.Approximately(view.orthographicSize, camera.maxHalfHeight), "Zoom out stops at the configured maximum");

            // A real sparse rescue roster must keep the gaps in number assignments.
            FarmingRunTransfer.Capture(new[] { "person_06", "person_02" }, database);
            camera = await Load();
            Check(camera.GetCharacterSlot(1) == null && camera.GetCharacterSlot(2)?.Id == "person_02" && camera.GetCharacterSlot(6)?.Id == "person_06", "Sparse rescue roster keeps stable slots 2 and 6");
            Check(!camera.IsFollowing && !camera.PressCharacterSlot(1), "Reload clears follow state and missing slot is harmless");
            // Leave a six-character fixture centred on the common room for visual inspection only.
            FarmingRunTransfer.Capture(Enumerable.Range(1, 6).Select(i => "person_" + i.ToString("00")), database);
            camera = await Load();
            camera.PressCharacterSlot(1);
            Check(!camera.IsFollowing, "Fresh scene starts in free camera mode");
            var invasion = UnityEngine.Object.FindFirstObjectByType<ZombieInvasionController>();
            await invasion.InitializeAsync();
            invasion.Simulation.Paused = false;
            await WaitFor(() => invasion.CanAdvance);
            camera.Pan(Vector2.right, 1000);
            var beforeSpawn = camera.transform.position;
            var beforeSpawnSize = camera.GetComponent<Camera>().orthographicSize;
            var zombie = invasion.SpawnWave();
            camera.SendMessage("LateUpdate");
            Check(zombie && Near(camera.transform.position, beforeSpawn) && camera.GetComponent<Camera>().orthographicSize == beforeSpawnSize,
                "Zombie spawn preserves free camera position and zoom");
            camera.Unlock();
            camera.PressCharacterSlot(1); camera.PressCharacterSlot(1);
            invasion.SpawnWave();
            camera.SendMessage("LateUpdate");
            Check(camera.IsFollowing && Near(camera.transform.position, Centre(1)), "Incoming waves respect an explicit follow lock");
            camera.Unlock();
            invasion.Simulation.Paused = true;
            return string.Join("\n", results);
        }
        catch (Exception e) { results.Add("FAIL: " + e); throw; }
        finally
        {
            if (field) { EventSystem.current?.SetSelectedGameObject(null); UnityEngine.Object.Destroy(field); }
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent(); previousMouse?.MakeCurrent();
            Time.timeScale = previousScale;
            File.WriteAllLines("Docs/ShelterCamera-checks.txt", results);
        }
    }
}
