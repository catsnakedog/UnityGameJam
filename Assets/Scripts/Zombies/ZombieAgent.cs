using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

namespace UnityGameJam.Zombies
{
    public enum ZombieState { Defending, Invading, Encounter, Occupying, WaitingForRoute, Dead, Approaching }

    [RequireComponent(typeof(ShelterWalker))]
    public sealed class ZombieAgent : MonoBehaviour
    {
        [SerializeField] TextMesh healthLabel;
        ZombieInvasionController owner;
        ShelterWalker walker;
        ShelterArea entryRoom;
        readonly List<CharacterIdentity> fighters = new List<CharacterIdentity>();
        readonly List<CharacterIdentity> pendingRolls = new List<CharacterIdentity>();
        readonly List<string> rollMessages = new List<string>();
        CharacterIdentity attackTarget;
        ZombieState afterEncounter;
        readonly HashSet<CharacterIdentity> participants = new HashSet<CharacterIdentity>();
        int nextCombatTick, nextVolleyTick, earliestEntryTick, turn;
        double lastApproachTick;
        bool pendingReroll;
        int lastNavigationVersion = -1;
        public ZombieDefinition Definition { get; private set; }
        public ZombieState State { get; private set; }
        public int Hp { get; private set; }
        public int InitialHp { get; private set; }
        public ShelterArea TargetRoom { get; private set; }
        public CharacterIdentity Fighter => attackTarget ? attackTarget : fighters.FirstOrDefault();
        public CombatRoll LastRoll { get; private set; }
        public int ResolvedCombats { get; private set; }
        public ShelterArea EntryRoom => entryRoom;
        public Vector2 Doorway { get; private set; }
        public bool IsTrafficBlocked { get; private set; }
        ZombieData Rules => owner.Data;

        public void Initialize(ZombieInvasionController controller, ZombieDefinition definition, int hp, ShelterArea entry, Vector2 doorway, int tick)
        {
            owner = controller; Definition = definition; Hp = InitialHp = hp; entryRoom = entry;
            Doorway = doorway;
            walker = GetComponent<ShelterWalker>();
            walker.playerSelectable = false; walker.externallyDriven = true;
            walker.navigation = owner.Navigation; walker.characterName = definition.DisplayName;
            walker.walkSpeed = Rules.WalkSpeed; walker.arrivalOffset = 0;
            walker.FindLandingSpace = (exit, previous) => owner.FindLandingSpace(this, exit, previous);
            State = ZombieState.Approaching;
            lastApproachTick = tick;
            earliestEntryTick = tick + owner.Simulation.Rules.InvasionApproachTicks;
            nextVolleyTick = tick + owner.Simulation.Rules.GuardVolleyEveryTicks;
            RefreshLabel();
        }
        void WalkApproach(double time)
        {
            if (time <= lastApproachTick) return;
            float seconds = (float)((time - lastApproachTick) * owner.Simulation.Rules.SecondsPerTick);
            lastApproachTick = time;
            if (!entryRoom || !entryRoom.IsWalkable) return;
            Vector2 from = transform.position;
            var step = Vector2.MoveTowards(from, Doorway, Rules.WalkSpeed * seconds);
            walker.ApplyMovementPosition(Vector2.Lerp(from, step, LimitMovement(from, step)), seconds);
        }
        void ProcessApproach(int tick)
        {
            WalkApproach(tick);
            if (!entryRoom || !entryRoom.IsWalkable) return;
            if (pendingReroll) RetryVolley(tick);
            else if (tick >= nextVolleyTick)
            {
                nextVolleyTick = tick + owner.Simulation.Rules.GuardVolleyEveryTicks;
                if (owner.CanStartDefense(this))
                {
                    foreach (var actor in owner.InRoom(entryRoom))
                        if (owner.CanAttack(actor, this, tick)) Enlist(actor, tick);
                    if (fighters.Count > 0) ResolveCombat(tick);
                }
            }
            TryEnterRoom(tick);
        }
        void TryEnterRoom(int tick)
        {
            if (State != ZombieState.Approaching || tick < earliestEntryTick || !entryRoom || !entryRoom.IsWalkable ||
                Vector2.Distance(transform.position, Doorway) > 0.001f || !owner.CanStartDefense(this)) return;
            // Physical arrival ends the ranged window, including any still-contested shots.
            Unlock();
            State = ZombieState.Defending;
            entryRoom.SetOccupied(this, true);
            owner.ReportBreach(this);
            foreach (var actor in owner.InRoom(entryRoom))
                if (owner.CanAttack(actor, this, tick)) Enlist(actor, tick);
            if (fighters.Count > 0)
            {
                nextCombatTick = tick;
                ResolveCombat(tick);
            }
            else ValidateCombat(tick);
        }
        void RetryVolley(int tick)
        {
            RemoveUnavailableFighters(entryRoom);
            if (pendingRolls.Count > 0) ResolveCombat(tick);
            else Unlock();
        }
        bool HasConsciousDefender() => owner.InRoom(entryRoom).Any(a => a.CanAct);
        bool CanFight(CharacterIdentity actor) => actor && actor.isActiveAndEnabled && actor.CanAct && !actor.GetComponent<ShelterWalker>().IsInElevator;
        void Enlist(CharacterIdentity actor, int tick)
        {
            if (!CanFight(actor)) return;
            var movement = actor.GetComponent<ShelterWalker>();
            if (movement.IsMovementLocked) return;
            if (fighters.Count > 0 && tick >= nextCombatTick && !owner.CanAttack(actor, this, tick)) return;
            if (fighters.Count == 0) nextCombatTick = tick + Rules.CombatEveryTicks;
            movement.AcquireMovementLock(this);
            fighters.Add(actor);
        }
        void Unlock()
        {
            foreach (var actor in fighters)
                if (actor) actor.GetComponent<ShelterWalker>().ReleaseMovementLock(this);
            fighters.Clear();
            pendingRolls.Clear();
            attackTarget = null;
            pendingReroll = false;
            if (walker) walker.ReleaseMovementLock(this);
        }
        public void ProcessTick(int tick)
        {
            if (!owner.CanAdvance || State == ZombieState.Dead) return;
            if (State == ZombieState.Approaching) { ProcessApproach(tick); RefreshLabel(); return; }
            ValidateCombat(tick);
            if (fighters.Count > 0 && tick >= nextCombatTick && !pendingReroll) ResolveCombat(tick);
            RefreshLabel();
        }
        void ValidateCombat(int tick)
        {
            if (State != ZombieState.Defending && State != ZombieState.Occupying && State != ZombieState.Encounter) return;
            var room = State == ZombieState.Defending ? entryRoom :
                State == ZombieState.Occupying || afterEncounter == ZombieState.Occupying ? TargetRoom : null;
            RemoveUnavailableFighters(room);
            if (State == ZombieState.Defending && !HasConsciousDefender())
            { BeginInvasion(); return; }
            if (!pendingReroll)
            {
                if (room)
                    foreach (var actor in owner.InRoom(room)) Enlist(actor, tick);
                else if (State == ZombieState.Encounter && afterEncounter == ZombieState.Invading)
                    foreach (var actor in owner.Roster.Characters)
                        if (actor && Vector2.Distance(actor.transform.position, transform.position) <= Rules.EncounterDistance)
                            Enlist(actor, tick);
            }
            if (State == ZombieState.Occupying && fighters.Count > 0) BeginEncounter(ZombieState.Occupying);
            if (fighters.Count == 0)
            {
                Unlock();
                if (State == ZombieState.Encounter) State = afterEncounter;
            }
        }
        void RemoveUnavailableFighters(ShelterArea room)
        {
            for (int i = fighters.Count - 1; i >= 0; i--)
            {
                var actor = fighters[i];
                if (CanFight(actor) && (!room || room.ContainsFeet(actor.transform.position))) continue;
                if (actor) actor.GetComponent<ShelterWalker>().ReleaseMovementLock(this);
                fighters.RemoveAt(i);
                pendingRolls.Remove(actor);
            }
        }
        void ResolveCombat(int tick)
        {
            bool ranged = State == ZombieState.Approaching;
            if (!pendingReroll)
            {
                attackTarget = ranged ? null : fighters[turn++ % fighters.Count];
                pendingRolls.AddRange(fighters);
            }
            // All attacks in this exchange land together. Only the zombie's chosen opponent can lose mental.
            int zombieDie = pendingRolls.Count > 0 ? ZombieCombat.RollDie(Rules, owner.RollDie) : 0;
            int ties = 0;
            rollMessages.Clear();
            for (int i = 0; i < pendingRolls.Count; i++)
            {
                var actor = pendingRolls[i];
                owner.RecordAttack(actor, this, tick);
                if (ranged) actor.GetComponent<SurvivorAnimation>()?.PlayRangedAttack(transform.position.x);
                participants.Add(actor);
                LastRoll = new CombatRoll(ZombieCombat.RollDie(Rules, owner.RollDie), zombieDie, actor.Combat, Definition.Combat);
                if (LastRoll.Outcome == CombatOutcome.Tie) pendingRolls[ties++] = actor;
                string result = ApplyRoll(actor, LastRoll.Outcome);
                rollMessages.Add($"{actor.Definition.DisplayName} {LastRoll.SurvivorTotal} 대 {LastRoll.ZombieTotal} · {result}");
            }
            pendingRolls.RemoveRange(ties, pendingRolls.Count - ties);
            if (Hp == 0) { Die(ranged || State == ZombieState.Defending); return; }
            if (rollMessages.Count > 0) owner.Report($"{Definition.DisplayName} HP {Hp}\n" + string.Join("\n", rollMessages));
            pendingReroll = ties > 0;
            if (pendingReroll) return;
            if (ranged) Unlock();
            else
            {
                nextCombatTick = tick + Rules.CombatEveryTicks;
                ValidateCombat(tick);
            }
            RefreshLabel();
        }
        string ApplyRoll(CharacterIdentity actor, CombatOutcome outcome)
        {
            if (outcome == CombatOutcome.Tie) return "재굴림";
            ResolvedCombats++;
            if (outcome == CombatOutcome.SurvivorWins)
            {
                Hp = Mathf.Max(0, Hp - Rules.DamageToZombie);
                return $"좀비 HP -{Rules.DamageToZombie}";
            }
            if (actor != attackTarget) return "공격 실패";
            actor.SetMental(Mathf.Max(0, actor.CurrentMental - Rules.MentalDamage));
            return $"멘탈 -{Rules.MentalDamage}";
        }
        void BeginInvasion()
        {
            Unlock();
            if (entryRoom) entryRoom.SetOccupied(this, false);
            State = ZombieState.Invading;
            PickTarget();
            owner.Report($"방어 돌파 · {Definition.DisplayName} HP {Hp} · " + (TargetRoom ? TargetRoom.displayName + " 이동" : "연결된 방 대기"));
            RefreshLabel();
        }
        bool PickTarget()
        {
            if (!entryRoom || !entryRoom.IsWalkable) { State = ZombieState.WaitingForRoute; return false; }
            owner.Navigation.Refresh();
            var from = (Vector2)transform.position;
            var candidates = owner.SceneRooms().Where(a => a.roomType != ShelterRoomType.ZombieInvasion &&
                owner.Navigation.TryFindElevatorPath(from, a, 0, Rules.WalkSpeed, out _)).ToArray();
            var free = candidates.Where(a => !a.IsOccupied).ToArray();
            if (free.Length > 0) candidates = free;
            lastNavigationVersion = owner.Navigation.Version;
            if (candidates.Length == 0) { TargetRoom = null; State = ZombieState.WaitingForRoute; return false; }
            TargetRoom = candidates[Random.Range(0, candidates.Length)]; State = ZombieState.Invading;
            if (!walker.MoveTo(TargetRoom)) { State = ZombieState.WaitingForRoute; return false; }
            return true;
        }
        void BeginEncounter(ZombieState resume)
        {
            afterEncounter = resume; State = ZombieState.Encounter;
            walker.AcquireMovementLock(this);
            owner.Report($"강제 전투 · {fighters.Count}명 ↔ {Definition.DisplayName}");
            RefreshLabel();
        }
        public void AdvanceMovement(float seconds)
        {
            if (!owner || !owner.CanAdvance || seconds <= 0 || State == ZombieState.Dead) return;
            if (walker.IsInElevator) { RefreshLabel("엘리베이터 이동"); return; }
            IsTrafficBlocked = false;
            if (State == ZombieState.Approaching)
            {
                WalkApproach(owner.Simulation.FractionalTick);
                if (pendingReroll) RetryVolley(owner.Simulation.CurrentTick);
                TryEnterRoom(owner.Simulation.CurrentTick);
                RefreshLabel();
                return;
            }
            ValidateCombat(owner.Simulation.CurrentTick);
            if (pendingReroll) ResolveCombat(Mathf.Max(nextCombatTick, owner.Simulation.CurrentTick));
            if (State == ZombieState.Dead) return;
            if ((State == ZombieState.Occupying || State == ZombieState.Encounter && afterEncounter == ZombieState.Occupying) && (!TargetRoom || !TargetRoom.IsWalkable))
            { if (TargetRoom) TargetRoom.SetOccupied(this, false); Unlock(); State = ZombieState.Invading; PickTarget(); }
            if (State == ZombieState.WaitingForRoute)
            {
                if (owner.Navigation.Version != lastNavigationVersion) PickTarget();
                RefreshLabel(); return;
            }
            if (State != ZombieState.Invading) { walker.Animate(0); return; }
            if (!TargetRoom || !TargetRoom.IsWalkable) { PickTarget(); if (State != ZombieState.Invading) return; }
            // Substeps preserve corners/ladder transitions; relative swept contact also catches a survivor crossing between frames.
            int steps = Mathf.Max(1, Mathf.CeilToInt(seconds / 0.04f));
            for (int i = 0; i < steps && State == ZombieState.Invading; i++)
            {
                Vector2 before = transform.position;
                float dt = seconds / steps;
                walker.Tick(dt, LimitMovement);
                if (walker.IsInElevator) break;
                foreach (var actor in owner.Roster.Characters)
                {
                    if (!CanFight(actor)) continue;
                    Vector2 oldActor = owner.PreviousPosition(actor), newActor = actor.transform.position;
                    Vector2 a = before - Vector2.Lerp(oldActor, newActor, (float)i / steps);
                    Vector2 b = (Vector2)transform.position - Vector2.Lerp(oldActor, newActor, (float)(i + 1) / steps);
                    Vector2 delta = b - a;
                    float t = delta.sqrMagnitude < 0.000001f ? 0 : Mathf.Clamp01(-Vector2.Dot(a, delta) / delta.sqrMagnitude);
                    if ((a + delta * t).sqrMagnitude <= Rules.EncounterDistance * Rules.EncounterDistance)
                        Enlist(actor, owner.Simulation.CurrentTick);
                }
                if (fighters.Count > 0) { BeginEncounter(ZombieState.Invading); break; }
                if (walker.IsInElevator) break;
                bool stoppedInsideTarget = IsTrafficBlocked && !walker.RequestedElevator && TargetRoom.ContainsFeet(transform.position);
                if (!walker.IsMoving || stoppedInsideTarget)
                {
                    if (walker.CurrentRoom == TargetRoom || stoppedInsideTarget)
                    {
                        walker.Stop();
                        State = ZombieState.Occupying; TargetRoom.SetOccupied(this, true);
                        owner.Report($"{TargetRoom.displayName} 점거 · 기능 정지 · 캐릭터를 보내 처치하세요");
                        ValidateCombat(owner.Simulation.CurrentTick);
                    }
                    else { State = ZombieState.WaitingForRoute; lastNavigationVersion = owner.Navigation.Version; }
                }
                walker.Animate(dt);
            }
            RefreshLabel();
        }
        float LimitMovement(Vector2 from, Vector2 to)
        {
            float fraction = owner.MovementFraction(this, from, to);
            if (fraction < 1f) IsTrafficBlocked = true;
            return fraction;
        }
        void Die(bool defense)
        {
            var rewarded = defense ? owner.InRoom(entryRoom).ToArray() : participants.Where(a => a).ToArray();
            State = ZombieState.Dead;
            foreach (var actor in rewarded) actor.SetMental((int)System.Math.Min(int.MaxValue, (long)actor.CurrentMental + Rules.MentalRecovery));
            owner.NotifyKilled(this, string.Join("\n", rollMessages));
            gameObject.SetActive(false); Destroy(gameObject);
        }
        internal void StopCombat()
        {
            Unlock();
            if (walker) walker.Stop();
            RefreshLabel("전투 종료");
        }
        public void Cleanup()
        {
            StopCombat();
            if (entryRoom) entryRoom.SetOccupied(this, false);
            if (TargetRoom) TargetRoom.SetOccupied(this, false);
        }
        void OnDisable() => Cleanup();
        void RefreshLabel(string detail = null)
        {
            if (!healthLabel || Definition == null) return;
            string state = State == ZombieState.Approaching ? (IsTrafficBlocked ? "입구 대기" : "전투 방 접근 중") : State == ZombieState.Defending ? $"전투 방 교전 · {fighters.Count}명" :
                State == ZombieState.Occupying ? "점거 중" : State == ZombieState.Encounter ? $"강제 전투 · {fighters.Count}명" :
                State == ZombieState.WaitingForRoute ? "경로 대기" : IsTrafficBlocked ? "앞 좀비 대기" : "침입 중";
            healthLabel.text = $"{Definition.DisplayName} · HP {Hp}/{InitialHp}\n{detail ?? state}" + (pendingReroll ? " · 동점 재굴림" : "");
        }
    }
}
