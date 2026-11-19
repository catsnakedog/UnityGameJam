using System;
using GameJam.Audio;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine;

namespace UnityGameJam.GameDev
{
    [DisallowMultipleComponent]
    public sealed class GameDevManager : MonoBehaviour
    {
        GameDevEndingCatalog endings;
        IGameDevScoreCalculator calculator = new GameDevScoreCalculator();
        Task initializing;
        public GameDevPhase Phase { get; private set; } = GameDevPhase.Loading;
        public GameDevProgress Progress { get; private set; }
        public GameDevProgress SubmittedProgress { get; private set; }
        public double? TotalScore { get; private set; }
        public GameDevEnding Ending { get; private set; }
        public string LastError { get; private set; }
        public event Action Changed;
        public event Action DevelopmentRestarted;
        public event Action<GameDevEnding, double> EndingReached;

        async void Start()
        {
            try { await InitializeAsync(); }
            catch (Exception error) { if (this != null) Debug.LogException(error, this); }
        }

        public Task InitializeAsync()
        {
            if (endings != null) return Task.CompletedTask;
            if (initializing != null && !initializing.IsCompleted) return initializing;
            initializing = LoadAsync();
            return initializing;
        }

        async Task LoadAsync()
        {
            Phase = GameDevPhase.Loading;
            LastError = null;
            Changed?.Invoke();
            try
            {
                await GameData.InitializeAsync();
                if (this == null) return;
                endings = GameDevEndingCatalog.FromTable(GameData.GetTable(GameDevKeys.EndingsTable));
                RestartDevelopment();
            }
            catch (Exception error)
            {
                if (this != null)
                {
                    LastError = error.Message;
                    Phase = GameDevPhase.Failed;
                    Changed?.Invoke();
                }
                throw;
            }
        }

        public bool SetProgress(GameDevField field, float percent)
        {
            var next = Progress.With(field, percent); // Validate even if the run has ended.
            if (Phase != GameDevPhase.Developing) return false;
            if (Progress.Get(field) == next.Get(field)) return true;
            Progress = next;
            Changed?.Invoke();
            return true;
        }

        public bool AddProgress(GameDevField field, float percentagePoints)
        {
            if (float.IsNaN(percentagePoints) || float.IsInfinity(percentagePoints)) throw new ArgumentOutOfRangeException(nameof(percentagePoints));
            double next = (double)Progress.Get(field) + percentagePoints;
            return SetProgress(field, (float)Math.Max(0, Math.Min(GameDevProgress.MaximumPercent, next)));
        }

        /// <summary>Explicit completion hook for a future game rule. Progress alone never ends development.</summary>
        public bool FinishDevelopment()
        {
            if (Phase != GameDevPhase.Developing) return false;
            SubmittedProgress = Progress;
            Phase = GameDevPhase.AwaitingScore;
            Changed?.Invoke();
            TryEvaluateScore();
            return true;
        }

        public void SetScoreCalculator(IGameDevScoreCalculator value)
        {
            calculator = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool TryEvaluateScore()
        {
            if (Phase != GameDevPhase.AwaitingScore) return false;
            try
            {
                if (!calculator.TryCalculate(SubmittedProgress, out double score)) return false;
                return SubmitFinalScore(score);
            }
            catch (Exception error)
            {
                LastError = error.Message;
                Changed?.Invoke();
                Debug.LogException(error, this);
                return false;
            }
        }

        /// <summary>External scoring systems may publish the total after FinishDevelopment.</summary>
        public bool SubmitFinalScore(double totalScore)
        {
            GameDevEnding.ValidateScore(totalScore);
            if (Phase != GameDevPhase.AwaitingScore) return false;
            var selected = endings.Select(totalScore);
            TotalScore = totalScore;
            Ending = selected;
            LastError = null;
            Phase = GameDevPhase.Ended;
            bool goodEnding = selected.Id == "ending_released" || selected.Id == "ending_breakthrough";
            GameAudio.Music(goodEnding ? GameSound.Victory : GameSound.Fail);
            GameAudio.Play(GameSound.Stamp);
            Changed?.Invoke();
            EndingReached?.Invoke(selected, totalScore);
            return true;
        }

        public bool RestartDevelopment()
        {
            if (endings == null) return false;
            Progress = default;
            SubmittedProgress = default;
            TotalScore = null;
            Ending = null;
            LastError = null;
            Phase = GameDevPhase.Developing;
            DevelopmentRestarted?.Invoke();
            Changed?.Invoke();
            return true;
        }
    }
}
