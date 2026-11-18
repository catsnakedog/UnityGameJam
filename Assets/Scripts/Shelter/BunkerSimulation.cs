using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine;
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
        int pendingPostEventTick;
        public int PendingWaveHpModifier { get; private set; }
        public int CurrentWaveHpModifier { get; private set; }
        public int LastWaveTick { get; private set; }
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
            development.SetScoreCalculator(new GameDevScoreCalculator(Rules.EvaluationTargetProgress, Rules.EvaluationReferenceTicks, () => CurrentTick));
            development.DevelopmentRestarted += ResetClock;
            ResetClock();
        }
        void ResetClock()
        {
            foreach (var actor in roster.Characters) if (actor) { actor.ConfigureMental(Rules); actor.ResetMental(); actor.ResetSupplies(); }
            pendingPostEventTick=0;
            PendingWaveHpModifier=CurrentWaveHpModifier=LastWaveTick=0;
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
                if (actor) { actor.AdvanceMentalTick(); actor.AdvanceGuardDutyTick(); actor.AdvanceHungerTick(tick); }
            // All due work on the last tick precedes submission of the final progress snapshot.
            if (tick % Rules.WorkEveryTicks == 0) { ProcessWork(); WorkProcessed?.Invoke(tick); }
            if (tick % Rules.RandomEventEveryTicks == 0) RandomEventDue?.Invoke(tick);
            if(IsPaused) { pendingPostEventTick=tick; clock.Paused=true; return; }
            CompleteTick(tick);
        }
        void CompleteTick(int tick)
        {
            if (tick % Rules.InvasionEveryTicks == 0)
            {
                CurrentWaveHpModifier=PendingWaveHpModifier; PendingWaveHpModifier=0; LastWaveTick=tick;
                ZombieInvasionDue?.Invoke(tick);
            }
            TickOccurred?.Invoke(tick);
            if (tick == Rules.TotalTicks) { development.FinishDevelopment(); DurationEnded?.Invoke(); }
            clock.Paused = IsPaused || development.Phase != GameDevPhase.Developing || !isActiveAndEnabled;
        }
        internal bool CanDevelopIn(ShelterRoomType room)
        {
            if (!Rules || !development || development.Phase != GameDevPhase.Developing || Rules.PercentPerAbility == 0) return false;
            var progress = development.Progress;
            bool unlocked = progress.Planning > Rules.PlanningUnlockPercent;
            switch (room)
            {
                case ShelterRoomType.Planning: return progress.Planning < GameDevProgress.MaximumPercent;
                case ShelterRoomType.Graphics: return unlocked && progress.Graphics < GameDevProgress.MaximumPercent;
                case ShelterRoomType.Programming: return unlocked && progress.Programming < GameDevProgress.MaximumPercent;
                default: return false;
            }
        }

        void ProcessWork()
        {
            if (Rules.PercentPerAbility == 0) return;
            double programming = 0, graphics = 0, planning = 0;
            foreach (var actor in roster.Characters)
            {
                if (!actor || !actor.isActiveAndEnabled || actor.Definition == null) continue;
                var walker = actor.GetComponent<ShelterWalker>();
                if (!walker || !walker.CanWorkInCurrentRoom || !CanDevelopIn(walker.CurrentRoom.roomType)) continue;
                switch (walker.CurrentRoom.roomType)
                {
                    case ShelterRoomType.Programming:
                        programming += actor.Programming;
                        break;
                    case ShelterRoomType.Graphics:
                        graphics += actor.Art;
                        break;
                    case ShelterRoomType.Planning:
                        planning += actor.Planning;
                        break;
                    default:
                        continue;
                }
                if (Rules.MentalCostPerWork > 0)
                    actor.SetMental(Math.Max(0, actor.CurrentMental - Rules.MentalCostPerWork));
            }
            development.AddProgress(GameDevField.Programming, (float)Math.Min(100, programming * Rules.PercentPerAbility));
            development.AddProgress(GameDevField.Graphics, (float)Math.Min(100, graphics * Rules.PercentPerAbility));
            development.AddProgress(GameDevField.Planning, (float)Math.Min(100, planning * Rules.PercentPerAbility));
        }
    }
}
