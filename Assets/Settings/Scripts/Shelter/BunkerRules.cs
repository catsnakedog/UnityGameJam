using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public enum BunkerDifficulty { Hard, Medium, Easy }

    public sealed class BunkerRules : ScriptableObject
    {
        public const string Address = "data/bunker-rules";
        [SerializeField, Min(1)] int totalTicks;
        [SerializeField, Min(0.001f)] float secondsPerTick;
        [SerializeField, Min(1)] int randomEventEveryTicks;
        [SerializeField, Min(1)] int invasionEveryTicks;
        [SerializeField, Min(1)] int firstInvasionTick;
        [Serializable]
        struct DifficultyPacing
        {
            [Min(0.001f)] public float secondsPerTick;
            [Min(1)] public int eventDivisions, invasionDivisions, firstInvasionDivisor;

            public void Validate()
            {
                if (!float.IsFinite(secondsPerTick) || secondsPerTick <= 0 ||
                    eventDivisions < 1 || invasionDivisions < 1 || firstInvasionDivisor < 1)
                    throw new InvalidOperationException("Invalid bunker difficulty pacing.");
            }
        }
        [SerializeField] DifficultyPacing easy, medium;

        // The caller owns the scene-local copy. The addressable settings remain shared.
        public BunkerRules CreateDifficultyOverride(BunkerDifficulty difficulty)
        {
            var pacing = difficulty switch
            {
                BunkerDifficulty.Easy => easy,
                BunkerDifficulty.Medium => medium,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };
            pacing.Validate();
            int Interval(int divisions) => Mathf.Max(1, Mathf.RoundToInt((float)totalTicks / divisions));
            var copy = Instantiate(this);
            copy.secondsPerTick = pacing.secondsPerTick;
            copy.randomEventEveryTicks = Interval(pacing.eventDivisions);
            copy.invasionEveryTicks = Interval(pacing.invasionDivisions);
            copy.firstInvasionTick = Interval(pacing.firstInvasionDivisor);
            return copy;
        }
        [SerializeField] int[] presentationTicks;
        [SerializeField] PresentationGrade[] presentationGrades;
        [Serializable]
        public struct PresentationGrade
        {
            [SerializeField, Min(0)] double minimumScore;
            [SerializeField] string label;
            [SerializeField] int mentalDelta;
            [SerializeField] Color color;
            public double MinimumScore => minimumScore;
            public string Label => label;
            public int MentalDelta => mentalDelta;
            public Color Color => color;
        }
        public IReadOnlyList<int> PresentationTicks => presentationTicks;
        public int FirstInvasionTick => firstInvasionTick;
        public bool IsPresentationTick(int tick) => tick < TotalTicks && Array.BinarySearch(presentationTicks, tick) >= 0;
        public bool IsInvasionTick(int tick) => tick >= firstInvasionTick && tick < TotalTicks && (tick - firstInvasionTick) % invasionEveryTicks == 0;
        public PresentationGrade GradePresentation(double score)
        {
            for (int i = presentationGrades.Length - 1; i >= 0; i--)
                if (score >= presentationGrades[i].MinimumScore) return presentationGrades[i];
            throw new ArgumentOutOfRangeException(nameof(score));
        }
        [SerializeField, Min(1)] int invasionApproachTicks;
        [SerializeField, Min(1), Tooltip("문 앞 보초가 고정 피해를 주는 벙커 틱 간격")] int guardVolleyEveryTicks;
        [SerializeField, Min(1)] int doorHitLimit;
        [SerializeField, Min(0)] int invasionMentalLoss;
        [SerializeField, Min(0)] int guardMentalLossPerTick;
        [SerializeField, Min(1)] int workEveryTicks;
        [SerializeField, Min(0), Tooltip("작업 1회에 해당 방 능력 1점당 증가하는 개발 진행도 (%p)")] float percentPerAbility;
        [SerializeField, Min(0), Tooltip("작업자 한 명이 개발 작업 1회에 소모하는 멘탈 포인트")] int mentalCostPerWork;
        [SerializeField, Range(0,100)] float planningUnlockPercent;
        [SerializeField, Min(1)] int mentalMaximum;
        [SerializeField, Min(0), Tooltip("피곤한 대기·걷기를 표시하는 멘탈 상한 (포함)")] int tiredMentalThreshold;
        [SerializeField, Min(1)] int sleepMentalPerTick;
        [SerializeField, Min(1), Tooltip("수면실 한 곳에서 동시에 텐트를 예약하거나 사용할 수 있는 인원")] int sleepingRoomCapacity;
        [SerializeField, Min(1)] int wakeMental;
        public const int HungerCapacity = 10;
        [Header("허기와 통조림")]
        [SerializeField, Min(1), Tooltip("허기가 줄어드는 벙커 틱 간격")] int hungerEveryTicks;
        [SerializeField, Range(0, HungerCapacity), Tooltip("감소 시 비우는 칸 수")] int hungerLoss;
        [SerializeField, Min(1), Tooltip("허기 0칸일 때 멘탈 감소량에 적용하는 배율")] int emptyHungerMentalLossMultiplier = 2;
        [SerializeField, Range(1, HungerCapacity), Tooltip("통조림 한 입으로 채우는 칸 수")] int foodRestore;
        [SerializeField, Min(1), Tooltip("새 통조림 한 개의 사용 횟수")] int servingsPerCan;
        public int HungerEveryTicks => hungerEveryTicks;
        public int HungerLoss => hungerLoss;
        public int EmptyHungerMentalLossMultiplier => emptyHungerMentalLossMultiplier;
        public int FoodRestore => foodRestore;
        public int ServingsPerCan => servingsPerCan;
        [SerializeField, Min(0.001f)] double evaluationTargetProgress;
        [SerializeField, Min(1)] int evaluationReferenceTicks;
        public double EvaluationTargetProgress => evaluationTargetProgress;
        public int EvaluationReferenceTicks => evaluationReferenceTicks;
        public int MentalMaximum => mentalMaximum;
        public int TiredMentalThreshold => tiredMentalThreshold;
        public int SleepMentalPerTick => sleepMentalPerTick;
        [SerializeField, Min(1)] int workRoomCapacity = 3;
        public int WorkRoomCapacity => workRoomCapacity;
        public int SleepingRoomCapacity => sleepingRoomCapacity;
        public int WakeMental => wakeMental;
        public int TotalTicks => totalTicks;
        public float SecondsPerTick => secondsPerTick;
        public int RandomEventEveryTicks => randomEventEveryTicks;
        public int InvasionEveryTicks => invasionEveryTicks;
        public int InvasionApproachTicks => invasionApproachTicks;
        public int GuardVolleyEveryTicks => guardVolleyEveryTicks;
        public int DoorHitLimit => doorHitLimit;
        public int InvasionMentalLoss => invasionMentalLoss;
        public int GuardMentalLossPerTick => guardMentalLossPerTick;
        public int WorkEveryTicks => workEveryTicks;
        public float PercentPerAbility => percentPerAbility;
        public int MentalCostPerWork => mentalCostPerWork;
        public float PlanningUnlockPercent => planningUnlockPercent;
        public void Validate()
        {
            easy.Validate();
            medium.Validate();
            if (firstInvasionTick < 1 || presentationTicks == null || presentationGrades == null ||
                presentationGrades.Length == 0 || presentationGrades[0].MinimumScore != 0)
                throw new InvalidOperationException("Missing bunker event schedule or presentation grades.");
            int previousTick = 0;
            foreach (int tick in presentationTicks)
            {
                if (tick <= previousTick) throw new InvalidOperationException("Presentation ticks must be positive and strictly increasing.");
                previousTick = tick;
            }
            double previousScore = -1;
            foreach (var grade in presentationGrades)
            {
                if (string.IsNullOrWhiteSpace(grade.Label) || !double.IsFinite(grade.MinimumScore) || grade.MinimumScore <= previousScore)
                    throw new InvalidOperationException("Presentation grades need labels and increasing finite thresholds.");
                previousScore = grade.MinimumScore;
            }
            if (tiredMentalThreshold < 0 || tiredMentalThreshold > mentalMaximum)
                throw new InvalidOperationException("Invalid data.bunker.rules: tiredMentalThreshold outside mental range.");
            if (invasionApproachTicks < 1 || guardVolleyEveryTicks < 1 || doorHitLimit < 1 || invasionMentalLoss < 0 || guardMentalLossPerTick < 0)
                throw new InvalidOperationException("Invalid bunker invasion timing or mental loss.");
            if (mentalCostPerWork < 0)
                throw new InvalidOperationException("Invalid data.bunker.rules: mentalCostPerWork must be nonnegative.");
            if (hungerEveryTicks < 1 || hungerLoss < 0 || hungerLoss > HungerCapacity || emptyHungerMentalLossMultiplier < 1 ||
                foodRestore < 1 || foodRestore > HungerCapacity || servingsPerCan < 1)
                throw new InvalidOperationException("Invalid bunker hunger or food settings.");
            if (evaluationTargetProgress <= 0 || double.IsNaN(evaluationTargetProgress) || double.IsInfinity(evaluationTargetProgress) || evaluationReferenceTicks <= 0 ||
                mentalMaximum <= 0 || sleepMentalPerTick <= 0 || sleepingRoomCapacity <= 0 || workRoomCapacity <= 0 || wakeMental <= 0 || wakeMental > mentalMaximum ||
                totalTicks <= 0 || randomEventEveryTicks <= 0 || invasionEveryTicks <= 0 || workEveryTicks <= 0 ||
                secondsPerTick <= 0 || float.IsNaN(secondsPerTick) || float.IsInfinity(secondsPerTick) ||
                percentPerAbility < 0 || float.IsNaN(percentPerAbility) || float.IsInfinity(percentPerAbility) ||
                planningUnlockPercent < 0 || planningUnlockPercent >= 100 || float.IsNaN(planningUnlockPercent) || float.IsInfinity(planningUnlockPercent))
                throw new InvalidOperationException("Invalid data.bunker.rules: durations/intervals must be positive and coefficient finite/nonnegative.");
        }
    }
}
