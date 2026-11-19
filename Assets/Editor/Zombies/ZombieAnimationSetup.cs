using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ZombieAnimationSetup
{
    [MenuItem("Tools/Shelter/Refresh Zombie Animations")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var root = PrefabUtility.LoadPrefabContents(ZombieSetup.PrefabPath);
        try
        {
            var animation = root.GetComponent<ZombieAnimation>() ?? root.AddComponent<ZombieAnimation>();
            var serialized = new SerializedObject(animation);
            var imageSlot = serialized.FindProperty("image");
            var image = (SpriteRenderer)imageSlot.objectReferenceValue;
            var rig = root.GetComponent<ShelterWalker>().visual;
            if (!image)
            {
                image = new GameObject("Zombie sprite").AddComponent<SpriteRenderer>();
                image.transform.SetParent(root.transform, false);
                image.sortingOrder = 28;
                image.gameObject.AddComponent<Animator>();
                imageSlot.objectReferenceValue = image;
            }
            image.sharedMaterial = SurvivorAnimationSetup.RimMaterial;
            rig.gameObject.SetActive(false);
            var variants = serialized.FindProperty("variants");
            variants.arraySize = 4;
            for (int i = 0; i < variants.arraySize; i++)
            {
                var entry = variants.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("zombieId").stringValue = "zombie_" + (i + 1);
                var folder = $"Assets/Art/Shelter/Zombies/no{i + 7:00}_tags";
                foreach (var pose in new[] { "idle", "walk", "hit" })
                {
                    var motion = SurvivorAnimationSetup.ReadMotion(folder, pose);
                    if (!motion.clip) throw new InvalidDataException($"Missing {pose} animation in {folder}");
                    var slot = entry.FindPropertyRelative(pose);
                    slot.FindPropertyRelative("clip").objectReferenceValue = motion.clip;
                    slot.FindPropertyRelative("firstFrame").objectReferenceValue = motion.firstFrame;
                    if (i == 0 && pose == "idle") image.sprite = motion.firstFrame;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ZombieSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
