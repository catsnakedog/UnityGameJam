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
        public bool IsMovementLocked => movementLocks.Count > 0 || ((identity = identity ? identity : GetComponent<UnityGameJam.Characters.CharacterIdentity>()) != null && !identity.CanAct);
        public void AcquireMovementLock(object owner) { if (owner != null) movementLocks.Add(owner); }
        public void ReleaseMovementLock(object owner) { if (owner != null) movementLocks.Remove(owner); }
        UnityGameJam.Characters.CharacterIdentity identity;

        private void Awake() => identity = GetComponent<UnityGameJam.Characters.CharacterIdentity>();

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
        public ShelterArea CurrentRoom { get; private set; }
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

        private float facing = 1f;
        internal bool FacingLeft => facing < 0;
        internal bool AnimationPaused => simulation && simulation.IsPaused;
        internal bool CanAnimateDevelopment => CanWorkInCurrentRoom && simulation && simulation.CanDevelopIn(CurrentRoom.roomType);
        private float phase;
        private BunkerSimulation simulation;

        private void Start()
        {
            foreach(var candidate in FindObjectsByType<BunkerSimulation>(FindObjectsSortMode.None))
                if(candidate.gameObject.scene==gameObject.scene) { simulation=candidate; break; }
            FindNavigation();
            if (nameLabel != null) nameLabel.text = characterName;
            if (identity) identity.RefreshStatusLabel();
            ResolveInitialRoom();
            SetSelected(false);
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
            var p = transform.InverseTransformPoint(point);
            return Mathf.Abs(p.x) <= 0.48f && p.y >= -0.08f && p.y <= 2.15f;
        }

        public bool MoveTo(ShelterArea room)
        {
            if (IsMovementLocked) return false;
            FindNavigation();
            if (!IsInElevator && Destination == room && IsMoving && navigation && routeVersion == navigation.Version) return true;
            Vector2 from = IsInElevator && rideIndex >= 0 ? route[rideIndex].position : (Vector2)transform.position;
            if (navigation == null || !navigation.TryFindElevatorPath(from, room, arrivalOffset, walkSpeed, out var nextRoute)) return false;
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
            if (IsInElevator) { stopAfterRide = true; afterRideDestination = null; Status = "도착층에서 정지"; return; }
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
        public void BeginRide(ShelterElevator lift) { CurrentElevator = lift; CurrentRoom = null; Status = "엘리베이터 탑승"; }
        public void ApplyRidePosition(Vector2 point, float dt) => ApplyMovementPosition(point, dt);
        internal void ApplyMovementPosition(Vector2 point, float dt)
        {
            var previous = (Vector2)transform.position;
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            Velocity = dt > 0 ? (point - previous) / dt : Vector2.zero;
            if (Mathf.Abs(Velocity.x) > 0.01f) facing = Mathf.Sign(Velocity.x);
            Animate(dt);
        }
        public void FinishRide(ShelterElevator lift)
        {
            if (CurrentElevator != lift) return;
            CurrentElevator = null; RequestedElevator = null; waypoint = rideIndex + 1; rideIndex = -1;
            Velocity = Vector2.zero;
            if (stopAfterRide) { stopAfterRide = false; Stop(); ResolveInitialRoom(); return; }
            if (afterRideDestination)
            {
                var next = afterRideDestination; afterRideDestination = null;
                Destination = null; // The remaining route still belongs to the previous order.
                if (!MoveTo(next)) Stop();
                return;
            }
            if (!IsMoving) Arrive(); else { Status = "MOVING"; ReserveNextRide(); }
        }
        public void CancelRide(ShelterElevator lift)
        {
            if (CurrentElevator == lift) { CurrentElevator = null; route = null; afterRideDestination = null; stopAfterRide = false; }
            if (RequestedElevator == lift) RequestedElevator = null;
            rideIndex = -1; Velocity = Vector2.zero;
            Status = IsMoving ? "엘리베이터 호출 대기" : "STOPPED";
        }
        void OnDisable()
        {
            if (CurrentElevator) CurrentElevator.Cancel(this, true);
            ReleaseRequest(); route = null;
        }
        void Arrive()
        { CurrentRoom = Destination && Destination.kind == ShelterArea.AreaKind.Room ? Destination : null; Status = "ARRIVED"; }

        public void Tick(float deltaTime, System.Func<Vector2, Vector2, float> movementFraction = null)
        {
            Velocity = Vector2.zero;
            if (IsInElevator) return; // The single vehicle update carries its passenger; never simulate it per requester.
            if (deltaTime <= 0f || !IsMoving || IsMovementLocked) return;
            if (navigation == null || Destination == null || !Destination.IsWalkable)
            {
                Stop(); Status = "NO ROUTE"; return;
            }
            if (routeVersion != navigation.Version && !MoveTo(Destination))
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
                if (area.gameObject.scene == gameObject.scene && area.kind == ShelterArea.AreaKind.Room && area.IsWalkable &&
                    area.ContainsFeet(transform.position))
                { CurrentRoom = area; break; }
        }

        public bool CanWorkInCurrentRoom => isActiveAndEnabled && !IsMoving && !IsInElevator && !IsMovementLocked && CurrentRoom != null &&
            CurrentRoom.kind == ShelterArea.AreaKind.Room && CurrentRoom.IsOperational &&
            CurrentRoom.gameObject.scene == gameObject.scene && CurrentRoom.ContainsFeet(transform.position);

        public void Animate(float deltaTime)
        {
            // Survivors use authored sprites. The joint rig is still used by the zombie prefab.
            if (!externallyDriven) return;
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
