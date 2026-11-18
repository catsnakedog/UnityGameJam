using UnityEngine;
using UnityEngine.Rendering;
namespace ShelterRun
{
    [ExecuteAlways, RequireComponent(typeof(SortingGroup))]
    public sealed class FarmingDepth2D : MonoBehaviour
    {
        public const int WorldOrder = 100;
        [SerializeField] int worldOrder = WorldOrder;
        SortingGroup group;
        void LateUpdate()
        {
            if (!group) group = GetComponent<SortingGroup>();
            group.sortingOrder = worldOrder;
            var p = transform.position;
            if (p.z != 0) { p.z = 0; transform.position = p; }
        }
    }
}
