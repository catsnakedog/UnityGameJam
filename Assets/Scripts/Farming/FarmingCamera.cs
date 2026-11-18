using UnityEngine;
using UnityGameJam.Common;

namespace ShelterRun
{
    [ExecuteAlways, RequireComponent(typeof(Camera), typeof(FixedAspectCamera))]
    public sealed class FarmingCamera : MonoBehaviour
    {
        [SerializeField] float referenceSize = 12.5f;
        [SerializeField] Transform target;
        Camera cameraComponent;
        void OnEnable()
        {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.transparencySortMode = UnityEngine.TransparencySortMode.CustomAxis;
            cameraComponent.transparencySortAxis = FarmingDepth2D.SortAxis;
        }
        void LateUpdate()
        {
            if (!Mathf.Approximately(cameraComponent.orthographicSize, referenceSize)) cameraComponent.orthographicSize = referenceSize;
            if (target)
            {
                var position = new Vector3(target.position.x, target.position.y + 1, -20);
                if (transform.position != position) transform.position = position;
            }
        }
    }
}
