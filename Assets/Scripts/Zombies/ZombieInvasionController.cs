using GameJam.Audio;
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
        [SerializeField] Transform entryDoor;
        [SerializeField] CombatDamagePopup damagePopup;

        internal void ShowDamage(Vector3 feet, int amount)
        {
            if (damagePopup && amount > 0)
                Instantiate(damagePopup, feet + Vector3.up * 2.6f, Quaternion.identity, transform).Show(amount);
        }
        readonly List<ZombieAgent> zombies = new List<ZombieAgent>();
        readonly Dictionary<CharacterIdentity, Vector2> previousPositions = new Dictionary<CharacterIdentity, Vector2>();
        readonly Dictionary<CharacterIdentity, (int tick, ZombieAgent zombie)> lastAttacks = new Dictionary<CharacterIdentity, (int, ZombieAgent)>();
        Task initializing;
        public ZombieData Data { get; private set; }
        public bool IsReady => Data != null;
        public string LastError { get; private set; }
        public string Message { get; private set; } = "좀비 침입 대기";
        public int Wave { get; private set; }
        public event Action<ZombieAgent> ZombieSpawned;
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
        void OnDevelopmentChanged()
        {
            if (development.Phase != GameDevPhase.AwaitingScore) return;
            foreach (var zombie in zombies) if (zombie) zombie.StopCombat();
        }
        public void ResetRun() { ClearZombies(); Wave = 0; LastError = null; Report("좀비 침입 대기"); RememberPositions(); }
        void ClearZombies()
        {
            lastAttacks.Clear();
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
            navigation.Refresh();
            navigation.GetConnectedFloorBounds(entry.Centre, out var left, out var right);
            // Approach from the exposed wall, away from the interior landing and its boarding queue.
            int side = right - entry.Right > entry.Left - left ? -1 : 1;
            Vector2 doorway = entry.ArrivalPoint(side * (entry.Right - entry.Left) * 0.5f);
            if (entryDoor && entryDoor.IsChildOf(entry.transform))
            {
                side = entryDoor.position.x < entry.Centre.x ? -1 : 1;
                doorway.x = entryDoor.position.x + side * Data.EncounterDistance;
            }
            float spawnDistance = Data.WalkSpeed * simulation.Rules.SecondsPerTick * simulation.Rules.InvasionApproachTicks;
            // Append behind the queue rather than filling a gap ahead of an older arrival.
            foreach (var other in zombies)
                if (IsLive(other) && other.EntryRoom == entry && other.State == ZombieState.Approaching)
                    spawnDistance = Mathf.Max(spawnDistance, side * (other.transform.position.x - doorway.x) + Data.MinimumSpacing);
            Vector2 spawn = doorway + Vector2.right * (side * spawnDistance);
            while (zombies.Any(z => IsLive(z) && Vector2.Distance(spawn, z.transform.position) < Data.MinimumSpacing)) spawn.x += side * Data.MinimumSpacing;
            var zombie = Instantiate(zombiePrefab, spawn, Quaternion.identity, transform);
            zombies.Add(zombie);
            GameAudio.Play(GameSound.Alarm);
            Report($"{wave}차 습격 · {definition.DisplayName}");
            zombie.Initialize(this, definition, simulation.GetCurrentWaveEnemyHp(definition.Hp), entry, doorway, simulation.CurrentTick);
            ZombieSpawned?.Invoke(zombie);
            return zombie;
        }
        static bool IsLive(ZombieAgent zombie) => zombie && zombie.isActiveAndEnabled && zombie.State != ZombieState.Dead;
        internal void IncludeApproachingZombies(ref float left, ref float right)
        {
            foreach (var zombie in zombies)
            {
                if (!IsLive(zombie) || zombie.State != ZombieState.Approaching) continue;
                float x = zombie.transform.position.x;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }
        }
        internal void ReportBreach(ZombieAgent zombie)
        {
            int loss = simulation.Rules.InvasionMentalLoss;
            CharacterIdentity witness = null;
            float nearest = float.PositiveInfinity;
            foreach (var actor in roster.Characters)
            {
                if (!actor) continue;
                actor.ApplyMentalChange(-loss, SurvivorEmotion.Cause.Breach);
                if (!actor.isActiveAndEnabled || !actor.CanAct) continue;
                float distance = (actor.transform.position - zombie.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                witness = actor;
            }
            if (witness) ShelterSpeech.Say(witness.transform, witness.Definition.Role, "invasion");
            Report($"{zombie.Definition.DisplayName} 전투 방 침입 · 전원 기본 멘탈 -{loss}");
        }
        public bool CanStartDefense(ZombieAgent candidate)
        {
            foreach (var other in zombies)
            {
                if (other == candidate) return true;
                if (IsLive(other) && other.EntryRoom == candidate.EntryRoom && (other.State == ZombieState.Approaching || other.State == ZombieState.Defending)) return false;
            }
            return false;
        }
        public float MovementFraction(ZombieAgent moving, Vector2 from, Vector2 to)
        {
            float allowed = 1f;
            var walker = moving.GetComponent<ShelterWalker>();
            bool walkingPast = walker.IsMoving && !walker.RequestedElevator && walker.Destination;
            Vector2 destination = walkingPast ? walker.Destination.ArrivalPoint(walker.arrivalOffset) : from;
            foreach (var other in zombies)
            {
                if (other == moving || !IsLive(other)) continue;
                var obstacle = other.GetComponent<ShelterWalker>();
                if (obstacle.IsInElevator) continue;
                // A passer may cross a call pad, but cannot park on top of its caller.
                if (walkingPast && obstacle.RequestedElevator && Vector2.Distance(destination, obstacle.transform.position) >= Data.MinimumSpacing &&
                    Vector2.Distance(obstacle.transform.position, obstacle.RequestedElevator.WaitingPoint(obstacle)) < 0.03f) continue;
                allowed = Mathf.Min(allowed, ZombieTraffic.SafeFraction(from, to, other.transform.position, Data.MinimumSpacing));
            }
            return allowed;
        }
        bool IsGroundObstacle(ZombieAgent moving, ZombieAgent other) =>
            other != moving && IsLive(other) && !other.GetComponent<ShelterWalker>().IsInElevator;

        bool HasGroundSpace(ZombieAgent moving, Vector2 point)
        {
            foreach (var other in zombies)
                if (IsGroundObstacle(moving, other) &&
                    Vector2.Distance(point, other.transform.position) < Data.MinimumSpacing - 0.0001f) return false;
            return true;
        }
        public Vector2? FindLandingSpace(ZombieAgent moving, Vector2 exit, Vector2 previous)
        {
            if (HasGroundSpace(moving, previous)) return previous;
            if (HasGroundSpace(moving, exit)) return exit;
            navigation.GetConnectedFloorBounds(exit, out var left, out var right);
            Vector2? best = null;
            float nearest = float.PositiveInfinity;
            void Consider(float x)
            {
                float distance = Mathf.Abs(x - exit.x);
                if (x < left || x > right || distance >= nearest) return;
                var point = new Vector2(x, exit.y);
                if (!navigation.AreaAt(point) || !HasGroundSpace(moving, point)) return;
                best = point;
                nearest = distance;
            }
            // With an occupied doorway, the nearest free spot borders a grounded zombie's clearance radius.
            foreach (var other in zombies)
            {
                if (!IsGroundObstacle(moving, other)) continue;
                Vector2 obstacle = other.transform.position;
                float dy = obstacle.y - exit.y;
                float radius = Data.MinimumSpacing;
                if (Mathf.Abs(dy) >= radius) continue;
                float dx = Mathf.Sqrt(radius * radius - dy * dy);
                Consider(obstacle.x - dx);
                Consider(obstacle.x + dx);
            }
            return best;
        }
        public IEnumerable<ShelterArea> SceneRooms() => FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID)
            .Where(a => a.gameObject.scene == gameObject.scene && a.kind == ShelterArea.AreaKind.Room && a.IsWalkable);
        public IEnumerable<CharacterIdentity> InRoom(ShelterArea room)
        {
            if (!room || !room.IsWalkable) yield break;
            foreach (var actor in roster.Characters)
                if (actor && actor.isActiveAndEnabled && !actor.GetComponent<ShelterWalker>().IsInElevator && room.ContainsFeet(actor.transform.position)) yield return actor;
        }

        internal ShelterArea EncounterAreaAt(Vector2 feet)
        {
            // Elevator landings overlap playable rooms and corridors in the authored map.
            ShelterArea corridor = null;
            foreach (var area in navigation.Areas)
            {
                if (!area.IsWalkable || !area.ContainsFeet(feet)) continue;
                if (area.kind == ShelterArea.AreaKind.Room) return area;
                if (!corridor || area.Right - area.Left < corridor.Right - corridor.Left) corridor = area;
            }
            return corridor ? corridor : navigation.AreaAt(feet);
        }

        internal void ArrangeCombat(ShelterArea area)
        {
            int enemies = 0, developers = 0;
            foreach (var zombie in zombies)
            {
                if (!IsLive(zombie) || zombie.FormationArea != area) continue;
                enemies++;
                foreach (var actor in zombie.Fighters) if (actor) developers++;
            }

            float half = (area.Right - area.Left) * 0.5f;
            int enemySlot = 0, developerSlot = 0;
            foreach (var zombie in zombies)
            {
                if (!IsLive(zombie) || zombie.FormationArea != area) continue;
                var enemyPoint = new Vector2(area.Left + (enemySlot++ + 0.5f) * half / enemies, area.FloorY);
                zombie.GetComponent<ShelterWalker>().TakeCombatPosition(enemyPoint, false);
                foreach (var actor in zombie.Fighters)
                {
                    if (!actor) continue;
                    float spacing = half / developers;
                    var point = new Vector2(area.Centre.x + (developerSlot++ + 0.5f) * spacing, area.FloorY);
                    actor.GetComponent<ShelterWalker>().TakeCombatPosition(point, true);
                    previousPositions[actor] = point; // Taking a slot is not a swept walking contact.
                }
            }
        }

        internal bool CanAttack(CharacterIdentity actor, ZombieAgent zombie, int tick) =>
            !lastAttacks.TryGetValue(actor, out var attack) || attack.tick != tick || attack.zombie == zombie;
        internal void RecordAttack(CharacterIdentity actor, ZombieAgent zombie, int tick) => lastAttacks[actor] = (tick, zombie);
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
        internal void NotifyKilled(ZombieAgent zombie, string attacks)
        {
            if (!zombies.Remove(zombie)) return;
            Report($"{zombie.Definition.DisplayName} 처치 · 멘탈 +{Data.MentalRecovery} · 남은 좀비 {zombies.Count}\n{attacks}");
        }
    }
}
