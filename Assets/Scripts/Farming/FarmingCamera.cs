using UnityEngine;
using UnityGameJam.Common;
namespace ShelterRun
{
    [ExecuteAlways, RequireComponent(typeof(Camera), typeof(FixedAspectCamera))]
    public sealed class FarmingCamera : MonoBehaviour
    {
        [SerializeField] float referenceSize = 4.5f;
        [SerializeField] Transform target;
        Camera cameraComponent;
        void OnEnable()
        {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.transparencySortMode = TransparencySortMode.Orthographic;
        }
        void LateUpdate() { Snap(); }
        public void Snap()
        {
            if (!cameraComponent) cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographicSize = referenceSize;
            if (!target) return;
            var area = FarmingMap2D.Instance ? FarmingMap2D.Instance.ActiveArea : null;
            float halfWidth = referenceSize * cameraComponent.aspect;
            float x = target.position.x;
            if (area) x = area.Right - area.Left <= halfWidth * 2 ? (area.Left + area.Right) * .5f : Mathf.Clamp(x, area.Left + halfWidth, area.Right - halfWidth);
            transform.position = new Vector3(x, area ? area.CameraY : target.position.y + 2, -20);
        }
    }
}
