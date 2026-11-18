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
        [SerializeField, Min(1)] int invasionApproachTicks;
        [SerializeField, Min(1), Tooltip("접근 중인 좀비에게 보초가 원거리 사격하는 벙커 틱 간격")] int guardVolleyEveryTicks;
        [SerializeField, Min(0)] int invasionMentalLoss;
        [SerializeField, Min(1)] int guardTicksPerMental;
        [SerializeField, Min(1)] int workEveryTicks;
        [SerializeField, Min(0), Tooltip("작업 1회에 해당 방 능력 1점당 증가하는 개발 진행도 (%p)")] float percentPerAbility;
        [SerializeField, Min(0), Tooltip("작업자 한 명이 개발 작업 1회에 소모하는 멘탈 포인트")] int mentalCostPerWork;
        [SerializeField, Range(0,100)] float planningUnlockPercent;
        [SerializeField, Min(1)] int mentalMaximum;
        [SerializeField, Min(0), Tooltip("피곤한 대기·걷기를 표시하는 멘탈 상한 (포함)")] int tiredMentalThreshold;
        [SerializeField, Min(1)] int faintDurationTicks;
        [SerializeField, Min(1)] int wakeMental;
        public const int HungerCapacity = 10;
        [Header("허기와 통조림")]
        [SerializeField, Min(1), Tooltip("허기가 줄어드는 벙커 틱 간격")] int hungerEveryTicks;
        [SerializeField, Range(0, HungerCapacity), Tooltip("감소 시 비우는 칸 수")] int hungerLoss;
        [SerializeField, Range(1, HungerCapacity), Tooltip("통조림 한 입으로 채우는 칸 수")] int foodRestore;
        [SerializeField, Min(1), Tooltip("새 통조림 한 개의 사용 횟수")] int servingsPerCan;
        [SerializeField] string[] usableMentalSupplyIds;
        public int HungerEveryTicks => hungerEveryTicks;
        public int HungerLoss => hungerLoss;
        public int FoodRestore => foodRestore;
        public int ServingsPerCan => servingsPerCan;
        public bool IsMentalConsumable(string id) => usableMentalSupplyIds != null && Array.IndexOf(usableMentalSupplyIds, id) >= 0;
        [SerializeField, Min(0.001f)] double evaluationTargetProgress;
        [SerializeField, Min(1)] int evaluationReferenceTicks;
        public double EvaluationTargetProgress => evaluationTargetProgress;
        public int EvaluationReferenceTicks => evaluationReferenceTicks;
        public int MentalMaximum => mentalMaximum;
        public int TiredMentalThreshold => tiredMentalThreshold;
        public int FaintDurationTicks => faintDurationTicks;
        public int WakeMental => wakeMental;
        public int TotalTicks => totalTicks;
        public float SecondsPerTick => secondsPerTick;
        public int RandomEventEveryTicks => randomEventEveryTicks;
        public int InvasionEveryTicks => invasionEveryTicks;
        public int InvasionApproachTicks => invasionApproachTicks;
        public int GuardVolleyEveryTicks => guardVolleyEveryTicks;
        public int InvasionMentalLoss => invasionMentalLoss;
        public int GuardTicksPerMental => guardTicksPerMental;
        public int WorkEveryTicks => workEveryTicks;
        public float PercentPerAbility => percentPerAbility;
        public int MentalCostPerWork => mentalCostPerWork;
        public float PlanningUnlockPercent => planningUnlockPercent;
        public void Validate()
        {
            if (tiredMentalThreshold < 0 || tiredMentalThreshold > mentalMaximum)
                throw new InvalidOperationException("Invalid data.bunker.rules: tiredMentalThreshold outside mental range.");
            if (invasionApproachTicks < 1 || guardVolleyEveryTicks < 1 || invasionMentalLoss < 0 || guardTicksPerMental < 1)
                throw new InvalidOperationException("Invalid bunker invasion timing or mental loss.");
            if (mentalCostPerWork < 0)
                throw new InvalidOperationException("Invalid data.bunker.rules: mentalCostPerWork must be nonnegative.");
            if (hungerEveryTicks < 1 || hungerLoss < 0 || hungerLoss > HungerCapacity ||
                foodRestore < 1 || foodRestore > HungerCapacity || servingsPerCan < 1)
                throw new InvalidOperationException("Invalid bunker hunger or food settings.");
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
