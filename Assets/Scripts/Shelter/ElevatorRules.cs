using System;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class ElevatorRules : ScriptableObject
    {
        public const string Address = "data/elevators";
        [SerializeField] float travelSpeed;
        [SerializeField] float queueSpacing;
        [SerializeField] float boardingOffset;
        [SerializeField] float exitOffset;
        public float TravelSpeed => travelSpeed;
        public float QueueSpacing => queueSpacing;
        public float BoardingOffset => boardingOffset;
        public float ExitOffset => exitOffset;
        public void Validate()
        {
            if (!Positive(travelSpeed) || !Positive(queueSpacing) || !Positive(boardingOffset) || !Positive(exitOffset) || exitOffset >= boardingOffset)
                throw new InvalidOperationException("Invalid data.shelter.elevators: positive finite values and exitOffset < boardingOffset required.");
        }
        static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
