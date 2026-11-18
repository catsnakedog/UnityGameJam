using UnityEngine;
using UnityEngine.Rendering;
namespace ShelterRun
{
    [ExecuteAlways, RequireComponent(typeof(SortingGroup))]
    public sealed class FarmingDepth2D : MonoBehaviour
    {
        public static readonly Vector3 SortAxis = new Vector3(0, 1, -1);
        public const int WorldOrder = 0;
        SortingGroup group;
        public float GroundOffset { get; set; }
        public float GroundDepth => transform.position.y - transform.position.z;
        void LateUpdate()
        {
            if (!group) group = GetComponent<SortingGroup>();
            // All world groups and individual wall tiles share one order. The camera
            // compares Y-Z at their ground pivots; visual height cannot reverse depth.
            if (group.sortingOrder != WorldOrder) group.sortingOrder = WorldOrder;
            var position = transform.position;
            if (!Mathf.Approximately(position.z, GroundOffset))
            { position.z = GroundOffset; transform.position = position; }
        }
    }
}
