using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.Common;

public static class FixedAspectSetup
{
    public const string ViewportName = "Viewport 1920x1080";

    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.isRootCanvas) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // The title menu is authored in 1280 x 720 canvas units. It shares the same 16:9 viewport.
        if (!canvas.GetComponent<TitleMenu>())
            scaler.referenceResolution = new Vector2(FixedAspectCamera.ReferenceWidth, FixedAspectCamera.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var viewport = canvas.transform.Find(ViewportName) as RectTransform;
        if (!viewport)
        {
            var children = canvas.transform.Cast<Transform>().ToArray();
            viewport = new GameObject(ViewportName, typeof(RectTransform), typeof(AspectRatioFitter), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(canvas.transform, false);
            foreach (var child in children) child.SetParent(viewport, false);
        }
        var fitter = viewport.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = FixedAspectCamera.Aspect;
        EditorUtility.SetDirty(scaler);
        EditorUtility.SetDirty(fitter);
    }

    [MenuItem("Tools/Display/Apply 1920x1080 Letterbox")]
    public static void ApplyToProject()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode before applying display settings.");
        PlayerSettings.defaultWebScreenWidth = FixedAspectCamera.ReferenceWidth;
        PlayerSettings.defaultWebScreenHeight = FixedAspectCamera.ReferenceHeight;

        const string hudPath = "Assets/Prefabs/Farming/FarmingHud.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(hudPath))
        {
            var root = PrefabUtility.LoadPrefabContents(hudPath);
            try
            {
                ConfigureCanvas(root.GetComponent<Canvas>());
                PrefabUtility.SaveAsPrefabAsset(root, hudPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        var originalScene = SceneManager.GetActiveScene();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = SceneManager.GetSceneByPath(path);
            bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
            if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        if (camera.targetTexture || !camera.CompareTag("MainCamera")) continue;
                        if (!camera.GetComponent<FixedAspectCamera>()) camera.gameObject.AddComponent<FixedAspectCamera>();
                        camera.GetComponent<FixedAspectCamera>().ApplyViewport();
                    }
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) ConfigureCanvas(canvas);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
        if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
        AssetDatabase.SaveAssets();
    }
}
