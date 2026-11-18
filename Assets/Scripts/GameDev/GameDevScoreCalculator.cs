using System;

namespace UnityGameJam.GameDev
{
    public interface IGameDevScoreCalculator
    {
        /// <returns>False while the scoring system has no result yet.</returns>
        bool TryCalculate(GameDevProgress progress, out double totalScore);
    }

    /// <summary>Progress divided by the expected progress at the current bunker tick. No percentage scaling.</summary>
    public sealed class GameDevScoreCalculator : IGameDevScoreCalculator
    {
        readonly double targetProgress;
        readonly int referenceTicks;
        readonly Func<int> currentTick;

        // A manager without a connected clock must keep waiting rather than invent time or a score.
        public GameDevScoreCalculator() { }

        public GameDevScoreCalculator(double targetProgress, int referenceTicks, Func<int> currentTick)
        {
            if (double.IsNaN(targetProgress) || double.IsInfinity(targetProgress) || targetProgress <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetProgress));
            if (referenceTicks <= 0) throw new ArgumentOutOfRangeException(nameof(referenceTicks));
            this.targetProgress = targetProgress;
            this.referenceTicks = referenceTicks;
            this.currentTick = currentTick ?? throw new ArgumentNullException(nameof(currentTick));
        }

        public bool TryCalculate(GameDevProgress progress, out double totalScore)
        {
            totalScore = default;
            if (currentTick == null) return false;
            int tick = currentTick();
            if (tick <= 0) return false;
            totalScore = Calculate(progress, targetProgress, tick, referenceTicks);
            GameDevEnding.ValidateScore(totalScore);
            return true;
        }
        internal static double Calculate(GameDevProgress progress, double targetProgress, int tick, int referenceTicks)
        {
            double expected = targetProgress * ((double)tick / referenceTicks);
            return ((double)progress.Planning + progress.Programming + progress.Graphics) / expected;
        }
    }
}
