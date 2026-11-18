using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

namespace UnityGameJam.Zombies
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ZombieInvasionController : MonoBehaviour
    {
        [SerializeField] BunkerSimulation simulation;
        [SerializeField] GameDevManager development;
        [SerializeField] ShelterRoster roster;
        [SerializeField] ShelterNavigation navigation;
        [SerializeField] ZombieAgent zombiePrefab;
        [SerializeField] TextMesh statusLabel;
        readonly List<ZombieAgent> zombies = new List<ZombieAgent>();
        readonly Dictionary<CharacterIdentity, Vector2> previousPositions = new Dictionary<CharacterIdentity, Vector2>();
        Task initializing;
        public ZombieData Data { get; private set; }
        public bool IsReady => Data != null;
        public string LastError { get; private set; }
        public string Message { get; private set; } = "좀비 침입 대기";
        public int Wave { get; private set; }
        public IReadOnlyList<ZombieAgent> Zombies => zombies.AsReadOnly();
        public ShelterRoster Roster => roster;
        public ShelterNavigation Navigation => navigation;
        public BunkerSimulation Simulation => simulation;
        public Func<int, int> RollDie { get; set; } = sides => UnityEngine.Random.Range(1, sides + 1);
        public bool CanAdvance => IsReady && isActiveAndEnabled && !simulation.IsPaused && development.Phase == GameDevPhase.Developing;

        void OnEnable()
        {
            if (!simulation || !development) return;
            simulation.AcquirePause(this);
            simulation.ZombieInvasionDue += OnWave;
            simulation.TickOccurred += OnTick;
            development.DevelopmentRestarted += ResetRun;
            development.Changed += OnDevelopmentChanged;
            _ = StartSafely();
        }
        async Task StartSafely()
        {
            try { await InitializeAsync(); }
            catch (Exception e) { if (this) { LastError = e.Message; Report("좀비 데이터 오류: " + e.Message); Debug.LogException(e, this); } }
            finally { if (this && simulation) simulation.ReleasePause(this); }
        }
        public Task InitializeAsync() => initializing ?? (initializing = LoadAsync());
        async Task LoadAsync()
        {
            if (!simulation || !development || !roster || !navigation || !zombiePrefab) throw new InvalidOperationException("Zombie scene references missing.");
            var data = await GameData.LoadSettingsAsync<ZombieData>(ZombieData.Address);
            data.Validate();
            await simulation.InitializeAsync();
            if (!this) return;
            Data = data;
            RememberPositions();
        }
        void OnDisable()
        {
            if (simulation) { simulation.ReleasePause(this); simulation.ZombieInvasionDue -= OnWave; simulation.TickOccurred -= OnTick; }
            if (development) { development.DevelopmentRestarted -= ResetRun; development.Changed -= OnDevelopmentChanged; }
            ClearZombies();
        }
        void OnDevelopmentChanged() { if (development.Phase != GameDevPhase.Developing) ClearZombies(); }
        public void ResetRun() { ClearZombies(); Wave = 0; LastError = null; Report("좀비 침입 대기"); RememberPositions(); }
        void ClearZombies()
        {
            foreach (var zombie in zombies.ToArray()) if (zombie) { zombie.Cleanup(); Destroy(zombie.gameObject); }
            zombies.Clear();
        }
        void OnWave(int tick)
        {
            if (IsReady && development.Phase == GameDevPhase.Developing && tick < simulation.Rules.TotalTicks) SpawnWave();
        }
        // Also used by the Play Mode inspector button. Scheduled waves consume NEXT_WAVE_HP in BunkerSimulation.
        public ZombieAgent SpawnWave()
        {
            if (!CanAdvance) throw new InvalidOperationException("Zombie system is not ready, paused, or run has ended.");
            var entry = SceneRooms().Where(a => a.roomType == ShelterRoomType.ZombieInvasion).OrderByDescending(a => a.FloorY).FirstOrDefault();
            if (!entry) { LastError = "활성 전투 공간(ZombieInvasion)이 필요합니다"; Report(LastError); return null; }
            int wave = ++Wave;
            var definition = Data.ForWave(wave);
            var zombie = Instantiate(zombiePrefab, new Vector3(entry.Right + Data.SpawnOffset, entry.FloorY), Quaternion.identity, transform);
            zombies.Add(zombie);
            zombie.Initialize(this, definition, simulation.GetCurrentWaveEnemyHp(definition.Hp), entry, simulation.CurrentTick);
            Report($"{wave}차 습격 · {definition.DisplayName} HP {zombie.Hp}");
            return zombie;
        }
        public IEnumerable<ShelterArea> SceneRooms() => FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID)
            .Where(a => a.gameObject.scene == gameObject.scene && a.kind == ShelterArea.AreaKind.Room && a.IsWalkable);
        public IEnumerable<CharacterIdentity> InRoom(ShelterArea room)
        {
            if (!room || !room.IsWalkable) yield break;
            foreach (var actor in roster.Characters)
                if (actor && actor.isActiveAndEnabled && Mathf.Abs(actor.transform.position.y - room.FloorY) < 0.05f && room.ContainsClick(actor.transform.position)) yield return actor;
        }
        public Vector2 PreviousPosition(CharacterIdentity actor) => previousPositions.TryGetValue(actor, out var point) ? point : (Vector2)actor.transform.position;
        void RememberPositions()
        {
            previousPositions.Clear();
            if (!roster) return;
            foreach (var actor in roster.Characters) if (actor) previousPositions[actor] = actor.transform.position;
        }
        void OnTick(int tick)
        {
            if (!CanAdvance) return;
            foreach (var zombie in zombies.ToArray()) if (zombie) zombie.ProcessTick(tick);
        }
        void Update() => AdvanceMovement(Time.deltaTime);
        public void AdvanceMovement(float seconds)
        {
            if (CanAdvance && seconds > 0)
                foreach (var zombie in zombies.ToArray()) if (zombie) zombie.AdvanceMovement(seconds);
            RememberPositions();
        }
        public void Report(string message) { Message = message; if (statusLabel) statusLabel.text = message; }
        public void NotifyKilled(ZombieAgent zombie)
        {
            zombies.Remove(zombie);
            Report($"{zombie.Definition.DisplayName} 처치 · 멘탈 +{Data.MentalRecovery} · 남은 좀비 {zombies.Count}");
        }
    }
}
