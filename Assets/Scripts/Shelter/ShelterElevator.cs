using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public enum ElevatorPhase { Idle, Calling, Ready, Boarding, Travelling, Unloading }

    [ExecuteAlways, DefaultExecutionOrder(-100)]
    public sealed class ShelterElevator : MonoBehaviour
    {
        [Min(0.2f)] public float height = 7.5f;
        public bool accessible = true;
        public int initialStop;
        public Transform shaftVisual;
        public Transform cabin;
        public TextMesh statusLabel;
        public ElevatorRules Rules { get; private set; }
        public Vector2 Bottom => transform.position;
        public Vector2 Top => transform.TransformPoint(new Vector3(0, height));
        public bool IsUsable => isActiveAndEnabled && accessible && Rules && Top.y > Bottom.y && Mathf.Abs(Top.x - Bottom.x) < 0.01f && stops.Count >= 2;
        public IReadOnlyList<float> Stops => stops;
        public ElevatorPhase Phase { get; private set; }
        public ShelterWalker Owner => Passenger ? Passenger : waiting.Count > 0 ? waiting[0].actor : null;
        public ShelterWalker Passenger => riders.Count > 0 ? riders[0].actor : null;
        public int PassengerCount => riders.Count;
        public float CabinY { get; private set; }
        public float TripDestination => riders.Count > 0 ? NextDropOff().to : waiting.Count > 0 ? waiting[0].to : CabinY;
        public int QueueCount => waiting.Count + riders.Count;
        readonly List<float> stops = new List<float>();
        readonly List<Request> waiting = new List<Request>();
        readonly List<Request> riders = new List<Request>(2);
        BunkerSimulation simulation;
        ShelterNavigation navigation;
        bool initialized;
        sealed class Request
        {
            public ShelterWalker actor;
            public float from, to;
            public int side;
            public float seatOffset;
            public Vector2? exit;
        }

        void OnEnable()
        {
            if (Application.isPlaying)
                foreach (var system in FindObjectsByType<ShelterTransit>(FindObjectsSortMode.None))
                    if (system.gameObject.scene == gameObject.scene) system.Register(this);
            RefreshVisuals();
        }
        void OnValidate() { height = Mathf.Max(0.2f, height); RefreshVisuals(); }
        void OnDisable() => ResetService();
        public void Configure(ElevatorRules rules, BunkerSimulation clock, ShelterNavigation graph)
        {
            Rules = rules; simulation = clock; navigation = graph;
            RefreshStops(graph.Areas);
            if (!initialized && stops.Count > 0) { CabinY = stops[Mathf.Clamp(initialStop, 0, stops.Count - 1)]; initialized = true; }
            RefreshVisuals();
        }
        public void RefreshStops(IEnumerable<ShelterArea> areas)
        {
            stops.Clear();
            foreach (var area in areas.OrderBy(a => a.FloorY))
                if (area.IsWalkable && area.FloorY >= Bottom.y - 0.05f && area.FloorY <= Top.y + 0.05f &&
                    Bottom.x >= area.Left - 0.05f && Bottom.x <= area.Right + 0.05f && !stops.Any(y => Mathf.Abs(y - area.FloorY) < 0.05f)) stops.Add(area.FloorY);
        }
        public Vector2 CallPoint(float floor, int side) => new Vector2(Bottom.x + side * Rules.BoardingOffset, floor);
        public Vector2 ExitPoint(float floor, int boardingSide) => new Vector2(Bottom.x - boardingSide * Rules.ExitOffset, floor);
        Request FindRequest(ShelterWalker actor) => waiting.Find(r => r.actor == actor) ?? riders.Find(r => r.actor == actor);
        public bool HasRequest(ShelterWalker actor) => FindRequest(actor) != null;
        public bool RequestRide(ShelterWalker actor, float from, float to, int side)
        {
            if (!IsUsable || !actor || Mathf.Abs(from - to) < 0.05f || !HasStop(from) || !HasStop(to)) return false;
            var existing = FindRequest(actor);
            if (existing != null) return Mathf.Abs(existing.from - from) < 0.05f && Mathf.Abs(existing.to - to) < 0.05f && existing.side == side;
            waiting.Add(new Request { actor = actor, from = from, to = to, side = side });
            DispatchEmptyCabin(); RefreshVisuals(); return true;
        }
        bool HasStop(float floor) => stops.Any(y => Mathf.Abs(floor - y) < 0.05f);
        public Vector2 WaitingPoint(ShelterWalker actor)
        {
            var request = waiting.Find(r => r.actor == actor);
            if (request == null) return actor.transform.position;
            int index = 0;
            foreach (var earlier in waiting)
            {
                if (earlier == request) break;
                if (Mathf.Abs(earlier.from - request.from) < 0.05f && earlier.side == request.side) index++;
            }
            Vector2 point = CallPoint(request.from, request.side) + Vector2.right * request.side * index * Rules.QueueSpacing;
            // Overflow callers retain their location instead of collapsing onto the final pad.
            return navigation.AreaAt(point) ? point : (Vector2)actor.transform.position;
        }
        public bool TryBoard(ShelterWalker actor)
        {
            if (Phase != ElevatorPhase.Ready || !IsUsable || !actor || actor != Owner ||
                !actor.isActiveAndEnabled || !actor.IsMoving || actor.IsMovementLocked) return false;
            var first = waiting[0];
            if (Vector2.Distance(actor.transform.position, CallPoint(first.from, first.side)) > 0.03f) return false;
            Phase = ElevatorPhase.Boarding;
            Board(first);
            BoardWaitingCompanion();
            RefreshVisuals();
            return true;
        }
        void Board(Request request)
        {
            waiting.Remove(request);
            riders.Add(request);
            request.actor.BeginRide(this);
        }
        void BoardWaitingCompanion()
        {
            if (riders.Count != 1) return;
            foreach (var request in waiting)
            {
                var actor = request.actor;
                if (!actor || !actor.isActiveAndEnabled || !actor.IsMoving || actor.IsMovementLocked ||
                    Mathf.Abs(request.from - CabinY) > 0.05f || Mathf.Abs(actor.transform.position.y - CabinY) > 0.03f) continue;
                // The next waiter can board directly from the second queue position.
                float distanceBehindPad = (actor.transform.position.x - CallPoint(request.from, request.side).x) * request.side;
                if (distanceBehindPad < -0.03f || distanceBehindPad > Rules.QueueSpacing + 0.03f) continue;
                request.seatOffset = request.side * 0.5f;
                riders[0].seatOffset = -request.seatOffset;
                Board(request);
                return;
            }
        }
        Request NextDropOff()
        {
            var first = riders[0];
            if (riders.Count == 2 && riders[1].to > Mathf.Min(first.from, first.to) &&
                riders[1].to < Mathf.Max(first.from, first.to)) return riders[1];
            return first;
        }
        public void Cancel(ShelterWalker actor, bool force = false)
        {
            var request = FindRequest(actor);
            if (request == null) return;
            if (riders.Contains(request) && !force) return;
            RemoveRequest(request);
            RefreshVisuals();
        }
        void RemoveRequest(Request request)
        {
            if (riders.Remove(request))
            {
                if (request.actor)
                {
                    float floor = stops.Count == 0 ? Bottom.y : stops.OrderBy(y => Mathf.Abs(y - CabinY)).First();
                    request.actor.ApplyRidePosition(ExitPoint(floor, request.side), 0);
                }
            }
            else waiting.Remove(request);
            if (request.actor) request.actor.CancelRide(this);
            if (riders.Count > 0 && Phase == ElevatorPhase.Unloading && Mathf.Abs(TripDestination - CabinY) > 0.001f)
                Phase = ElevatorPhase.Travelling;
            DispatchEmptyCabin();
        }
        void DispatchEmptyCabin()
        {
            if (riders.Count > 0) return;
            if (waiting.Count == 0 || !IsUsable) Phase = ElevatorPhase.Idle;
            else Phase = Mathf.Abs(CabinY - waiting[0].from) < 0.001f ? ElevatorPhase.Ready : ElevatorPhase.Calling;
        }
        void Update()
        {
            if (Application.isPlaying) Advance(Time.deltaTime);
        }
        public void Advance(float dt)
        {
            if (dt <= 0 || !Rules || simulation && (!simulation.IsReady || simulation.IsPaused)) return;
            if (!IsUsable) { if (QueueCount > 0) ResetService(); return; }
            for (int i = waiting.Count - 1; i >= 0; i--)
            {
                var request = waiting[i];
                if (!ValidRequest(request) || request.actor.IsMovementLocked || !request.actor.IsMoving) RemoveRequest(request);
            }
            for (int i = riders.Count - 1; i >= 0; i--)
                if (!ValidRequest(riders[i])) RemoveRequest(riders[i]);
            switch (Phase)
            {
                case ElevatorPhase.Calling:
                    CabinY = Mathf.MoveTowards(CabinY, waiting[0].from, Rules.TravelSpeed * dt);
                    if (Mathf.Abs(CabinY - waiting[0].from) < 0.001f) Phase = ElevatorPhase.Ready;
                    break;
                case ElevatorPhase.Ready: TryBoard(Owner); break;
                case ElevatorPhase.Boarding:
                    BoardWaitingCompanion();
                    bool seated = true;
                    foreach (var rider in riders) seated &= MovePassenger(rider, Seat(rider), dt);
                    if (seated) Phase = ElevatorPhase.Travelling;
                    break;
                case ElevatorPhase.Travelling:
                    float destination = TripDestination;
                    CabinY = Mathf.MoveTowards(CabinY, destination, Rules.TravelSpeed * dt);
                    foreach (var rider in riders) rider.actor.ApplyRidePosition(Seat(rider), dt);
                    if (Mathf.Abs(CabinY - destination) < 0.001f) Phase = ElevatorPhase.Unloading;
                    break;
                case ElevatorPhase.Unloading:
                    var leaving = NextDropOff();
                    var planned = ExitPoint(leaving.to, leaving.side);
                    var findSpace = leaving.actor.FindLandingSpace;
                    leaving.exit = findSpace == null ? planned : findSpace(planned, leaving.exit ?? planned);
                    if (leaving.exit.HasValue && MovePassenger(leaving, leaving.exit.Value, dt))
                    {
                        riders.Remove(leaving);
                        if (riders.Count > 0 && Mathf.Abs(TripDestination - CabinY) > 0.001f) Phase = ElevatorPhase.Travelling;
                        DispatchEmptyCabin();
                        leaving.actor.FinishRide(this);
                    }
                    break;
            }
            RefreshVisuals();
        }
        bool ValidRequest(Request request) => request.actor && request.actor.isActiveAndEnabled && HasStop(request.from) && HasStop(request.to);
        Vector2 Seat(Request rider) => new Vector2(Bottom.x + rider.seatOffset, CabinY);
        static bool MovePassenger(Request request, Vector2 to, float dt)
        {
            var actor = request.actor;
            var next = Vector2.MoveTowards(actor.transform.position, to, actor.walkSpeed * dt);
            actor.ApplyRidePosition(next, dt);
            return Vector2.Distance(next, to) < 0.001f;
        }
        public void ResetService()
        {
            foreach (var request in riders.Concat(waiting).ToArray())
            {
                RemoveRequest(request);
                if (request.actor) request.actor.Stop();
            }
            Phase = ElevatorPhase.Idle;
            if (stops.Count > 0) CabinY = stops[Mathf.Clamp(initialStop, 0, stops.Count - 1)];
            RefreshVisuals();
        }
        public void RefreshVisuals()
        {
            if (shaftVisual) { shaftVisual.localPosition = new Vector3(0, height * 0.5f, 0); shaftVisual.localScale = new Vector3(1, height, 1); }
            if (cabin) cabin.position = new Vector3(Bottom.x, Application.isPlaying && initialized ? CabinY : Bottom.y, transform.position.z);
            if (statusLabel)
            {
                if (!Application.isPlaying) statusLabel.text = "ELEVATOR";
                else if (riders.Count == 2) statusLabel.text = riders[0].actor.characterName + " · " + riders[1].actor.characterName + " 탑승 · 대기 " + waiting.Count;
                else statusLabel.text = Owner ? Owner.characterName + " 이용 중 · 대기 " + (QueueCount - 1) : "호출 대기";
                statusLabel.transform.position = new Vector3(Bottom.x, (cabin ? cabin.position.y : Bottom.y) + 2.8f, -0.1f);
            }
        }
        void OnDrawGizmosSelected()
        { Gizmos.color = Color.cyan; Gizmos.DrawLine(Bottom, Top); foreach (float y in stops) Gizmos.DrawWireSphere(new Vector3(Bottom.x, y), 0.2f); }
    }
}
