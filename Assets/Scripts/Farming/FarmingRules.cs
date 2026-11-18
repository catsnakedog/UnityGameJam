using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShelterRun
{
    [CreateAssetMenu(menuName = "Shelter Run/Farming Rules")]
    public sealed class FarmingRules : ScriptableObject
    {
        [Serializable]
        public struct RoomSpawnRule
        {
            [SerializeField, Min(0)] int people;
            [SerializeField, Min(1)] int legendWeight;
            public int People => people;
            public int LegendWeight => legendWeight;
        }

        [SerializeField] float duration;
        [SerializeField] float preparationDuration;
        [SerializeField] int capacity;
        [SerializeField] float moveSpeed;
        [SerializeField] float interactionRadius;
        [SerializeField] float dropDistance;
        [SerializeField] float throwDuration;
        [SerializeField] float fadeDuration;
        [SerializeField] float throwArc;
        [SerializeField] float warningSeconds;
        [SerializeField] Vector2 mapHalfExtents;
        [SerializeField] float roomRevealDuration;
        [Tooltip("Playable survivor who starts secured for the bunker.")]
        [SerializeField] string startingPersonId;
        [Tooltip("Nearest corridor door to the bunker first.")]
        [SerializeField] RoomSpawnRule[] roomSpawns;
        public float Duration => duration;
        public float PreparationDuration => preparationDuration;
        public int Capacity => capacity;
        public string StartingPersonId => startingPersonId;
        public float MoveSpeed => moveSpeed;
        public float InteractionRadius => interactionRadius;
        public float DropDistance => dropDistance;
        public float ThrowDuration => throwDuration;
        public float FadeDuration => fadeDuration;
        public float ThrowArc => throwArc;
        public float WarningSeconds => warningSeconds;
        public Vector2 MapHalfExtents => mapHalfExtents;
        public float RoomRevealDuration => roomRevealDuration;
        public IReadOnlyList<RoomSpawnRule> RoomSpawns => roomSpawns;

        public void Validate()
        {
            foreach (float value in new[] { duration, preparationDuration, moveSpeed, interactionRadius, dropDistance, throwDuration, fadeDuration, throwArc, warningSeconds, mapHalfExtents.x, mapHalfExtents.y, roomRevealDuration })
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("FarmingRules requires positive finite values.");
            if (capacity < 1 || warningSeconds > duration)
                throw new InvalidOperationException("Invalid FarmingRules capacity/warningSeconds.");
            if (string.IsNullOrWhiteSpace(startingPersonId))
                throw new InvalidOperationException("Missing FarmingRules starting person ID.");
            if (roomSpawns == null || roomSpawns.Length == 0)
                throw new InvalidOperationException("Missing FarmingRules room spawn settings.");
            int previousWeight = 0;
            long totalWeight = 0;
            foreach (var room in roomSpawns)
            {
                totalWeight += room.LegendWeight;
                if (room.People < 0 || room.LegendWeight <= previousWeight || totalWeight > int.MaxValue)
                    throw new InvalidOperationException("Invalid person quota or legend weight in FarmingRules.");
                previousWeight = room.LegendWeight;
            }
        }
    }
}
