using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityGameJam.Shelter;

public static class SurvivorAnimationSetup
{
    const string ArtRoot = "Assets/Art/Shelter/Characters";
    const string PrefabPath = "Assets/Prefabs/Shelter/Survivor.prefab";
    internal static Material RimMaterial => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Shelter/CharacterRim.mat");

    // The filenames correspond to SurvivorAnimation.Pose in serialized order.
    static readonly string[] Files = { "idle_1", "idle_2", "walk_1", "walk_2", "workin", "working", "sleep", "dead_2", "attack", "hit", "dead_1" };

    [MenuItem("Tools/Shelter/Refresh Survivor Animations")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before refreshing survivor art.");
        var folders = AssetDatabase.GetSubFolders(ArtRoot).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (folders.Length == 0) throw new InvalidOperationException("Missing survivor artwork folders in " + ArtRoot);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animation = root.GetComponent<SurvivorAnimation>() ?? root.AddComponent<SurvivorAnimation>();
            var serialized = new SerializedObject(animation);
            var image = (SpriteRenderer)serialized.FindProperty("image").objectReferenceValue;
            if (!image)
            {
                image = new GameObject("Character sprite").AddComponent<SpriteRenderer>();
                image.transform.SetParent(root.transform, false);
                image.sortingOrder = 28;
                serialized.FindProperty("image").objectReferenceValue = image;
            }
            image.sharedMaterial = RimMaterial;
            // SampleAnimation needs an Animator on the object that owns the sprite binding.
            if (!image.TryGetComponent<Animator>(out _)) image.gameObject.AddComponent<Animator>();
            var characters = serialized.FindProperty("characters");
            characters.arraySize = folders.Length;
            for (int i = 0; i < folders.Length; i++)
            {
                var folder = folders[i];
                var match = Regex.Match(Path.GetFileName(folder), @"^no([0-9]{2})_tags$");
                if (!match.Success) throw new InvalidDataException("Expected noXX_tags: " + folder);
                var number = match.Groups[1].Value;
                var entry = characters.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("characterId").stringValue = "person_" + number;
                var motions = entry.FindPropertyRelative("motions");
                motions.arraySize = Files.Length;
                var standing = ReadMotion(folder, "no" + number).firstFrame;
                for (int j = 0; j < Files.Length; j++)
                {
                    var motion = ReadMotion(folder, Files[j]);
                    if (!motion.firstFrame && j == (int)SurvivorAnimation.Pose.Sit) motion = ReadMotion(folder, "sit");
                    if (!motion.firstFrame && j == (int)SurvivorAnimation.Pose.Type) motion = ReadMotion(folder, "typing");
                    var slot = motions.GetArrayElementAtIndex(j);
                    slot.FindPropertyRelative("clip").objectReferenceValue = motion.clip;
                    slot.FindPropertyRelative("firstFrame").objectReferenceValue = motion.firstFrame;
                    if (!standing && j == (int)SurvivorAnimation.Pose.Idle) standing = motion.firstFrame;
                }
                if (!standing) throw new InvalidDataException("Supply idle_1 or no" + number + " in " + folder);
                entry.FindPropertyRelative("standing").objectReferenceValue = standing;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    internal static SurvivorAnimation.Motion ReadMotion(string folder, string filename)
    {
        string path = folder + "/" + filename + ".aseprite";
        var motion = new SurvivorAnimation.Motion();
        if (!File.Exists(path)) return motion;
        var importer = AssetImporter.GetAtPath(path) as AsepriteImporter;
        if (!importer) throw new InvalidDataException("Missing Aseprite importer for " + path);
        var feet = new Vector2(0.5f, 0.25f); // Source canvas is 128 px tall; feet end at row 96.
        if (importer.layerImportMode != LayerImportModes.MergeFrame || importer.pivotAlignment != SpriteAlignment.Custom ||
            importer.customPivotPosition != feet || importer.pivotSpace != PivotSpaces.Canvas ||
            importer.spritePixelsPerUnit != 22 || importer.filterMode != FilterMode.Point ||
            importer.importMode != FileImportModes.AnimatedSprite || importer.generateModelPrefab || !importer.generateAnimationClips)
        {
            importer.importMode = FileImportModes.AnimatedSprite;
            importer.layerImportMode = LayerImportModes.MergeFrame;
            importer.pivotAlignment = SpriteAlignment.Custom;
            importer.customPivotPosition = feet;
            importer.pivotSpace = PivotSpaces.Canvas;
            importer.spritePixelsPerUnit = 22;
            importer.filterMode = FilterMode.Point;
            importer.generateModelPrefab = false;
            importer.generateAnimationClips = true;
            importer.SaveAndReimport();
        }
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var clips = assets.OfType<AnimationClip>().ToArray();
        motion.clip = clips.FirstOrDefault(c => c.name == filename) ?? clips.SingleOrDefault();
        if (motion.clip)
        {
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(motion.clip)
                .Single(b => b.path == "" && b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");
            motion.firstFrame = (Sprite)AnimationUtility.GetObjectReferenceCurve(motion.clip, binding)[0].value;
        }
        else motion.firstFrame = assets.OfType<Sprite>().SingleOrDefault();
        if (!motion.firstFrame) throw new InvalidDataException("No drawable first frame in " + path);
        return motion;
    }
}
