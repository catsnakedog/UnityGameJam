using GameJam.Audio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

namespace UnityGameJam.Zombies
{
    public enum ZombieState { Defending, Invading, Encounter, Occupying, WaitingForRoute, Dead, Approaching }

    [RequireComponent(typeof(ShelterWalker))]
    public sealed class ZombieAgent : MonoBehaviour
    {
        [SerializeField] TextMesh healthLabel;
        [SerializeField] Image healthFill;
        ZombieInvasionController owner;
        ShelterWalker walker;
        ZombieAnimation spriteAnimation;
        ShelterArea entryRoom;
        readonly List<CharacterIdentity> fighters = new List<CharacterIdentity>();
        internal IReadOnlyList<CharacterIdentity> Fighters => fighters;
        internal ShelterArea FormationArea { get; private set; }
        bool formationChanged;
        readonly List<CharacterIdentity> pendingRolls = new List<CharacterIdentity>();
        readonly List<string> rollMessages = new List<string>();
        CharacterIdentity attackTarget;
        ZombieState afterEncounter;
        readonly HashSet<CharacterIdentity> participants = new HashSet<CharacterIdentity>();
        int nextCombatTick, nextVolleyTick, doorHits, turn;
        double lastApproachTick;
        bool pendingReroll;
        bool knockedAtDoor;
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

        SoundHandle footsteps;

        public void Initialize(ZombieInvasionController controller, ZombieDefinition definition, int hp, ShelterArea entry, Vector2 doorway, int tick)
        {
            owner = controller; Definition = definition; Hp = InitialHp = hp; entryRoom = entry;
            Doorway = doorway;
            walker = GetComponent<ShelterWalker>();
            spriteAnimation = GetComponent<ZombieAnimation>();
            spriteAnimation.Bind(definition.Id);
            walker.playerSelectable = false; walker.externallyDriven = true;
            walker.navigation = owner.Navigation; walker.characterName = definition.DisplayName;
            walker.walkSpeed = Rules.WalkSpeed; walker.arrivalOffset = 0;
            walker.FindLandingSpace = (exit, previous) => owner.FindLandingSpace(this, exit, previous);
            State = ZombieState.Approaching;
            lastApproachTick = tick;
            knockedAtDoor = false;
            footsteps = GameAudio.Play(GameSound.ZombieSteps, transform, definition.DisplayName == "거대 좀비" ? .85f : 1f);
            ShelterSpeech.Say(transform, "좀비", "approach");
            healthLabel.transform.position = new Vector3(transform.position.x, spriteAnimation.HeadY + .26f, healthLabel.transform.position.z);
            healthLabel.gameObject.SetActive(true);
            RefreshHealth();
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
        void CheckDoor(int tick)
        {
            if (!entryRoom || !entryRoom.IsWalkable || Vector2.Distance(transform.position, Doorway) > .001f ||
                !owner.CanStartDefense(this)) return;
            if (!knockedAtDoor)
            {
                knockedAtDoor = true;
                nextVolleyTick = tick;
                footsteps.Stop();
                GameAudio.Play(GameSound.DoorBang);
                ShelterSpeech.Say(transform, "좀비", "knock");
            }
            RemoveUnavailableFighters(entryRoom);
            foreach (var actor in owner.InRoom(entryRoom)) Enlist(actor, tick);
            if (fighters.Count > 0)
            {
                if (tick < nextVolleyTick) return;
                var shooter = doorHits == 0 || fighters.Count == 1 ? fighters[0] : fighters[1];
                if (!owner.CanAttack(shooter, this, tick)) return;
                if (shooter != fighters[0])
                {
                    fighters.Add(fighters[0]);
                    fighters.RemoveAt(0);
                }
                nextVolleyTick = tick + owner.Simulation.Rules.GuardVolleyEveryTicks;
                owner.RecordAttack(shooter, this, tick);
                shooter.GetComponent<SurvivorAnimation>()?.PlayAttack(transform.position.x);
                GameAudio.Play(GameSound.CanThrow);
                ShelterSpeech.Say(shooter.transform, shooter.Definition.Role, "can_throw");
                participants.Add(shooter);
                int damage = Mathf.Min(Hp, Rules.DamageToZombie);
                Hp -= damage;
                RefreshHealth();
                if (damage > 0) spriteAnimation.PlayHit();
                doorHits++;
                ResolvedCombats++;
                GameAudio.Play(GameSound.CanHit);
                ShelterSpeech.Say(transform, "좀비", "can_hit");
                owner.ShowDamage(transform.position, damage);
                rollMessages.Clear();
                rollMessages.Add($"{shooter.Definition.DisplayName} · 문 앞 공격 -{damage}");
                owner.Report(rollMessages[0]);
                if (Hp == 0) { Die(true); return; }
                if (doorHits < owner.Simulation.Rules.DoorHitLimit) return;
            }
            else if (HasConsciousDefender()) return;
            Unlock();
            State = ZombieState.Defending;
            entryRoom.SetOccupied(this, true);
            ShelterSpeech.Say(transform, "좀비", "invade");
            GameAudio.Play(GameSound.DoorBust);
            owner.ReportBreach(this);
            UpdateDefense(lastApproachTick, tick);
        }
        bool WithinMeleeReach(CharacterIdentity actor) =>
            ((Vector2)(actor.transform.position - transform.position)).sqrMagnitude <= Rules.EncounterDistance * Rules.EncounterDistance;

        void UpdateDefense(double time, int tick)
        {
            float seconds = (float)System.Math.Max(0, time - lastApproachTick) * owner.Simulation.Rules.SecondsPerTick;
            lastApproachTick = System.Math.Max(time, lastApproachTick);
            RemoveUnavailableFighters(entryRoom);
            if (fighters.Count == 0)
            {
                CharacterIdentity nearest = null;
                float distance = float.PositiveInfinity;
                foreach (var actor in owner.InRoom(entryRoom))
                {
                    if (!CanFight(actor) || actor.GetComponent<ShelterWalker>().IsMovementLocked) continue;
                    float gap = Vector2.Distance(actor.transform.position, transform.position);
                    if (gap < distance) { nearest = actor; distance = gap; }
                }
                if (!nearest)
                {
                    if (!HasConsciousDefender()) BeginInvasion();
                    return;
                }
                if (distance > Rules.EncounterDistance && seconds > 0f)
                {
                    Vector2 from = transform.position;
                    var step = Vector2.MoveTowards(from, nearest.transform.position,
                        Mathf.Min(Rules.WalkSpeed * seconds, distance - Rules.EncounterDistance * 0.95f));
                    walker.ApplyMovementPosition(Vector2.Lerp(from, step, LimitMovement(from, step)), seconds);
                }
            }
            bool starting = fighters.Count == 0;
            if (!pendingReroll)
                foreach (var actor in owner.InRoom(entryRoom))
                    if (WithinMeleeReach(actor) && owner.CanAttack(actor, this, tick)) Enlist(actor, tick);
            if (fighters.Count == 0) { pendingReroll = false; return; }
            if (starting) nextCombatTick = tick;
            if (tick >= nextCombatTick || pendingReroll) ResolveCombat(tick);
        }
        bool HasConsciousDefender() => owner.InRoom(entryRoom).Any(CanFight);
        bool CanFight(CharacterIdentity actor) => actor && actor.isActiveAndEnabled && actor.CanAct &&
            actor.TryGetComponent<ShelterWalker>(out var walker) && !walker.IsInElevator && !walker.InTent;
        void Enlist(CharacterIdentity actor, int tick)
        {
            if (!CanFight(actor)) return;
            var movement = actor.GetComponent<ShelterWalker>();
            if (movement.IsMovementLocked) return;
            if (fighters.Count > 0 && tick >= nextCombatTick && !owner.CanAttack(actor, this, tick)) return;
            if (fighters.Count == 0) nextCombatTick = tick + Rules.CombatEveryTicks;
            movement.AcquireMovementLock(this);
            fighters.Add(actor);
            if (actor.TryGetComponent<SurvivorEmotion>(out var emotion)) emotion.ShowAnger();
            formationChanged = true;
        }
        void FormUp(ShelterArea area)
        {
            if (!area || (!formationChanged && FormationArea == area)) return;
            FormationArea = area;
            formationChanged = false;
            owner.ArrangeCombat(area);
        }
        void ReleaseFighter(CharacterIdentity actor)
        {
            if (!actor) return;
            var movement = actor.GetComponent<ShelterWalker>();
            movement.ReleaseMovementLock(this);
            if (FormationArea) movement.LeaveCombatPosition();
        }
        void Unlock()
        {
            foreach (var actor in fighters) ReleaseFighter(actor);
            fighters.Clear();
            pendingRolls.Clear();
            attackTarget = null;
            pendingReroll = false;
            if (walker) walker.ReleaseMovementLock(this);
            var previousArea = FormationArea;
            FormationArea = null;
            formationChanged = false;
            if (owner && previousArea) owner.ArrangeCombat(previousArea);
        }
        public void ProcessTick(int tick)
        {
            if (!owner.CanAdvance || State == ZombieState.Dead) return;
            if (State == ZombieState.Approaching) { WalkApproach(tick); CheckDoor(tick); return; }
            if (State == ZombieState.Defending) { UpdateDefense(tick, tick); return; }
            ValidateCombat(tick);
            if (fighters.Count > 0 && tick >= nextCombatTick && !pendingReroll) ResolveCombat(tick);
        }
        void ValidateCombat(int tick)
        {
            if (State != ZombieState.Occupying && State != ZombieState.Encounter) return;
            var room = State == ZombieState.Occupying ? TargetRoom : FormationArea;
            RemoveUnavailableFighters(room);
            if (!pendingReroll)
            {
                if (room)
                    foreach (var actor in owner.InRoom(room)) Enlist(actor, tick);
                else if (State == ZombieState.Encounter && afterEncounter == ZombieState.Invading)
                    foreach (var actor in owner.Roster.Characters)
                        if (actor && Vector2.Distance(actor.transform.position, transform.position) <= Rules.EncounterDistance)
                            Enlist(actor, tick);
            }
            if (fighters.Count > 0)
            {
                if (State == ZombieState.Occupying) BeginEncounter(ZombieState.Occupying);
                else FormUp(room);
            }
            else
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
                if (CanFight(actor) && (!room || room.ContainsFeet(actor.transform.position)) &&
                    (State != ZombieState.Defending || WithinMeleeReach(actor))) continue;
                ReleaseFighter(actor);
                fighters.RemoveAt(i);
                pendingRolls.Remove(actor);
                formationChanged = true;
            }
        }
        void ResolveCombat(int tick)
        {
            if (!pendingReroll)
            {
                attackTarget = fighters[turn++ % fighters.Count];
                pendingRolls.AddRange(fighters);
            }
            // All attacks in this exchange land together. Only the zombie's chosen opponent can lose mental.
            int zombieDie = pendingRolls.Count > 0 ? ZombieCombat.RollDie(Rules, owner.RollDie) : 0;
            int ties = 0;
            int hpBeforeExchange = Hp;
            rollMessages.Clear();
            for (int i = 0; i < pendingRolls.Count; i++)
            {
                var actor = pendingRolls[i];
                owner.RecordAttack(actor, this, tick);
                if (!pendingReroll) actor.GetComponent<SurvivorAnimation>()?.PlayAttack(transform.position.x);
                participants.Add(actor);
                LastRoll = new CombatRoll(ZombieCombat.RollDie(Rules, owner.RollDie), zombieDie, actor.Combat, Definition.Combat);
                if (LastRoll.Outcome == CombatOutcome.Tie) pendingRolls[ties++] = actor;
                string result = ApplyRoll(actor, LastRoll.Outcome);
                if (actor.CanAct && actor.TryGetComponent<SurvivorEmotion>(out var emotion)) emotion.ShowAnger();
                rollMessages.Add($"{actor.Definition.DisplayName} {LastRoll.SurvivorTotal} 대 {LastRoll.ZombieTotal} · {result}");
            }
            pendingRolls.RemoveRange(ties, pendingRolls.Count - ties);
            if (Hp < hpBeforeExchange)
            {
                spriteAnimation.PlayHit();
                owner.ShowDamage(transform.position, hpBeforeExchange - Hp);
                ShelterSpeech.Say(transform, "좀비", "melee_hit");
            }
            if (Hp == 0) { Die(State == ZombieState.Defending); return; }
            if (rollMessages.Count > 0) owner.Report($"{Definition.DisplayName} HP {Hp}\n" + string.Join("\n", rollMessages));
            pendingReroll = ties > 0;
            if (pendingReroll) return;
            nextCombatTick = tick + Rules.CombatEveryTicks;
            if (State == ZombieState.Defending)
            {
                if (!HasConsciousDefender()) BeginInvasion();
            }
            else ValidateCombat(tick);
        }
        string ApplyRoll(CharacterIdentity actor, CombatOutcome outcome)
        {
            if (outcome == CombatOutcome.Tie) return "재굴림";
            ResolvedCombats++;
            if (outcome == CombatOutcome.SurvivorWins)
            {
                int before = Hp;
                Hp = Mathf.Max(0, Hp - Rules.DamageToZombie);
                RefreshHealth();
                if (Hp < before) GameAudio.Play(GameSound.MeleeHit);
                ShelterSpeech.Say(actor.transform, actor.Definition.Role, "melee_win");
                if (before > 0 && Hp == 0) ShelterSpeech.Say(actor.transform, actor.Definition.Role, "kill");
                return $"좀비 HP -{Rules.DamageToZombie}";
            }
            GameAudio.Play(GameSound.MeleeMiss);
            ShelterSpeech.Say(actor.transform, actor.Definition.Role, "melee_loss");
            if (actor != attackTarget) return "공격 실패";
            int change = actor.ApplyMentalChange(-Rules.MentalDamage, SurvivorEmotion.Cause.Combat);
            if (change < 0) actor.GetComponent<SurvivorAnimation>()?.PlayHit(transform.position.x);
            owner.ShowDamage(actor.transform.position, -change);
            return $"멘탈 {change}";
        }
        void BeginInvasion()
        {
            Unlock();
            if (entryRoom) entryRoom.SetOccupied(this, false);
            State = ZombieState.Invading;
            PickTarget();
            owner.Report($"방어 돌파 · {Definition.DisplayName} HP {Hp} · " + (TargetRoom ? TargetRoom.displayName + " 이동" : "연결된 방 대기"));
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
            FormUp(resume == ZombieState.Occupying ? TargetRoom : owner.EncounterAreaAt(transform.position));
            owner.Report($"강제 전투 · {fighters.Count}명 vs {Definition.DisplayName}");
        }
        public void AdvanceMovement(float seconds)
        {
            if (!owner || !owner.CanAdvance || seconds <= 0 || State == ZombieState.Dead) return;
            if (walker.IsInElevator) return;
            IsTrafficBlocked = false;
            if (State == ZombieState.Approaching)
            {
                WalkApproach(owner.Simulation.FractionalTick);
                CheckDoor(owner.Simulation.CurrentTick);
                return;
            }
            if (State == ZombieState.Defending)
            {
                UpdateDefense(owner.Simulation.FractionalTick, owner.Simulation.CurrentTick);
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
                return;
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
            if (State == ZombieState.Approaching) GameAudio.Play(GameSound.Defend);
            GameAudio.Play(GameSound.ZombieDown);
            State = ZombieState.Dead;
            ShelterSpeech.Say(transform, "좀비", "death");
            foreach (var actor in rewarded) actor.SetMental((int)System.Math.Min(int.MaxValue, (long)actor.CurrentMental + Rules.MentalRecovery), SurvivorEmotion.Cause.Victory);
            owner.NotifyKilled(this, string.Join("\n", rollMessages));
            gameObject.SetActive(false); Destroy(gameObject);
        }
        internal void StopCombat()
        {
            footsteps.Stop();
            Unlock();
            if (walker) walker.Stop();
        }
        public void Cleanup()
        {
            StopCombat();
            if (entryRoom) entryRoom.SetOccupied(this, false);
            if (TargetRoom) TargetRoom.SetOccupied(this, false);
        }
        void OnDisable() => Cleanup();
        void RefreshHealth()
        {
            healthFill.fillAmount = (float)Hp / InitialHp;
            healthLabel.text = $"{Hp}/{InitialHp}";
        }
    }
}
