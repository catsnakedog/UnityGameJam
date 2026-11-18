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
        [Min(0.1f)] public float climbSpeed = 2.2f;

        [Header("Placeholder character")]
        public Transform visual;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        public ShelterArea Destination { get; private set; }
        public ShelterArea CurrentRoom { get; private set; }
        public ShelterLadder CurrentLadder { get; private set; }
        public bool IsClimbing => CurrentLadder != null;
        public Vector2 Velocity { get; private set; }
        public bool IsMoving => route != null && waypoint < route.Count;
        public string Status { get; private set; } = "IDLE";
        private List<ShelterNavigation.Waypoint> route;
        private int waypoint;
        private int routeVersion;

        private float facing = 1f;
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
            if (navigation == null || !navigation.TryFindPath(transform.position, CurrentLadder, room,
                arrivalOffset, walkSpeed, climbSpeed, out var nextRoute)) return false;
            Destination = room;
            CurrentRoom = null;
            route = nextRoute;
            waypoint = 0;
            routeVersion = navigation.Version;
            Status = "MOVING";
            return true;
        }

        public void Stop()
        {
            route = null;
            Destination = null;
            Velocity = Vector2.zero;
            // Keep ladder context when stopped halfway up so another order can reverse safely.
            Status = "STOPPED";
        }

        public void Tick(float deltaTime)
        {
            Velocity = Vector2.zero;
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
            float remainingTime = deltaTime;
            while (IsMoving && remainingTime > 0f)
            {
                var target = route[waypoint];
                CurrentLadder = target.ladder;
                float speed = Mathf.Max(0.1f, IsClimbing ? climbSpeed : walkSpeed);
                float distance = Vector2.Distance(transform.position, target.position);
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
            if (!IsMoving) { CurrentLadder = null; CurrentRoom = Destination.kind == ShelterArea.AreaKind.Room ? Destination : null; Status = "ARRIVED"; }
            Velocity = (transform.position - previous) / deltaTime;
            if (!IsClimbing && Mathf.Abs(Velocity.x) > 0.01f) facing = Mathf.Sign(Velocity.x);
        }

        public void ResolveInitialRoom()
        {
            if (IsMoving || IsClimbing) return;
            CurrentRoom = null;
            foreach (var area in FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID))
                if (area.gameObject.scene == gameObject.scene && area.kind == ShelterArea.AreaKind.Room && area.IsWalkable &&
                    Mathf.Abs(transform.position.y - area.FloorY) < 0.05f && area.ContainsClick(transform.position))
                { CurrentRoom = area; break; }
        }

        public bool CanWorkInCurrentRoom => isActiveAndEnabled && !IsMoving && !IsClimbing && !IsMovementLocked && CurrentRoom != null &&
            CurrentRoom.kind == ShelterArea.AreaKind.Room && CurrentRoom.IsOperational &&
            CurrentRoom.gameObject.scene == gameObject.scene && Mathf.Abs(transform.position.y - CurrentRoom.FloorY) < 0.05f &&
            CurrentRoom.ContainsClick(transform.position);

        public void Animate(float deltaTime)
        {
            float speed = IsClimbing ? Mathf.Abs(Velocity.y) : Mathf.Abs(Velocity.x);
            phase += deltaTime * speed * 4.5f;
            float swing = speed > 0.01f ? Mathf.Sin(phase) : 0f;
            if (visual != null)
            {
                visual.localScale = new Vector3(IsClimbing ? 1f : facing, 1f, 1f);
                visual.localPosition = new Vector3(0f, Mathf.Abs(swing) * 0.025f, 0f);
            }
            Pose(leftLeg, swing * (IsClimbing ? 22f : 25f));
            Pose(rightLeg, -swing * (IsClimbing ? 22f : 25f));
            Pose(leftArm, IsClimbing ? 145f + swing * 25f : -swing * 20f - 6f);
            Pose(rightArm, IsClimbing ? -145f + swing * 25f : swing * 20f + 6f);
        }

        private static void Pose(Transform joint, float angle)
        {
            if (joint != null) joint.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
