using System.Collections.Generic;
using UnityEngine;

namespace ShelterRun
{
    public sealed class FarmingSpawnTable : MonoBehaviour
    {
        [SerializeField] Transform[] tabletopSlots;
        public IReadOnlyList<Transform> TabletopSlots => tabletopSlots;
    }
}
