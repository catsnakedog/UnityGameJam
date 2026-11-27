using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGameJam.Common
{
    /// <summary>Fits the same 1920 x 1080 composition into any window without cropping.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-1000)]
    public sealed class FixedAspectCamera : MonoBehaviour
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        public const float Aspect = (float)ReferenceWidth / ReferenceHeight;

        Camera view;
        Rect previousRect;

        public static Rect CalculateViewport(int width, int height)
        {
            if (width <= 0 || height <= 0) return new Rect(0, 0, 1, 1);
            float screenAspect = (float)width / height;
            if (screenAspect > Aspect)
            {
                float widthFraction = Aspect / screenAspect;
                return new Rect((1 - widthFraction) * 0.5f, 0, widthFraction, 1);
            }
            float heightFraction = screenAspect / Aspect;
            return new Rect(0, (1 - heightFraction) * 0.5f, 1, heightFraction);
        }

        void OnEnable()
        {
            view = GetComponent<Camera>();
            previousRect = view.rect;
            RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
            ApplyViewport();
        }

        // Update before gameplay reads pixelRect for pointer input on a resize frame.
        void Update() => ApplyViewport();
        void OnPreCull() => ApplyViewport();

        void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == view) ApplyViewport();
        }

        public void ApplyViewport()
        {
            if (!view) return;
            // Render textures are owned by their caller, not by the game window.
            if (view.targetTexture) return;
            view.rect = CalculateViewport(Screen.width, Screen.height);
            view.aspect = Aspect;
        }

        void OnGUI()
        {
            if (!view || !view.enabled || view.targetTexture || Event.current.type != EventType.Repaint) return;
            Rect rect = CalculateViewport(Screen.width, Screen.height);
            float side = rect.x * Screen.width;
            float top = rect.y * Screen.height;
            // IMGUI runs after camera rendering and also covers any stale backbuffer pixels.
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            int oldDepth = GUI.depth;
            GUI.color = Color.black;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = int.MinValue;
            if (side > 0)
            {
                GUI.DrawTexture(new Rect(0, 0, side, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - side, 0, side, Screen.height), Texture2D.whiteTexture);
            }
            if (top > 0)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, top), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, Screen.height - top, Screen.width, top), Texture2D.whiteTexture);
            }
            GUI.depth = oldDepth;
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
            if (!view) return;
            view.rect = previousRect;
            view.ResetAspect();
        }
    }
}
