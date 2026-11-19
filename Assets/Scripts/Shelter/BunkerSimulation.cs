using GameJam.Audio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.GameDev;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class BunkerSimulation : MonoBehaviour
    {
        [SerializeField] GameDevManager development;
        [SerializeField] ShelterRoster roster;
        Task initializing;
        BunkerTickClock clock;
        readonly List<(CharacterIdentity actor, GameDevField field, int ability)> workers = new List<(CharacterIdentity, GameDevField, int)>();
        readonly double[] totalAbility = new double[3];
        readonly float[] appliedProgress = new float[3];
        int pendingPostEventTick;
        public int PendingWaveHpModifier { get; private set; }
        public int CurrentWaveHpModifier { get; private set; }
        public int LastWaveTick { get; private set; }
        public int LastPresentationTick { get; private set; }
        public event Action<int, double, BunkerRules.PresentationGrade> PresentationDue;
        public void SetPendingWaveHpModifier(int value) => PendingWaveHpModifier=value;
        public int GetCurrentWaveEnemyHp(int baseHp)
        {
            if(baseHp<=0) throw new ArgumentOutOfRangeException(nameof(baseHp));
            return checked((int)Math.Max(1L,(long)baseHp+CurrentWaveHpModifier));
        }
        public BunkerRules Rules { get; private set; }
        public bool IsReady => clock != null;
        public int CurrentTick => clock?.CurrentTick ?? 0;
        public double FractionalTick => clock?.FractionalTick ?? 0;
        public int RemainingTicks => Rules ? Mathf.Max(0, Rules.TotalTicks - CurrentTick) : 0;
        public bool Paused { get; set; }
        readonly HashSet<object> pauseOwners = new HashSet<object>();
        public bool IsPaused => Paused || pauseOwners.Count > 0;
        public void AcquirePause(object owner) { if (owner == null) throw new ArgumentNullException(nameof(owner)); pauseOwners.Add(owner); }
        public void ReleasePause(object owner) { if (owner != null) pauseOwners.Remove(owner); }
        public string LastError { get; private set; }
        public event Action<int> TickOccurred;
        public event Action<int> RandomEventDue;
        public event Action<int> ZombieInvasionDue;
        public event Action<int> WorkProcessed;
        public event Action DurationEnded;

        async void Start()
        {
            try { await InitializeAsync(); }
            catch (Exception e) { if (this) { LastError = e.Message; Debug.LogException(e, this); } }
        }
        public Task InitializeAsync() => initializing ?? (initializing = LoadAsync());
        async Task LoadAsync()
        {
            if (!development || !roster) throw new InvalidOperationException("BunkerSimulation requires scene development and roster references.");
            var rules = await GameData.LoadSettingsAsync<BunkerRules>(BunkerRules.Address);
            if (!this) return;
            rules.Validate();
            await development.InitializeAsync();
            if (!this) return;
            await roster.InitializeAsync();
            if (!this) return;
            Rules = rules;
            var people = roster.Characters;
            var walkers = new ShelterWalker[people.Count];
            for (int i = 0; i < people.Count; i++) walkers[i] = people[i].GetComponent<ShelterWalker>();
            foreach (var header in FindObjectsByType<ShelterRoomHeader>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (header.gameObject.scene == gameObject.scene) header.Bind(walkers);
            development.SetScoreCalculator(new GameDevScoreCalculator(Rules.EvaluationTargetProgress, Rules.EvaluationReferenceTicks, () => CurrentTick));
            development.DevelopmentRestarted += ResetClock;
            ResetClock();
        }
        void ResetClock()
        {
            GameAudio.Music(GameSound.Bunker);
            if (TryGetComponent<ShelterSpeech>(out var speech)) speech.Clear();
            foreach (var actor in roster.Characters)
            {
                if (!actor) continue;
                actor.ConfigureMental(Rules);
                actor.ResetMental();
                actor.ResetSupplies();
                if (actor.TryGetComponent<WorkProgressFeedback>(out var feedback)) feedback.Hide();
            }
            pendingPostEventTick=0;
            PendingWaveHpModifier=CurrentWaveHpModifier=LastWaveTick=0;
            LastPresentationTick=0;
            clock = new BunkerTickClock(Rules);
            clock.Tick += ProcessTick;
            Paused = false;
            LastError = null;
        }
        void OnDestroy() { if (development) development.DevelopmentRestarted -= ResetClock; }
        void Update() => Advance(Time.deltaTime);
        public void Advance(double seconds)
        {
            if (!IsReady || !isActiveAndEnabled || IsPaused || development.Phase != GameDevPhase.Developing) return;
            if(pendingPostEventTick!=0)
            {
                int tick=pendingPostEventTick; pendingPostEventTick=0; CompleteTick(tick);
                if(IsPaused || development.Phase!=GameDevPhase.Developing) return;
            }
            clock.Paused = false;
            clock.Advance(seconds);
        }
        void ProcessTick(int tick)
        {
            foreach (var actor in roster.Characters)
                if (actor) { actor.AdvanceHungerTick(tick); actor.AdvanceMentalTick(); }
            // All due work on the last tick precedes submission of the final progress snapshot.
            if (tick % Rules.WorkEveryTicks == 0) { ProcessWork(); WorkProcessed?.Invoke(tick); }
            if (Rules.IsPresentationTick(tick))
            {
                double score = GameDevScoreCalculator.Calculate(development.Progress, Rules.EvaluationTargetProgress, tick, Rules.TotalTicks);
                var grade = Rules.GradePresentation(score);
                LastPresentationTick = tick;
                foreach (var actor in roster.Characters)
                    if (actor) actor.ApplyMentalChange(grade.MentalDelta, SurvivorEmotion.Cause.Presentation);
                PresentationDue?.Invoke(tick, score, grade);
            }
            else if (tick < Rules.TotalTicks && tick % Rules.RandomEventEveryTicks == 0) RandomEventDue?.Invoke(tick);
            if (tick < Rules.TotalTicks && (long)tick * 24 / Rules.TotalTicks != (long)(tick - 1) * 24 / Rules.TotalTicks && !IsPaused)
                GameAudio.Play(GameSound.Hour);
            if(IsPaused) { pendingPostEventTick=tick; clock.Paused=true; return; }
            CompleteTick(tick);
        }
        void CompleteTick(int tick)
        {
            if (Rules.IsInvasionTick(tick))
            {
                CurrentWaveHpModifier=PendingWaveHpModifier; PendingWaveHpModifier=0; LastWaveTick=tick;
                ZombieInvasionDue?.Invoke(tick);
            }
            TickOccurred?.Invoke(tick);
            if (tick == Rules.TotalTicks) { development.FinishDevelopment(); DurationEnded?.Invoke(); }
            clock.Paused = IsPaused || development.Phase != GameDevPhase.Developing || !isActiveAndEnabled;
        }
        internal bool PlanningUnlocksDepartments => Rules && development && development.Progress.Planning > Rules.PlanningUnlockPercent;

        internal bool CanDevelopIn(ShelterRoomType room)
        {
            if (!Rules || !development || development.Phase != GameDevPhase.Developing || Rules.PercentPerAbility == 0) return false;
            var progress = development.Progress;
            switch (room)
            {
                case ShelterRoomType.Planning: return progress.Planning < GameDevProgress.MaximumPercent;
                case ShelterRoomType.Graphics: return PlanningUnlocksDepartments && progress.Graphics < GameDevProgress.MaximumPercent;
                case ShelterRoomType.Programming: return PlanningUnlocksDepartments && progress.Programming < GameDevProgress.MaximumPercent;
                default: return false;
            }
        }

        void ProcessWork()
        {
            if (Rules.PercentPerAbility == 0) return;
            workers.Clear();
            Array.Clear(totalAbility, 0, totalAbility.Length);
            var characters = roster.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                var actor = characters[i];
                if (!actor || !actor.isActiveAndEnabled || actor.Definition == null) continue;
                var walker = actor.GetComponent<ShelterWalker>();
                if (!walker || !walker.CanWorkInCurrentRoom) continue;
                var room = walker.CurrentRoom.roomType;
                if (room == ShelterRoomType.ZombieInvasion) actor.Speak("guard");
                if (!walker.CanDoDevelopmentWork) continue;
                GameDevField field;
                int ability;
                switch (walker.CurrentRoom.roomType)
                {
                    case ShelterRoomType.Programming:
                        field = GameDevField.Programming;
                        ability = actor.Programming;
                        break;
                    case ShelterRoomType.Graphics:
                        field = GameDevField.Graphics;
                        ability = actor.Art;
                        break;
                    case ShelterRoomType.Planning:
                        field = GameDevField.Planning;
                        ability = actor.Planning;
                        break;
                    default:
                        continue;
                }
                workers.Add((actor, field, ability));
                totalAbility[(int)field] += ability;
            }
            if (Rules.MentalCostPerWork > 0)
                foreach (var worker in workers)
                    worker.actor.ApplyMentalChange(-Rules.MentalCostPerWork, SurvivorEmotion.Cause.Work);
            ApplyWork(GameDevField.Programming);
            ApplyWork(GameDevField.Graphics);
            ApplyWork(GameDevField.Planning);
            foreach (var worker in workers)
            {
                int index = (int)worker.field;
                if (appliedProgress[index] <= 0 || worker.ability == 0) continue;
                float share = (float)(appliedProgress[index] * (double)worker.ability / totalAbility[index]);
                if (worker.actor.TryGetComponent<WorkProgressFeedback>(out var feedback)) feedback.Show(worker.field, share);
                if (worker.actor.CanAct) worker.actor.Speak("work");
            }
        }

        void ApplyWork(GameDevField field)
        {
            int index = (int)field;
            float before = development.Progress.Get(field);
            development.AddProgress(field, (float)Math.Min(GameDevProgress.MaximumPercent, totalAbility[index] * Rules.PercentPerAbility));
            appliedProgress[index] = development.Progress.Get(field) - before;
        }
    }
}
