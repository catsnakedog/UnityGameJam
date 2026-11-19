using GameJam.Audio;
using UnityEngine;
using System.Collections.Generic;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterWalker : MonoBehaviour
    {
        public string characterName = "MIRA";
        public ShelterNavigation navigation;
        [Tooltip("Spacing at a room destination, in world units.")]
        public float arrivalOffset;
        public SpriteRenderer selectionMarker;
        public TextMesh nameLabel;
        public bool playerSelectable = true;
        [HideInInspector] public bool externallyDriven;
        readonly HashSet<object> movementLocks = new HashSet<object>();
        public bool IsMovementLocked => InTent || movementLocks.Count > 0 || (Identity && !Identity.CanAct);
        ShelterTent reservedTent;
        public bool InTent => reservedTent && reservedTent.Occupied;
        readonly List<Canvas> hiddenCanvases = new List<Canvas>();
        readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        public void AcquireMovementLock(object owner) { if (owner != null) movementLocks.Add(owner); }
        public void ReleaseMovementLock(object owner) { if (owner != null) movementLocks.Remove(owner); }
        UnityGameJam.Characters.CharacterIdentity identity;

        private UnityGameJam.Characters.CharacterIdentity Identity =>
            identity ? identity : (identity = GetComponent<UnityGameJam.Characters.CharacterIdentity>());

        [Header("Movement")]
        [Min(0.1f)] public float walkSpeed = 3.2f;
        [HideInInspector] public float climbSpeed = 2.2f; // Serialized legacy value; elevators own their travel speed.

        [Header("Placeholder character")]
        public Transform visual;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        public ShelterArea Destination { get; private set; }
        ShelterArea currentRoom;
        public ShelterArea CurrentRoom
        {
            get => currentRoom;
            private set
            {
                if (currentRoom == value) return;
                if (currentRoom) currentRoom.Leave(this);
                currentRoom = value;
                if (currentRoom) currentRoom.Enter(this);
            }
        }
        public ShelterElevator CurrentElevator { get; private set; }
        public ShelterElevator RequestedElevator { get; private set; }
        public bool IsInElevator => CurrentElevator != null;
        public System.Func<Vector2, Vector2, Vector2?> FindLandingSpace { get; set; }
        public ShelterLadder CurrentLadder => null;
        public bool IsClimbing => false;
        public Vector2 Velocity { get; private set; }
        public bool IsMoving => route != null && waypoint < route.Count;
        public string Status { get; private set; } = "IDLE";
        private List<ShelterNavigation.Waypoint> route;
        private int waypoint;
        private int routeVersion;
        private int rideIndex = -1;
        private ShelterArea afterRideDestination;
        private bool stopAfterRide;
        private float destinationOffset;

        private ShelterWalker rescueTarget;
        private ShelterWalker rescuedBy;
        private ShelterArea rescueBed;
        private float patientOffset;
        private bool carrying;
        public string RoomOrderFailure { get; private set; }
        public bool IsBeingCarried => rescuedBy && rescuedBy.carrying;
        internal bool IsCarrying => carrying;
        public bool IsRescuing => rescueTarget;
        public bool NeedsRescue => Identity && Identity.IsFainted && !Identity.IsRestingAfterRescue;

        private float facing = 1f;
        internal bool FacingLeft => facing < 0;
        internal bool AnimationPaused => simulation && simulation.IsPaused;
        internal bool CanDoDevelopmentWork => CanWorkInCurrentRoom && simulation && simulation.CanDevelopIn(CurrentRoom.roomType)
            && CurrentRoom.HasWorkSlot(this, simulation.Rules.WorkRoomCapacity);
        internal bool TryGetAssignedWorkstation(out ShelterArea.Workstation station)
        {
            station = default;
            return CanWorkInCurrentRoom && Identity && CurrentRoom.TryGetWorkstation(Identity.Definition, out station);
        }
        private float phase;
        private BunkerSimulation simulation;
        private bool started;

        private void Start()
        {
            foreach(var candidate in FindObjectsByType<BunkerSimulation>(FindObjectsSortMode.None))
                if(candidate.gameObject.scene==gameObject.scene) { simulation=candidate; break; }
            FindNavigation();
            if (nameLabel != null) nameLabel.text = characterName;
            if (Identity) Identity.RefreshStatusLabel();
            ResolveInitialRoom();
            started = true;
            SetSelected(false);
        }

        private void OnEnable()
        {
            if (started) ResolveInitialRoom();
        }

        private void Update()
        {
            if (externallyDriven) return;
            if(simulation && simulation.IsPaused) return;
            Tick(Time.deltaTime);
            Animate(Time.deltaTime);
        }

        private void FindNavigation()
        {
            if (navigation != null) return;
            foreach (var graph in FindObjectsByType<ShelterNavigation>(FindObjectsSortMode.None))
                if (graph.gameObject.scene == gameObject.scene) { navigation = graph; break; }
        }

        public void SetSelected(bool selected)
        {
            if (selectionMarker != null) selectionMarker.enabled = selected;
            if (nameLabel != null) nameLabel.color = selected ? new Color(0.95f, 0.8f, 0.45f) : new Color(0.75f, 0.8f, 0.76f);
        }

        public bool ContainsClick(Vector2 point)
        {
            if (InTent) return false;
            var p = transform.InverseTransformPoint(point);
            return Mathf.Abs(p.x) <= 0.48f && p.y >= -0.08f && p.y <= 2.15f;
        }

        internal bool TryGetReservedX(ShelterArea room, out float position)
        {
            position = transform.position.x;
            if (!Identity || !isActiveAndEnabled) return false;
            if (rescuedBy && rescuedBy.rescueBed == room)
            {
                position = room.ArrivalPoint(rescuedBy.patientOffset).x;
                return true;
            }
            if (!IsMoving) return !IsBeingCarried && CurrentRoom == room;
            if (Destination != room || stopAfterRide || (IsRescuing && !carrying)) return false;
            position = room.ArrivalPoint(destinationOffset).x;
            return true;
        }

        public bool MoveTo(ShelterArea room)
        {
            RoomOrderFailure = null;
            if (!room || IsRescuing || IsMovementLocked) return false;
            float offset = arrivalOffset;
            if (Identity && room.kind == ShelterArea.AreaKind.Room) offset = room.StandingOffset(this);
            if (!MoveTo(room, offset)) return false;
            LeaveTent();
            return true;
        }

        public bool UseTent(ShelterTent tent)
        {
            RoomOrderFailure = null;
            if (!Identity || IsRescuing || IsMovementLocked || !tent) return false;
            if (!tent.AvailableTo(this, Identity.Rules)) { RoomOrderFailure = "사용할 수 없는 텐트입니다"; return false; }
            if (Identity.CurrentMental >= Identity.Rules.MentalMaximum) { RoomOrderFailure = "멘탈이 이미 가득 찼습니다"; return false; }
            if (!MoveTo(tent.Room, tent.Offset)) return false;
            ReserveTent(tent);
            return true;
        }

        void ReserveTent(ShelterTent tent)
        {
            LeaveTent();
            reservedTent = tent;
            tent.Reserve(this);
        }

        void EnterTent()
        {
            if (!reservedTent || CurrentRoom != reservedTent.Room || !CurrentRoom.IsOperational ||
                Identity.CurrentMental >= Identity.Rules.MentalMaximum) { LeaveTent(); return; }
            if (!reservedTent.Occupied) GameAudio.Play(GameSound.Sleep);
            reservedTent.Enter();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if (!renderer.forceRenderingOff) { renderer.forceRenderingOff = true; hiddenRenderers.Add(renderer); }
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
                if (canvas.enabled) { canvas.enabled = false; hiddenCanvases.Add(canvas); }
        }

        internal void LeaveTent()
        {
            if (InTent && isActiveAndEnabled) GameAudio.Play(GameSound.Wake);
            if (reservedTent) reservedTent.Release();
            reservedTent = null;
            foreach (var renderer in hiddenRenderers) if (renderer) renderer.forceRenderingOff = false;
            foreach (var canvas in hiddenCanvases) if (canvas) canvas.enabled = true;
            hiddenRenderers.Clear();
            hiddenCanvases.Clear();
        }

        private bool MoveTo(ShelterArea room, float offset)
        {
            if (IsMovementLocked) return false;
            FindNavigation();
            if (!IsInElevator && Destination == room && Mathf.Approximately(offset, destinationOffset) && IsMoving && navigation && routeVersion == navigation.Version) return true;
            Vector2 from = IsInElevator && rideIndex >= 0 ? route[rideIndex].position : (Vector2)transform.position;
            if (navigation == null || !navigation.TryFindElevatorPath(from, room, offset, walkSpeed, out var nextRoute)) return false;
            destinationOffset = offset;
            if (IsInElevator) { afterRideDestination = room; stopAfterRide = false; Destination = room; Status = "하차 후 목적지 변경"; return true; }
            ReleaseRequest();
            Destination = room;
            CurrentRoom = null;
            route = nextRoute;
            waypoint = 0;
            routeVersion = navigation.Version;
            Status = "MOVING";
            ReserveNextRide();
            return true;
        }

        public void Stop()
        {
            if (!InTent) LeaveTent();
            if (IsInElevator) { stopAfterRide = true; afterRideDestination = null; Status = "도착층에서 정지"; return; }
            ReleaseRescueTarget(false);
            ReleaseRequest();
            route = null;
            Destination = null;
            Velocity = Vector2.zero;
            Status = "STOPPED";
        }

        void ReleaseRequest()
        {
            var lift = RequestedElevator; RequestedElevator = null; rideIndex = -1;
            if (lift) lift.Cancel(this);
        }
        void ReserveNextRide()
        {
            if (RequestedElevator || route == null) return;
            for (int i = waypoint; i < route.Count; i++)
            {
                var leg = route[i]; if (!leg.elevator) continue;
                rideIndex = i; RequestedElevator = leg.elevator;
                if (!RequestedElevator.RequestRide(this, leg.fromFloor, leg.position.y, leg.side))
                { RequestedElevator = null; rideIndex = -1; route = null; Status = "NO ROUTE"; }
                return;
            }
        }
        // Keep the authored order of the body, name, emote and gauges inside the cabin.
        const int PassengerOrderOffset = -80;
        bool behindPassage;
        internal int RideOrderOffset => behindPassage ? PassengerOrderOffset : 0;

        internal void SetRideDepth(bool aboard)
        {
            if (behindPassage == aboard) return;
            behindPassage = aboard;
            int delta = aboard ? PassengerOrderOffset : -PassengerOrderOffset;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.sortingOrder += delta;
            foreach (var canvas in GetComponentsInChildren<Canvas>(true)) canvas.sortingOrder += delta;
            if (carrying && rescueTarget) rescueTarget.SetRideDepth(aboard);
        }

        public void BeginRide(ShelterElevator lift)
        {
            CurrentElevator = lift;
            CurrentRoom = null;
            Status = "엘리베이터 탑승";
        }
        public void ApplyRidePosition(Vector2 point, float dt) => ApplyMovementPosition(point, dt);
        internal void TakeCombatPosition(Vector2 point, bool faceLeft)
        {
            facing = faceLeft ? -1f : 1f;
            ApplyMovementPosition(point, 0f);
        }

        internal void LeaveCombatPosition()
        {
            if (!IsMoving && Identity && (!CurrentRoom || !CurrentRoom.ContainsFeet(transform.position)))
                ResolveInitialRoom();
            if (!IsMoving && !IsMovementLocked && Identity && CurrentRoom &&
                CurrentRoom.TryGetWorkstation(Identity.Definition, out var station) &&
                !station.IsAt(transform.position.x))
                MoveTo(CurrentRoom);
        }

        internal void ApplyMovementPosition(Vector2 point, float dt)
        {
            var previous = (Vector2)transform.position;
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            Velocity = dt > 0 ? (point - previous) / dt : Vector2.zero;
            if (Mathf.Abs(Velocity.x) > 0.01f) facing = Mathf.Sign(Velocity.x);
            SyncPassengerPosition();
            Animate(dt);
        }
        public void FinishRide(ShelterElevator lift)
        {
            if (CurrentElevator != lift) return;
            SetRideDepth(false);
            CurrentElevator = null; RequestedElevator = null; waypoint = rideIndex + 1; rideIndex = -1;
            Velocity = Vector2.zero;
            if (stopAfterRide) { stopAfterRide = false; Stop(); ResolveInitialRoom(); return; }
            if (afterRideDestination)
            {
                var next = afterRideDestination; afterRideDestination = null;
                Destination = null; // The remaining route still belongs to the previous order.
                if (!MoveTo(next, destinationOffset)) Stop();
                return;
            }
            if (!IsMoving) Arrive(); else { Status = "MOVING"; ReserveNextRide(); }
        }
        public void CancelRide(ShelterElevator lift)
        {
            if (CurrentElevator == lift) { SetRideDepth(false); CurrentElevator = null; route = null; afterRideDestination = null; stopAfterRide = false; }
            if (RequestedElevator == lift) RequestedElevator = null;
            rideIndex = -1; Velocity = Vector2.zero;
            Status = IsMoving ? "엘리베이터 호출 대기" : "STOPPED";
        }
        void OnDisable()
        {
            LeaveTent();
            if (CurrentElevator) CurrentElevator.Cancel(this, true);
            ResetRescue();
            ReleaseRequest(); route = null;
            CurrentRoom = null;
        }
        void Arrive()
        {
            CurrentRoom = Destination && Destination.kind == ShelterArea.AreaKind.Room ? Destination : null;
            Status = "ARRIVED";
            if (!rescueTarget)
            {
                if (reservedTent) EnterTent();
                return;
            }
            if (carrying)
            {
                bool delivered = CurrentRoom == rescueBed && rescueBed && rescueBed.IsOperational && rescueTarget.reservedTent;
                ReleaseRescueTarget(delivered);
                if (delivered)
                {
                    // Step clear of the opening after handing the patient over.
                    var room = CurrentRoom;
                    CurrentRoom = null;
                    if (!MoveTo(room)) ResolveInitialRoom();
                }
                return;
            }
            if (!rescueTarget.NeedsRescue || !rescueBed || !rescueBed.IsOperational ||
                !rescueTarget.isActiveAndEnabled || Vector2.Distance(transform.position, rescueTarget.transform.position) > 0.55f)
            {
                Stop();
                return;
            }
            carrying = true;
            rescueTarget.CurrentRoom = null;
            SyncPassengerPosition();
            if (!MoveTo(rescueBed, patientOffset)) Stop();
        }

        // The patient's reservation spans both trips so a second rescuer cannot take the same patient.
        public bool TryRescue(ShelterWalker target)
        {
            RoomOrderFailure = null;
            if (!target || target == this || IsMovementLocked || rescueTarget || target.rescuedBy ||
                !target.isActiveAndEnabled || !target.NeedsRescue || target.IsInElevator) return false;
            FindNavigation();
            if (!navigation) return false;
            navigation.Refresh();
            var pickup = navigation.AreaAt(target.transform.position);
            if (!pickup) return false;
            foreach (var bed in navigation.Areas)
            {
                if (bed.kind != ShelterArea.AreaKind.Room || bed.roomType != ShelterRoomType.Sleeping || !bed.IsOperational) continue;
                var tent = System.Array.Find(bed.GetComponentsInChildren<ShelterTent>(), t => t.AvailableTo(target, Identity.Rules));
                if (!tent)
                {
                    RoomOrderFailure = "빈 텐트가 없습니다";
                    continue;
                }
                float bedOffset = tent.Offset;
                if (!navigation.TryFindElevatorPath(target.transform.position, bed, bedOffset, walkSpeed, out _)) continue;
                if (!MoveTo(pickup, target.transform.position.x - pickup.Centre.x)) return false;
                LeaveTent();
                target.ReserveTent(tent);
                rescueTarget = target;
                rescueBed = bed;
                patientOffset = bedOffset;
                target.rescuedBy = this;
                RoomOrderFailure = null;
                return true;
            }
            return false;
        }

        internal void OnFainted()
        {
            // If already riding, the existing elevator takes the patient to its landing before dropping anyone.
            Stop();
            if (!IsInElevator) ResolveInitialRoom();
        }

        internal void ResetRescue()
        {
            if (rescuedBy)
            {
                var carrier = rescuedBy;
                if (carrier.CurrentElevator) carrier.CurrentElevator.Cancel(carrier, true);
                carrier.Stop();
            }
            if (!rescueTarget) return;
            if (CurrentElevator) CurrentElevator.Cancel(this, true);
            Stop();
        }

        private void SyncPassengerPosition()
        {
            if (carrying && rescueTarget)
                rescueTarget.transform.position = transform.position + new Vector3(-0.32f * facing, 0.65f, -0.02f);
        }

        private void ReleaseRescueTarget(bool delivered)
        {
            var target = rescueTarget;
            if (target)
            {
                target.rescuedBy = null;
                if (carrying)
                {
                    target.SetRideDepth(false);
                    Vector3 drop = transform.position;
                    if (delivered)
                        drop.x = rescueBed.ArrivalPoint(patientOffset).x;
                    target.transform.position = drop;
                    target.ResolveInitialRoom();
                    if (delivered)
                    {
                        target.CurrentRoom = rescueBed;
                        target.Identity.BeginBedRest();
                        target.EnterTent();
                    }
                }
                if (!delivered) target.LeaveTent();
            }
            carrying = false;
            rescueTarget = null;
            rescueBed = null;
        }

        public void Tick(float deltaTime, System.Func<Vector2, Vector2, float> movementFraction = null)
        {
            AdvanceRoute(deltaTime, movementFraction);
            SyncPassengerPosition();
        }

        private void AdvanceRoute(float deltaTime, System.Func<Vector2, Vector2, float> movementFraction)
        {
            Velocity = Vector2.zero;
            if (IsInElevator) return; // The single vehicle update carries its passenger; never simulate it per requester.
            if (rescueTarget && !IsMoving) Stop();
            if (deltaTime > 0f && !IsMoving && !externallyDriven && CanDoDevelopmentWork &&
                TryGetAssignedWorkstation(out var station) && !station.IsAt(transform.position.x))
            {
                var desk = CurrentRoom.ArrivalPoint(station.feet.position.x - CurrentRoom.Centre.x);
                ApplyMovementPosition(Vector2.MoveTowards(transform.position, desk, walkSpeed * deltaTime), deltaTime);
                return;
            }
            if (deltaTime <= 0f || !IsMoving || IsMovementLocked) return;
            if (navigation == null || Destination == null || !Destination.IsWalkable)
            {
                Stop(); Status = "NO ROUTE"; return;
            }
            if (routeVersion != navigation.Version && !MoveTo(Destination, destinationOffset))
            {
                Stop(); Status = "NO ROUTE"; return;
            }
            Vector3 previous = transform.position;
            ReserveNextRide();
            if (route == null) return;
            if (RequestedElevator && rideIndex >= waypoint)
            {
                Vector2 target = RequestedElevator.WaitingPoint(this);
                Vector2 proposed = Vector2.MoveTowards(transform.position, target, walkSpeed * deltaTime);
                float allowed = movementFraction == null ? 1f : Mathf.Clamp01(movementFraction(transform.position, proposed));
                Vector2 limited = Vector2.Lerp(transform.position, proposed, allowed);
                transform.position = new Vector3(limited.x, limited.y, previous.z);
                Velocity = (transform.position - previous) / deltaTime;
                if (Mathf.Abs(Velocity.x) > 0.01f) facing = Mathf.Sign(Velocity.x);
                Status = RequestedElevator.Owner == this ? "호출 주도권 · 엘리베이터 대기" : "엘리베이터 차례 대기";
                RequestedElevator.TryBoard(this);
                return;
            }
            float remainingTime = deltaTime;
            while (IsMoving && remainingTime > 0f)
            {
                var target = route[waypoint];
                float speed = Mathf.Max(0.1f, walkSpeed);
                float distance = Vector2.Distance(transform.position, target.position);
                Vector2 proposed = Vector2.MoveTowards(transform.position, target.position, speed * remainingTime);
                float allowed = movementFraction == null ? 1f : Mathf.Clamp01(movementFraction(transform.position, proposed));
                if (allowed < 1f)
                {
                    Vector2 limited = Vector2.Lerp(transform.position, proposed, allowed);
                    transform.position = new Vector3(limited.x, limited.y, previous.z);
                    break; // Keep the current waypoint; resume the same route when traffic clears.
                }
                if (distance <= speed * remainingTime + 0.00001f)
                {
                    transform.position = new Vector3(target.position.x, target.position.y, previous.z);
                    remainingTime -= distance / speed;
                    waypoint++;
                }
                else
                {
                    Vector2 next = Vector2.MoveTowards(transform.position, target.position, speed * remainingTime);
                    transform.position = new Vector3(next.x, next.y, previous.z);
                    remainingTime = 0f;
                }
            }
            if (!IsMoving) Arrive();
            Velocity = (transform.position - previous) / deltaTime;
            if (Mathf.Abs(Velocity.x) > 0.01f) facing = Mathf.Sign(Velocity.x);
        }

        public void ResolveInitialRoom()
        {
            if (IsMoving || IsInElevator) return;
            CurrentRoom = null;
            foreach (var area in FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID))
            {
                if (area.gameObject.scene != gameObject.scene || area.kind != ShelterArea.AreaKind.Room ||
                    !area.IsWalkable || !area.ContainsFeet(transform.position)) continue;
                CurrentRoom = area;
                break;
            }
        }

        public bool CanWorkInCurrentRoom => isActiveAndEnabled && !IsMoving && !IsInElevator && !IsMovementLocked && CurrentRoom != null &&
            CurrentRoom.kind == ShelterArea.AreaKind.Room && CurrentRoom.IsOperational &&
            CurrentRoom.gameObject.scene == gameObject.scene && CurrentRoom.ContainsFeet(transform.position);

        public void Animate(float deltaTime)
        {
            if (!externallyDriven || !visual || !visual.gameObject.activeSelf) return;
            float speed = IsInElevator ? 0f : Mathf.Abs(Velocity.x);
            phase += deltaTime * speed * 4.5f;
            float swing = speed > 0.01f ? Mathf.Sin(phase) : 0f;
            if (visual != null)
            {
                visual.localScale = new Vector3(facing, 1f, 1f);
                visual.localPosition = new Vector3(0f, Mathf.Abs(swing) * 0.025f, 0f);
            }
            Pose(leftLeg, swing * 25f); Pose(rightLeg, -swing * 25f);
            Pose(leftArm, -swing * 20f - 6f); Pose(rightArm, swing * 20f + 6f);
        }

        private static void Pose(Transform joint, float angle)
        {
            if (joint != null) joint.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
