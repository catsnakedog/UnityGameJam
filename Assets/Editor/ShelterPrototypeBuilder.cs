using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Shelter;

public static class ShelterPrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/ShelterPrototype.unity";
    private static Sprite square;
    private static Sprite circle;
    private static Sprite glow;
    private static Material spriteMaterial;
    private static Font font;
    private static readonly Color Ink = Hex("14191B");
    private static readonly Color Steel = Hex("5D6869");
    private static readonly Color Paper = Hex("C4C5BA");
    private static readonly Color Amber = Hex("C9A96C");

    [MenuItem("Tools/Shelter/Create Movement Prototype")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before creating the scene.");
        var existing = SceneManager.GetActiveScene();
        if (existing.isDirty) throw new InvalidOperationException("Save the active scene before creating the prototype.");
        if (File.Exists(ScenePath)) throw new InvalidOperationException("The prototype already exists. Open it to edit its objects.");

        PrepareAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var background = new GameObject("01 - Distant city").transform;
        DrawCity(background);
        var building = new GameObject("02 - Shelter cutaway").transform;
        var floors = new ShelterFloor[3];
        string[,] names = { { "WORKSHOP", "STORAGE", "ENTRY" },
            { "KITCHEN", "COMMON ROOM", "DORMITORY" }, { "ATTIC", "REST AREA", "LOOKOUT" } };

        Rect("Foundation", building, 0f, -0.56f, 27.2f, 0.85f, Ink, 2);
        for (int level = 0; level < 3; level++)
        {
            float y = level * 3.8f;
            var floorRoot = new GameObject($"Floor {level + 1} - walkable lane").transform;
            floorRoot.SetParent(building, false);
            floorRoot.position = new Vector3(0f, y);
            floors[level] = floorRoot.gameObject.AddComponent<ShelterFloor>();
            for (int room = 0; room < 3; room++)
            {
                float x = (room - 1) * 8.65f;
                DrawRoom(floorRoot, x, y, level, room, names[level, room]);
            }
            DrawFloor(building, y, level == 0 ? (float?)null : level == 1 ? -5.8f : 5.8f);
            Label($"Floor number {level}", building, $"0{level + 1}", -14.1f, y + 0.12f, 0.26f, Amber);
            Rect("Floor guide", building, -13.6f, y + 0.12f, 0.4f, 0.025f, Steel, 5);
        }
        Rect("Left outer wall", building, -13.15f, 5.64f, 0.3f, 11.7f, Ink, 12);
        Rect("Right outer wall", building, 13.15f, 5.64f, 0.3f, 11.7f, Ink, 12);
        Rect("Roof", building, 0f, 11.5f, 26.65f, 0.36f, Ink, 12);
        Rect("Roof coping", building, 0f, 11.72f, 26.8f, 0.08f, Steel, 13);
        Rect("Chimney", building, -10.1f, 12.13f, 0.9f, 0.7f, Ink, 0);
        Rect("Chimney cap", building, -10.1f, 12.5f, 1.12f, 0.12f, Steel, 0);

        var ladderRoot = new GameObject("03 - Ladder connections").transform;
        var ladders = new[]
        {
            DrawLadder(ladderRoot, -5.8f, floors[0], floors[1], "Ladder A - floors 01 to 02"),
            DrawLadder(ladderRoot, 5.8f, floors[1], floors[2], "Ladder B - floors 02 to 03")
        };
        CreateWalker(floors[0], ladders);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ShelterCamera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 5.7f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("20292E");
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;


        ShelterNavigationSetup.ConfigureScene();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = GameObject.Find("Survivor - MIRA");
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 5.5f), Quaternion.identity, 16f, true);
        }
        Debug.Log("Shelter movement prototype saved: " + ScenePath);
    }

    private static void PrepareAssets()
    {
        Directory.CreateDirectory("Assets/Art/Shelter");
        Directory.CreateDirectory("Assets/Materials/Shelter");
        AssetDatabase.Refresh();
        square = MakeSprite("Block", 4, (x, y) => Color.white);
        circle = MakeSprite("Disc", 64, (x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01((0.5f - Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f)) * 64f)));
        glow = MakeSprite("WindowGlow", 64, (x, y) => new Color(1f, 1f, 1f,
            Mathf.Pow(Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f) * 2f), 2f)));
        const string materialPath = "Assets/Materials/Shelter/Blockout.mat";
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (spriteMaterial == null)
        {
            spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(spriteMaterial, materialPath);
        }
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NeoDunggeunmo.ttf");
    }

    private static Sprite MakeSprite(string name, int size, Func<float, float, Color> pixel)
    {
        string path = $"Assets/Art/Shelter/{name}.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) texture.SetPixel(x, y, pixel((x + 0.5f) / size, (y + 0.5f) / size));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void DrawCity(Transform parent)
    {
        Rect("Lower haze", parent, 0f, 0f, 80f, 10f, Hex("293136"), -40);
        var random = new System.Random(42);
        for (int i = 0; i < 20; i++)
        {
            float x = -35f + i * 3.8f;
            float height = 7f + (float)random.NextDouble() * 8f;
            Rect("Distant building", parent, x, height * 0.5f - 1f, 3.2f, height, Hex("293337"), -35);
            for (int row = 0; row < height - 1f; row += 2)
                for (int col = 0; col < 2; col++)
                    Rect("Distant window", parent, x - 0.8f + col * 1.3f, row, 0.35f, 0.6f, Hex("354043"), -34);
        }
        for (int i = 0; i < 85; i++)
        {
            float x = -23f + (float)random.NextDouble() * 46f;
            float y = -2f + (float)random.NextDouble() * 19f;
            var rain = Rect("Rain scratch", parent, x, y, 0.012f, 0.25f + (float)random.NextDouble() * 0.6f,
                new Color(0.5f, 0.58f, 0.6f, 0.12f), -25);
            rain.localRotation = Quaternion.Euler(0f, 0f, -17f);
        }
    }

    private static void DrawRoom(Transform parent, float x, float y, int level, int room, string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        Color wall = level == 1 ? Hex("424848") : Hex("394244");
        if (room == 1) wall *= new Color(0.9f, 0.93f, 0.94f, 1f);
        Rect("Plaster rear wall", root, x, y + 1.85f, 8.55f, 3.65f, wall, -15);
        Rect("Wall skirting", root, x, y + 0.26f, 8.5f, 0.35f, Hex("2B3234"), -12);
        Rect("Ceiling shadow", root, x, y + 3.4f, 8.5f, 0.4f, new Color(0.03f, 0.04f, 0.04f, 0.25f), -10);
        Label("Room label", root, name, x - 3.85f, y + 3.16f, 0.15f, Hex("9DA6A0"));
        DrawWindow(root, x + (room == 0 ? -0.8f : 0.5f), y + 1.95f);
        var random = new System.Random(level * 10 + room + 7);
        for (int i = 0; i < 28; i++)
        {
            float xx = x - 4.1f + (float)random.NextDouble() * 8.2f;
            float yy = y + 0.5f + (float)random.NextDouble() * 2.4f;
            Rect("Plaster wear", root, xx, yy, 0.08f + (float)random.NextDouble() * 0.42f, 0.015f,
                new Color(0.7f, 0.72f, 0.65f, 0.08f), -9);
        }
        if (room < 2)
        {
            Rect("Doorway lintel", root, x + 4.32f, y + 3.0f, 0.25f, 1.35f, Ink, 14);
            Rect("Doorway rear edge", root, x + 4.32f, y + 1.1f, 0.06f, 2.2f, Hex("626765"), -8);
        }
        if ((level == 1 && room == 2) || (level == 2 && room == 1))
        {
            Bed(root, x + 0.4f, y);
            Crate(root, x + 2.65f, y, 0.7f);
        }
        else if (level == 0 && room == 1 || level == 2 && room == 0)
        {
            Crate(root, x - 1.3f, y, 1.15f);
            Crate(root, x + 0.05f, y, 1.25f);
            Crate(root, x - 0.6f, y + 1.25f, 0.8f);
            Crate(root, x + 2.8f, y, 0.6f);
        }
        else if (level == 0 && room == 2)
        {
            Rect("Entry door", root, x + 1.4f, y + 1.1f, 1.2f, 2.2f, Hex("272E30"), -6);
            Rect("Door inset", root, x + 1.4f, y + 1.45f, 0.85f, 1.1f, Hex("343D3F"), -5);
            Rect("Door handle", root, x + 1.82f, y + 1f, 0.14f, 0.06f, Amber, -4);
            Crate(root, x - 0.3f, y, 0.65f);
        }
        else
        {
            Table(root, x, y);
            if (level == 1 && room == 0)
            {
                Rect("Kitchen cabinet", root, x + 1.9f, y + 0.6f, 1.5f, 1.15f, Hex("5E6460"), -5);
                Rect("Kitchen top", root, x + 1.9f, y + 1.2f, 1.65f, 0.1f, Paper, -4);
                Rect("Cupboard seam", root, x + 1.9f, y + 0.6f, 0.04f, 1.05f, Ink, -4);
            }
            if (level == 2 && room == 2)
            {
                Rect("Radio", root, x, y + 1.25f, 0.7f, 0.4f, Steel, -3);
                Rect("Radio speaker", root, x - 0.1f, y + 1.25f, 0.3f, 0.28f, Ink, -2);
                Rect("Antenna", root, x + 0.2f, y + 1.8f, 0.025f, 0.85f, Steel, -3);
            }
        }
        // Small hanging practical lamps keep the cutaway readable without expensive lighting.
        Rect("Lamp cable", root, x - 1.5f, y + 3.1f, 0.025f, 0.7f, Ink, -3);
        Rect("Lamp shade", root, x - 1.5f, y + 2.75f, 0.46f, 0.12f, Ink, -2);
        Rect("Lamp bulb", root, x - 1.5f, y + 2.68f, 0.15f, 0.08f, Amber, -1);
        Rect("Lamp glow", root, x - 1.5f, y + 2.45f, 2.4f, 2.1f, new Color(0.94f, 0.72f, 0.36f, 0.14f), -7, glow);
    }

    private static void DrawWindow(Transform parent, float x, float y)
    {
        Rect("Window recess", parent, x, y, 1.7f, 1.65f, Ink, -12);
        Rect("Window sky", parent, x, y, 1.48f, 1.44f, Hex("687C7E"), -11);
        Rect("Window reflected light", parent, x, y + 0.3f, 1.44f, 0.68f, Hex("80908E"), -10);
        Rect("Window mullion", parent, x, y, 0.08f, 1.55f, Ink, -9);
        Rect("Window crossbar", parent, x, y, 1.55f, 0.07f, Ink, -9);
        Rect("Window sill", parent, x, y - 0.86f, 1.95f, 0.12f, Steel, -8);
    }

    private static void DrawFloor(Transform parent, float y, float? opening)
    {
        if (opening.HasValue)
        {
            float gapLeft = opening.Value - 0.52f;
            float gapRight = opening.Value + 0.52f;
            Rect("Floor left of hatch", parent, (-13f + gapLeft) * 0.5f, y - 0.16f, gapLeft + 13f, 0.32f, Ink, 15);
            Rect("Floor right of hatch", parent, (13f + gapRight) * 0.5f, y - 0.16f, 13f - gapRight, 0.32f, Ink, 15);
            Rect("Hatch left lip", parent, gapLeft - 0.12f, y, 0.24f, 0.07f, Amber, 16);
            Rect("Hatch right lip", parent, gapRight + 0.12f, y, 0.24f, 0.07f, Amber, 16);
        }
        else Rect("Ground slab", parent, 0f, y - 0.16f, 26f, 0.32f, Ink, 15);
        for (float x = -12.8f; x < 13f; x += 0.72f)
        {
            if (opening.HasValue && Mathf.Abs(x - opening.Value) < 0.9f) continue;
            Rect("Floorboard edge", parent, x, y - 0.015f, 0.64f, 0.035f, Hex("7F8276"), 16);
        }
    }

    private static ShelterLadder DrawLadder(Transform parent, float x, ShelterFloor bottom, ShelterFloor top, string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = new Vector3(x, bottom.Height);
        var ladder = root.gameObject.AddComponent<ShelterLadder>();
        ladder.height = top.Height - bottom.Height;
        float height = top.Height - bottom.Height;
        Rect("Left rail", root, x - 0.38f, bottom.Height + height * 0.5f, 0.095f, height + 0.12f, Amber, 6);
        Rect("Right rail", root, x + 0.38f, bottom.Height + height * 0.5f, 0.095f, height + 0.12f, Amber, 6);
        for (float y = bottom.Height + 0.2f; y < top.Height; y += 0.3f)
            Rect("Rung", root, x, y, 0.8f, 0.065f, Hex("A69472"), 7);
        return ladder;
    }

    private static void CreateWalker(ShelterFloor floor, ShelterLadder[] ladders)
    {
        var root = new GameObject("04 - Survivor");
        root.transform.position = new Vector3(-10.5f, floor.Height);
        var walker = root.AddComponent<ShelterWalker>();
        var visual = new GameObject("Editable silhouette").transform;
        visual.SetParent(root.transform, false);
        walker.visual = visual;
        float x = root.transform.position.x;
        Rect("Ground shadow", root.transform, x, 0.035f, 0.86f, 0.13f, new Color(0f, 0f, 0f, 0.45f), 19, circle);
        walker.leftLeg = Limb("Back leg", visual, x - 0.1f, 0.72f, 0.17f, 0.69f, Hex("25292A"), 21);
        walker.rightLeg = Limb("Front leg", visual, x + 0.13f, 0.72f, 0.19f, 0.69f, Hex("343A3B"), 23);
        walker.leftArm = Limb("Back arm", visual, x - 0.21f, 1.34f, 0.17f, 0.66f, Hex("59605B"), 22);
        Rect("Coat silhouette", visual, x, 1.03f, 0.57f, 0.85f, Hex("1E2425"), 24);
        Rect("Coat", visual, x, 1.05f, 0.46f, 0.76f, Hex("929587"), 25);
        Rect("Coat seam", visual, x + 0.13f, 1.05f, 0.035f, 0.69f, Hex("4C534E"), 26);
        Rect("Backpack", visual, x - 0.33f, 1.1f, 0.22f, 0.52f, Hex("525E59"), 27);
        Rect("Neck", visual, x + 0.04f, 1.49f, 0.16f, 0.2f, Hex("AFAD99"), 26);
        Rect("Head", visual, x + 0.05f, 1.7f, 0.37f, 0.43f, Hex("C6C2AA"), 27, circle);
        Rect("Hair", visual, x + 0.025f, 1.85f, 0.38f, 0.18f, Hex("303737"), 28, circle);
        Rect("Nose", visual, x + 0.245f, 1.7f, 0.09f, 0.08f, Hex("C6C2AA"), 27);
        Rect("Scarf", visual, x + 0.02f, 1.47f, 0.36f, 0.12f, Amber, 29);
        walker.rightArm = Limb("Front arm", visual, x + 0.2f, 1.34f, 0.18f, 0.65f, Hex("7F887B"), 28);
    }

    private static Transform Limb(string name, Transform parent, float x, float y, float width, float height, Color color, int order)
    {
        var joint = new GameObject(name + " joint").transform;
        joint.SetParent(parent, false);
        joint.position = new Vector3(x, y);
        Rect(name, joint, x, y - height * 0.5f, width, height, color, order);
        return joint;
    }

    private static void Table(Transform parent, float x, float y)
    {
        Rect("Table top", parent, x, y + 1f, 2.6f, 0.16f, Hex("7E8071"), -4);
        Rect("Table left leg", parent, x - 1f, y + 0.48f, 0.15f, 0.95f, Ink, -5);
        Rect("Table right leg", parent, x + 1f, y + 0.48f, 0.15f, 0.95f, Ink, -5);
        Rect("Table bottle", parent, x - 0.7f, y + 1.29f, 0.16f, 0.42f, Hex("45574F"), -3);
        Rect("Tin cup", parent, x + 0.65f, y + 1.18f, 0.23f, 0.2f, Steel, -3);
    }

    private static void Bed(Transform parent, float x, float y)
    {
        Rect("Bed frame", parent, x, y + 0.39f, 3.3f, 0.15f, Ink, -5);
        Rect("Bed foot", parent, x - 1.5f, y + 0.25f, 0.12f, 0.5f, Steel, -4);
        Rect("Bed head", parent, x + 1.5f, y + 0.5f, 0.12f, 1f, Steel, -4);
        Rect("Mattress", parent, x, y + 0.58f, 3.1f, 0.25f, Hex("80877C"), -4);
        Rect("Blanket", parent, x - 0.4f, y + 0.71f, 2.1f, 0.16f, Hex("555F5B"), -3);
        Rect("Pillow", parent, x + 1.03f, y + 0.77f, 0.65f, 0.15f, Paper, -3);
    }

    private static void Crate(Transform parent, float x, float y, float size)
    {
        Rect("Wooden crate", parent, x, y + size * 0.5f, size, size, Hex("66675B"), -5);
        Rect("Crate panel", parent, x, y + size * 0.5f, size - 0.12f, size - 0.12f, Hex("454E49"), -4);
        for (int i = 1; i < 4; i++)
            Rect("Crate slat", parent, x, y + size * i / 4f, size - 0.1f, 0.035f, Hex("74766A"), -3);
    }

    private static Transform Rect(string name, Transform parent, float x, float y, float width, float height,
        Color color, int order, Sprite sprite = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : square;
        renderer.sharedMaterial = spriteMaterial;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go.transform;
    }

    private static void Label(string name, Transform parent, string text, float x, float y, float size, Color color, int order = 30)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.font = font;
        label.fontSize = 64;
        label.characterSize = size * 0.28f;
        label.anchor = TextAnchor.MiddleLeft;
        label.color = color;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = order;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out var color);
        return color;
    }
}
