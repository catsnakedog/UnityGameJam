using UnityEngine;
using UnityGameJam.Common;

namespace UnityGameJam.Shelter
{
    [RequireComponent(typeof(Camera), typeof(FixedAspectCamera))]
    public sealed class ShelterCamera : MonoBehaviour
    {
        public float minimumHalfHeight = 8.3f;
        public float minimumHalfWidth = 16.2f;
        private Camera view;

        private void Awake() => view = GetComponent<Camera>();

        private void LateUpdate()
        {
            // Preserve the same room and ladder framing inside the fixed 16:9 viewport.
            view.orthographicSize = Mathf.Max(minimumHalfHeight,
                minimumHalfWidth / FixedAspectCamera.Aspect);
        }
    }
}
