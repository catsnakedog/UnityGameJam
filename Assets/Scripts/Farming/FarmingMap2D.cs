using UnityEngine;
namespace ShelterRun
{
    [DefaultExecutionOrder(-80)]
    public sealed class FarmingMap2D : MonoBehaviour
    {
        [SerializeField] FarmingRoom2D corridor;
        [SerializeField] FarmingRoom2D[] rooms;
        [SerializeField] FarmingDoor2D[] doors;
        [SerializeField] FarmingPlayer player;
        [SerializeField] FarmingSession session;
        public static FarmingMap2D Instance { get; private set; }
        public FarmingRoom2D Corridor => corridor;
        public FarmingRoom2D[] Rooms => rooms;
        public FarmingDoor2D[] Doors => doors;
        public FarmingRoom2D ActiveArea { get; private set; }
        public FarmingRoom2D ActiveRoom => ActiveArea == corridor ? null : ActiveArea;
        void Awake()
        {
            Instance = this; ActiveArea = corridor;
            corridor.Apply(1); foreach (var room in rooms) room.Apply(0);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public FarmingRoom2D AreaAt(Vector3 point)
        {
            if (corridor.Footprint.Contains(point)) return corridor;
            foreach (var room in rooms) if (room.Footprint.Contains(point)) return room;
            return null;
        }
        public FarmingRoom2D RoomAt(Vector3 point) { var area = AreaAt(point); return area == corridor ? null : area; }
        public bool IsFloor(Vector3 point)
        {
            var area = AreaAt(point);
            return area && point.x >= area.Left && point.x <= area.Right;
        }
        public float VisibilityAt(Vector3 point) { var area = AreaAt(point); return area ? area.Reveal : 0; }
        public bool CanReach(Vector3 from, Vector3 to) => AreaAt(from) == ActiveArea && AreaAt(to) == ActiveArea;
        public Vector3 Constrain(Vector3 point, float margin = 0)
            => ActiveArea.Constrain(point, margin);
        public FarmingDoor2D NearestDoor(Vector3 point, float radius)
        {
            FarmingDoor2D best = null; float distance = radius;
            foreach (var door in doors)
            {
                if (door.Area != ActiveArea || !door.Target) continue;
                float next = Mathf.Abs(point.x - door.transform.position.x);
                if (next <= distance) { best = door; distance = next; }
            }
            return best;
        }
        public bool Traverse(FarmingDoor2D door)
        {
            if (!session.CanAct || !door || !door.Target || door.Area != ActiveArea ||
                Mathf.Abs(player.transform.position.x - door.transform.position.x) > session.Rules.InteractionRadius) return false;
            ActiveArea.Apply(0);
            ActiveArea = door.Target.Area;
            player.Teleport(ActiveArea.Constrain(door.Target.Arrival));
            var camera = Camera.main ? Camera.main.GetComponent<FarmingCamera>() : null;
            if (camera) camera.Snap();
            session.Notify(ActiveArea == corridor ? "복도로 나왔습니다" : ActiveArea.name + " 입실");
            return true;
        }
        void Update()
        {
            if (session.Rules == null) return;
            corridor.Apply(Mathf.MoveTowards(corridor.Reveal, ActiveArea == corridor ? 1 : 0, Time.deltaTime / session.Rules.RoomRevealDuration));
            foreach (var room in rooms)
                room.Apply(Mathf.MoveTowards(room.Reveal, ActiveArea == room ? 1 : 0, Time.deltaTime / session.Rules.RoomRevealDuration));
        }
    }
}
