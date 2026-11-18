using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

namespace UnityGameJam.Zombies
{
    public enum ZombieState { Defending, Invading, Encounter, Occupying, WaitingForRoute, Dead }

    [RequireComponent(typeof(ShelterWalker))]
    public sealed class ZombieAgent : MonoBehaviour
    {
        [SerializeField] TextMesh healthLabel;
        ZombieInvasionController owner;
        ShelterWalker walker;
        ShelterArea entryRoom;
        CharacterIdentity fighter;
        ZombieState afterEncounter;
        readonly HashSet<CharacterIdentity> participants = new HashSet<CharacterIdentity>();
        readonly HashSet<CharacterIdentity> passedContacts = new HashSet<CharacterIdentity>();
        int nextCombatTick, turn;
        bool approachingEntry, pendingReroll;
        int lastNavigationVersion = -1;
        public ZombieDefinition Definition { get; private set; }
        public ZombieState State { get; private set; }
        public int Hp { get; private set; }
        public int InitialHp { get; private set; }
        public int DefenseAttempts { get; private set; }
        public int DefenseBudget { get; private set; }
        public ShelterArea TargetRoom { get; private set; }
        public CharacterIdentity Fighter => fighter;
        public CombatRoll LastRoll { get; private set; }
        public int ResolvedCombats { get; private set; }
        ZombieData Rules => owner.Data;

        public void Initialize(ZombieInvasionController controller, ZombieDefinition definition, int hp, ShelterArea entry, int tick)
        {
            owner = controller; Definition = definition; Hp = InitialHp = hp; entryRoom = entry;
            walker = GetComponent<ShelterWalker>();
            walker.playerSelectable = false; walker.externallyDriven = true;
            walker.navigation = owner.Navigation; walker.characterName = definition.DisplayName;
            walker.walkSpeed = Rules.WalkSpeed; walker.climbSpeed = Rules.ClimbSpeed; walker.arrivalOffset = 0;
            DefenseBudget = checked(owner.InRoom(entryRoom).Count() * Rules.AttemptsPerDefender);
            State = ZombieState.Defending;
            if (!HasConsciousDefender()) BeginInvasion(); else SelectDefender(tick);
            RefreshLabel();
        }
        bool HasConsciousDefender() => owner.InRoom(entryRoom).Any(a => a.CanAct);
        bool Available(CharacterIdentity actor) => actor && actor.isActiveAndEnabled && actor.CanAct && !actor.GetComponent<ShelterWalker>().IsMovementLocked;
        void Lock(CharacterIdentity actor, int tick)
        {
            fighter = actor; fighter.GetComponent<ShelterWalker>().AcquireMovementLock(this);
            participants.Add(actor); nextCombatTick = tick + Rules.CombatEveryTicks; pendingReroll = false;
        }
        void Unlock()
        {
            if (fighter) fighter.GetComponent<ShelterWalker>().ReleaseMovementLock(this);
            fighter = null; pendingReroll = false;
        }
        void SelectDefender(int tick)
        {
            var candidates = owner.InRoom(entryRoom).Where(Available).ToArray();
            if (candidates.Length > 0) Lock(candidates[turn++ % candidates.Length], tick);
        }
        public void ProcessTick(int tick)
        {
            if (!owner.CanAdvance || State == ZombieState.Dead) return;
            ValidateCombat(tick);
            if (fighter && tick >= nextCombatTick && !pendingReroll) ResolveCombat(tick);
            RefreshLabel();
        }
        void ValidateCombat(int tick)
        {
            if (fighter && (!fighter.isActiveAndEnabled || !fighter.CanAct))
            {
                Unlock();
                if (State == ZombieState.Encounter) State = afterEncounter;
            }
            if (State == ZombieState.Defending)
            {
                if (!HasConsciousDefender() || DefenseAttempts >= DefenseBudget) { BeginInvasion(); return; }
                if (!fighter) SelectDefender(tick);
            }
            else if (State == ZombieState.Occupying && !fighter)
            {
                var actor = owner.InRoom(TargetRoom).FirstOrDefault(Available);
                if (actor) BeginEncounter(actor, ZombieState.Occupying, tick);
            }
        }
        void ResolveCombat(int tick)
        {
            if (!fighter) return;
            var actor = fighter;
            LastRoll = ZombieCombat.Roll(Rules, actor.Definition.Combat, Definition.Combat, owner.RollDie);
            if (LastRoll.Outcome == CombatOutcome.Tie) { pendingReroll = true; RefreshLabel("동점 · 재굴림"); return; }
            pendingReroll = false; ResolvedCombats++;
            bool defending = State == ZombieState.Defending;
            if (defending) DefenseAttempts++;
            if (LastRoll.Outcome == CombatOutcome.SurvivorWins) Hp = Mathf.Max(0, Hp - Rules.DamageToZombie);
            else actor.SetMental(Mathf.Max(0, actor.CurrentMental - Rules.MentalDamage));
            owner.Report($"{actor.Definition.DisplayName} {LastRoll.SurvivorDie}+{actor.Definition.Combat} : {Definition.DisplayName} {LastRoll.ZombieDie}+{Definition.Combat} · " +
                (LastRoll.Outcome == CombatOutcome.SurvivorWins ? $"좀비 HP -{Rules.DamageToZombie}" : $"멘탈 -{Rules.MentalDamage}"));
            Unlock();
            if (Hp == 0) { Die(defending); return; }
            if (defending)
            {
                if (DefenseAttempts >= DefenseBudget || !HasConsciousDefender()) BeginInvasion(); else SelectDefender(tick);
            }
            else
            {
                passedContacts.Add(actor);
                State = afterEncounter;
                if (State == ZombieState.Occupying) ValidateCombat(tick);
            }
            RefreshLabel();
        }
        void BeginInvasion()
        {
            Unlock();
            State = ZombieState.Invading;
            approachingEntry = true;
            PickTarget();
            owner.Report($"방어 돌파 · {Definition.DisplayName} HP {Hp} · " + (TargetRoom ? TargetRoom.displayName + " 이동" : "연결된 방 대기"));
            RefreshLabel();
        }
        Vector2 EntryPoint => entryRoom.ArrivalPoint((entryRoom.Right - entryRoom.Left) * 0.5f);
        bool PickTarget()
        {
            if (!entryRoom || !entryRoom.IsWalkable) { State = ZombieState.WaitingForRoute; return false; }
            owner.Navigation.Refresh();
            var from = approachingEntry ? EntryPoint : (Vector2)transform.position;
            var candidates = owner.SceneRooms().Where(a => a.roomType != ShelterRoomType.ZombieInvasion &&
                owner.Navigation.TryFindPath(from, approachingEntry ? null : walker.CurrentLadder, a, 0, Rules.WalkSpeed, Rules.ClimbSpeed, out _)).ToArray();
            var free = candidates.Where(a => !a.IsOccupied).ToArray();
            if (free.Length > 0) candidates = free;
            lastNavigationVersion = owner.Navigation.Version;
            if (candidates.Length == 0) { TargetRoom = null; State = ZombieState.WaitingForRoute; return false; }
            TargetRoom = candidates[Random.Range(0, candidates.Length)]; State = ZombieState.Invading;
            if (!approachingEntry && !walker.MoveTo(TargetRoom)) { State = ZombieState.WaitingForRoute; return false; }
            return true;
        }
        void BeginEncounter(CharacterIdentity actor, ZombieState resume, int tick)
        {
            afterEncounter = resume; State = ZombieState.Encounter; Lock(actor, tick);
            owner.Report($"강제 전투 · {actor.Definition.DisplayName} ↔ {Definition.DisplayName}");
            RefreshLabel();
        }
        public void AdvanceMovement(float seconds)
        {
            if (!owner || !owner.CanAdvance || seconds <= 0 || State == ZombieState.Dead) return;
            ValidateCombat(owner.Simulation.CurrentTick);
            if (pendingReroll && fighter) ResolveCombat(owner.Simulation.CurrentTick);
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
            passedContacts.RemoveWhere(a => !a || Vector2.Distance(a.transform.position, transform.position) > Rules.EncounterDistance * 2);
            // Substeps preserve corners/ladder transitions; relative swept contact also catches a survivor crossing between frames.
            int steps = Mathf.Max(1, Mathf.CeilToInt(seconds / 0.04f));
            for (int i = 0; i < steps && State == ZombieState.Invading; i++)
            {
                Vector2 before = transform.position;
                float dt = seconds / steps;
                if (approachingEntry)
                {
                    if (!entryRoom || !entryRoom.IsWalkable) { State = ZombieState.WaitingForRoute; break; }
                    Vector2 next = Vector2.MoveTowards(transform.position, EntryPoint, Rules.WalkSpeed * dt);
                    transform.position = new Vector3(next.x, next.y, transform.position.z);
                    if (Vector2.Distance(next, EntryPoint) < 0.001f)
                    {
                        approachingEntry = false;
                        if (!walker.MoveTo(TargetRoom)) { State = ZombieState.WaitingForRoute; lastNavigationVersion = owner.Navigation.Version; }
                    }
                }
                else walker.Tick(dt);
                CharacterIdentity contact = null;
                float earliest = float.PositiveInfinity;
                foreach (var actor in owner.Roster.Characters)
                {
                    if (!Available(actor) || passedContacts.Contains(actor)) continue;
                    Vector2 oldActor = owner.PreviousPosition(actor), newActor = actor.transform.position;
                    Vector2 a = before - Vector2.Lerp(oldActor, newActor, (float)i / steps);
                    Vector2 b = (Vector2)transform.position - Vector2.Lerp(oldActor, newActor, (float)(i + 1) / steps);
                    Vector2 delta = b - a;
                    float t = delta.sqrMagnitude < 0.000001f ? 0 : Mathf.Clamp01(-Vector2.Dot(a, delta) / delta.sqrMagnitude);
                    if ((a + delta * t).sqrMagnitude <= Rules.EncounterDistance * Rules.EncounterDistance && t < earliest) { contact = actor; earliest = t; }
                }
                if (contact) { BeginEncounter(contact, ZombieState.Invading, owner.Simulation.CurrentTick); break; }
                if (!approachingEntry && !walker.IsMoving)
                {
                    if (walker.CurrentRoom == TargetRoom)
                    {
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
        void Die(bool defense)
        {
            var rewarded = defense ? owner.InRoom(entryRoom).ToArray() : participants.Where(a => a).ToArray();
            Cleanup(); State = ZombieState.Dead;
            foreach (var actor in rewarded) actor.SetMental((int)System.Math.Min(int.MaxValue, (long)actor.CurrentMental + Rules.MentalRecovery));
            owner.NotifyKilled(this);
            gameObject.SetActive(false); Destroy(gameObject);
        }
        public void Cleanup()
        {
            Unlock();
            if (TargetRoom) TargetRoom.SetOccupied(this, false);
            if (walker) walker.Stop();
        }
        void OnDisable() => Cleanup();
        void RefreshLabel(string detail = null)
        {
            if (!healthLabel || Definition == null) return;
            string state = State == ZombieState.Defending ? $"방어 {DefenseAttempts}/{DefenseBudget}" :
                State == ZombieState.Occupying ? "점거 중" : State == ZombieState.Encounter ? "강제 전투" :
                State == ZombieState.WaitingForRoute ? "경로 대기" : "침입 중";
            healthLabel.text = $"{Definition.DisplayName} · HP {Hp}/{InitialHp}\n{detail ?? state}";
        }
    }
}
