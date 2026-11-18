using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Shelter;

public static class ShelterNavigationSetup
{
    public const string PrefabFolder = "Assets/Prefabs/Shelter";
    private static Sprite Block => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shelter/Block.png");
    private static Material Material => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Shelter/Blockout.mat");

    [MenuItem("Tools/Shelter/Legacy/Configure Ladder Navigation")]
    public static void ConfigureScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ShelterPrototypeBuilder.ScenePath && scene.path != "") throw new InvalidOperationException("Open ShelterPrototype first.");
        if (SceneObjects<ShelterNavigation>().Length > 0) return;
        var graph = EnsureManager();
        var roomRoot = new GameObject("02 - Placeable rooms and corridors").transform;
        foreach (var floor in SceneObjects<ShelterFloor>())
        {
            foreach (var room in floor.transform.Cast<Transform>().ToArray())
            {
                var wall = room.Find("Plaster rear wall");
                if (wall == null) continue;
                Vector3 pivot = new Vector3(wall.position.x, floor.Height, 0f);
                MovePivot(room, pivot);
                room.SetParent(roomRoot, true);
                var area = room.gameObject.AddComponent<ShelterArea>();
                area.displayName = room.name;
                area.width = 8.65f;
                area.height = 3.8f;
                area.backgroundVisual = wall;
                area.nameLabel = room.Find("Room label").GetComponent<TextMesh>();
                area.floorVisual = MakeRect("Walkable floor", room, ColorFrom("14191B"), 15).transform;
                area.highlight = MakeRect("Destination highlight", room, ColorFrom("D9B66D"), 31);
                area.ShowHighlight(false);
                area.RefreshVisuals();
            }
            UnityEngine.Object.DestroyImmediate(floor.gameObject);
        }
        var building = GameObject.Find("02 - Shelter cutaway");
        if (building != null)
        {
            foreach (var child in building.transform.Cast<Transform>().ToArray())
                if (child.name.Contains("Floor") || child.name.Contains("floor") || child.name.Contains("wall") ||
                    child.name.Contains("slab") || child.name == "Foundation" || child.name.StartsWith("Roof"))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        foreach (var ladder in SceneObjects<ShelterLadder>())
        {
            if (ladder.ladderVisual != null) continue;
            var children = ladder.transform.Cast<Transform>().ToArray();
            var visual = new GameObject("Ladder visual (normalized)").transform;
            visual.SetParent(ladder.transform, false);
            foreach (var child in children)
            {
                if (child.GetComponent<TextMesh>() != null) { UnityEngine.Object.DestroyImmediate(child.gameObject); continue; }
                child.SetParent(visual, false);
                child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y / ladder.height, child.localPosition.z);
                child.localScale = new Vector3(child.localScale.x, child.localScale.y / ladder.height, child.localScale.z);
            }
            ladder.ladderVisual = visual;
            ladder.RefreshVisuals();
        }
        var source = SceneObjects<ShelterWalker>().First();
        ConfigureActor(source, graph, "MIRA", -0.65f);
        var second = UnityEngine.Object.Instantiate(source);
        second.transform.position = new Vector3(1.8f, 0f, 0f);
        ConfigureActor(second, graph, "JIN", 0f);
        TintCoat(second, ColorFrom("7D9C9C"));
        var third = UnityEngine.Object.Instantiate(source);
        third.transform.position = new Vector3(9.8f, 3.8f, 0f);
        ConfigureActor(third, graph, "NOA", 0.65f);
        TintCoat(third, ColorFrom("B08C78"));

        SetNote("Subtitle", "SIDE VIEW  /  CLICK TO COMMAND");
        SetNote("Walk instructions", "CLICK A SURVIVOR  >  CLICK A ROOM");
        SetNote("Climb instructions", "");
        SetNote("Reset instructions", "3 SURVIVORS");
        var status = SetNote("Arrow instructions", "ROOMS + CORRIDORS + LADDERS CONNECT AUTOMATICALLY");
        graph.GetComponent<ShelterCommander>().statusLabel = status;
        graph.GetComponent<ShelterCommander>().view = SceneObjects<Camera>().FirstOrDefault(c => c.CompareTag("MainCamera"));
        graph.Refresh();
        CreatePrefabs(source, SceneObjects<ShelterArea>().First(), SceneObjects<ShelterLadder>().First());
        EditorSceneManager.MarkSceneDirty(scene);
        if (scene.path != "") EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void MovePivot(Transform root, Vector3 position)
    {
        var children = root.Cast<Transform>().ToArray();
        var positions = children.Select(c => c.position).ToArray();
        root.position = position;
        for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
    }

    private static void ConfigureActor(ShelterWalker actor, ShelterNavigation graph, string name, float offset)
    {
        actor.name = "Survivor - " + name;
        actor.characterName = name;
        actor.arrivalOffset = offset;
        actor.navigation = graph;
        if (actor.selectionMarker == null)
        {
            actor.selectionMarker = MakeRect("Selection marker", actor.transform, ColorFrom("E5BF71"), 32);
            actor.selectionMarker.transform.localPosition = new Vector3(0f, 0.05f);
            actor.selectionMarker.transform.localScale = new Vector3(0.95f, 0.09f, 1f);
        }
        if (actor.nameLabel == null) actor.nameLabel = MakeLabel("Survivor name", actor.transform, name);
        actor.nameLabel.text = name;
        actor.nameLabel.anchor = TextAnchor.MiddleCenter;
        actor.nameLabel.transform.localPosition = new Vector3(0f, 2.15f);
        actor.nameLabel.characterSize = 0.035f;
        actor.SetSelected(false);
    }

    private static void TintCoat(ShelterWalker actor, Color color)
    {
        var coat = actor.visual.Find("Coat");
        if (coat != null) coat.GetComponent<SpriteRenderer>().color = color;
    }

    private static void CreatePrefabs(ShelterWalker source, ShelterArea room, ShelterLadder ladder)
    {
        Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();
        var actorCopy = UnityEngine.Object.Instantiate(source);
        actorCopy.transform.position = Vector3.zero;
        actorCopy.navigation = null;
        actorCopy.characterName = "SURVIVOR";
        actorCopy.nameLabel.text = "SURVIVOR";
        SavePiece(actorCopy.gameObject, "Survivor");

        var roomCopy = UnityEngine.Object.Instantiate(room);
        roomCopy.transform.position = Vector3.zero;
        roomCopy.displayName = "ROOM";
        roomCopy.width = 8f;
        roomCopy.height = 4f;
        roomCopy.RefreshVisuals();
        SavePiece(roomCopy.gameObject, "Room");

        var corridor = new GameObject("Corridor").AddComponent<ShelterArea>();
        corridor.kind = ShelterArea.AreaKind.Corridor;
        corridor.displayName = "CORRIDOR";
        corridor.width = 2f;
        corridor.height = 4f;
        corridor.backgroundVisual = MakeRect("Passage rear wall", corridor.transform, ColorFrom("303A3C"), -15).transform;
        corridor.floorVisual = MakeRect("Walkable floor", corridor.transform, ColorFrom("14191B"), 15).transform;
        corridor.nameLabel = MakeLabel("Corridor label", corridor.transform, "CORRIDOR");
        corridor.nameLabel.characterSize = 0.025f;
        corridor.highlight = MakeRect("Destination highlight", corridor.transform, ColorFrom("D9B66D"), 31);
        corridor.RefreshVisuals();
        corridor.ShowHighlight(false);
        SavePiece(corridor.gameObject, "Corridor");

        var ladderCopy = UnityEngine.Object.Instantiate(ladder);
        ladderCopy.transform.position = Vector3.zero;
        ladderCopy.height = 4f;
        ladderCopy.RefreshVisuals();
        SavePiece(ladderCopy.gameObject, "Ladder");
    }

    private static void SavePiece(GameObject piece, string name)
    {
        piece.name = name;
        PrefabUtility.SaveAsPrefabAsset(piece, PrefabFolder + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(piece);
    }

    public static ShelterNavigation EnsureManager()
    {
        var graph = SceneObjects<ShelterNavigation>().FirstOrDefault();
        if (graph == null) graph = new GameObject("Shelter navigation and selection").AddComponent<ShelterNavigation>();
        var commander = graph.GetComponent<ShelterCommander>();
        if (commander == null) commander = graph.gameObject.AddComponent<ShelterCommander>();
        commander.navigation = graph;
        commander.view = SceneObjects<Camera>().FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (commander.view == null)
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ShelterCamera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5.7f, -10f);
            commander.view = camera.GetComponent<Camera>();
            commander.view.orthographic = true;
            commander.view.orthographicSize = 9.2f;
            commander.view.clearFlags = CameraClearFlags.SolidColor;
            commander.view.backgroundColor = ColorFrom("20292E");
        }
        return graph;
    }

    [MenuItem("GameObject/Shelter/Room", false, 20)] public static void AddRoom() => AddPiece("Room");
    [MenuItem("GameObject/Shelter/Corridor", false, 21)] public static void AddCorridor() => AddPiece("Corridor");
    [MenuItem("GameObject/Shelter/Elevator", false, 22)] public static void AddElevator() => AddPiece("Elevator");
    public static void AddLadder() => AddElevator();
    [MenuItem("GameObject/Shelter/Survivor", false, 23)] public static void AddSurvivor() => AddPiece("Survivor");

    private static void AddPiece(string name)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Place persistent pieces outside Play mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + name + ".prefab");
        if (prefab == null) throw new InvalidOperationException("Configure the ShelterPrototype scene first.");
        EnsureManager();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
        Undo.RegisterCreatedObjectUndo(instance, "Place shelter " + name);
        Selection.activeGameObject = instance;
    }

    private static T[] SceneObjects<T>() where T : Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None)
        .Where(c => c.gameObject.scene == SceneManager.GetActiveScene()).ToArray();

    private static SpriteRenderer MakeRect(string name, Transform parent, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sprite = go.AddComponent<SpriteRenderer>();
        sprite.sprite = Block;
        sprite.sharedMaterial = Material;
        sprite.color = color;
        sprite.sortingOrder = order;
        return sprite;
    }
    private static TextMesh MakeLabel(string name, Transform parent, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMesh>();
        label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NeoDunggeunmo.ttf");
        label.text = text;
        label.fontSize = 64;
        label.characterSize = 0.04f;
        label.color = ColorFrom("BBC5B9");
        label.anchor = TextAnchor.MiddleLeft;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = label.font.material;
        renderer.sortingOrder = 40;
        return label;
    }
    private static TextMesh SetNote(string name, string text)
    {
        var go = GameObject.Find(name);
        if (go == null) return null;
        var label = go.GetComponent<TextMesh>();
        label.text = text;
        return label;
    }
    private static Color ColorFrom(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
}
