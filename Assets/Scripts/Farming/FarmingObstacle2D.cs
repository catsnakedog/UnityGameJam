using UnityEngine;
namespace ShelterRun
{
    public sealed class FarmingObstacle2D : MonoBehaviour
    {
        [SerializeField] bool blocksInteraction = true;
        public bool BlocksInteraction => blocksInteraction;
    }
}
