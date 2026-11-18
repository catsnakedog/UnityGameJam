using System;
using UnityEngine;

namespace ShelterRun
{
    [CreateAssetMenu(menuName = "Shelter Run/Farming Rules")]
    public sealed class FarmingRules : ScriptableObject
    {
        [SerializeField] float duration;
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
        public float Duration => duration;
        public int Capacity => capacity;
        public float MoveSpeed => moveSpeed;
        public float InteractionRadius => interactionRadius;
        public float DropDistance => dropDistance;
        public float ThrowDuration => throwDuration;
        public float FadeDuration => fadeDuration;
        public float ThrowArc => throwArc;
        public float WarningSeconds => warningSeconds;
        public Vector2 MapHalfExtents => mapHalfExtents;
        public float RoomRevealDuration => roomRevealDuration;

        public void Validate()
        {
            foreach (float value in new[] { duration, moveSpeed, interactionRadius, dropDistance, throwDuration, fadeDuration, throwArc, warningSeconds, mapHalfExtents.x, mapHalfExtents.y, roomRevealDuration })
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("FarmingRules requires positive finite values.");
            if (capacity < 1 || warningSeconds > duration)
                throw new InvalidOperationException("Invalid FarmingRules capacity/warningSeconds.");
        }
    }
}
