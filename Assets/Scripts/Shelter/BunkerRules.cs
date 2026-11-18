using System;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class BunkerRules : ScriptableObject
    {
        public const string Address = "data/bunker-rules";
        [SerializeField, Min(1)] int totalTicks;
        [SerializeField, Min(0.001f)] float secondsPerTick;
        [SerializeField, Min(1)] int randomEventEveryTicks;
        [SerializeField, Min(1)] int invasionEveryTicks;
        [SerializeField, Min(1)] int workEveryTicks;
        [SerializeField, Min(0)] float percentPerAbility;
        [SerializeField, Range(0,100)] float planningUnlockPercent;
        [SerializeField, Min(1)] int mentalMaximum;
        [SerializeField, Min(1)] int faintDurationTicks;
        [SerializeField, Min(1)] int wakeMental;
        [SerializeField, Min(0.001f)] double evaluationTargetProgress;
        [SerializeField, Min(1)] int evaluationReferenceTicks;
        public double EvaluationTargetProgress => evaluationTargetProgress;
        public int EvaluationReferenceTicks => evaluationReferenceTicks;
        public int MentalMaximum => mentalMaximum;
        public int FaintDurationTicks => faintDurationTicks;
        public int WakeMental => wakeMental;
        public int TotalTicks => totalTicks;
        public float SecondsPerTick => secondsPerTick;
        public int RandomEventEveryTicks => randomEventEveryTicks;
        public int InvasionEveryTicks => invasionEveryTicks;
        public int WorkEveryTicks => workEveryTicks;
        public float PercentPerAbility => percentPerAbility;
        public float PlanningUnlockPercent => planningUnlockPercent;
        public void Validate()
        {
            if (evaluationTargetProgress <= 0 || double.IsNaN(evaluationTargetProgress) || double.IsInfinity(evaluationTargetProgress) || evaluationReferenceTicks <= 0 ||
                mentalMaximum <= 0 || faintDurationTicks <= 0 || wakeMental <= 0 || wakeMental > mentalMaximum ||
                totalTicks <= 0 || randomEventEveryTicks <= 0 || invasionEveryTicks <= 0 || workEveryTicks <= 0 ||
                secondsPerTick <= 0 || float.IsNaN(secondsPerTick) || float.IsInfinity(secondsPerTick) ||
                percentPerAbility < 0 || float.IsNaN(percentPerAbility) || float.IsInfinity(percentPerAbility) ||
                planningUnlockPercent < 0 || planningUnlockPercent >= 100 || float.IsNaN(planningUnlockPercent) || float.IsInfinity(planningUnlockPercent))
                throw new InvalidOperationException("Invalid data.bunker.rules: durations/intervals must be positive and coefficient finite/nonnegative.");
        }
    }
}
