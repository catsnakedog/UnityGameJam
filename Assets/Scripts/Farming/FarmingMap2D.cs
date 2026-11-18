using UnityEngine;
using UnityEngine.Tilemaps;

namespace ShelterRun
{
    /// <summary>XY world; logical floor coordinates use the reference tiles' 2:1 projection.</summary>
    [DefaultExecutionOrder(-80)]
    public sealed class FarmingMap2D : MonoBehaviour
    {
        [SerializeField] Tilemap floor;
        [SerializeField] FarmingRoom2D[] rooms;
        [SerializeField] FarmingPlayer player;
        [SerializeField] FarmingSession session;
        public static FarmingMap2D Instance { get; private set; }
        public Tilemap Floor => floor;
        public FarmingRoom2D[] Rooms => rooms;
        public FarmingRoom2D ActiveRoom { get; private set; }
        // Corridor +v rises to screen-right; rooms at -u sit on its upper-left side.
        public static Vector3 Project(Vector2 point) => new Vector3((point.x + point.y) * .5f, (point.y - point.x) * .25f, 0);
        public static Vector2 Unproject(Vector3 point) => new Vector2(point.x - point.y * 2, point.x + point.y * 2);
        void Awake() { Instance = this; foreach (var room in rooms) room.Apply(0); }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public bool IsFloor(Vector3 point) => floor.HasTile(floor.WorldToCell(point));
        public FarmingRoom2D RoomAt(Vector3 point)
        {
            var logical = Unproject(point);
            foreach (var room in rooms) if (room.Footprint.Contains(logical)) return room;
            return null;
        }
        public bool CanReach(Vector3 from, Vector3 to)
        {
            if (RoomAt(from) != RoomAt(to)) return false;
            foreach (var hit in Physics2D.LinecastAll(from, to))
            {
                var obstacle = hit.collider.GetComponent<FarmingObstacle2D>();
                if (obstacle && obstacle.BlocksInteraction) return false;
            }
            return true;
        }
        void Update()
        {
            ActiveRoom = RoomAt(player.transform.position);
            if (session.Rules == null) return;
            foreach (var room in rooms)
                room.Apply(Mathf.MoveTowards(room.Reveal, room == ActiveRoom ? 1 : 0, Time.deltaTime / session.Rules.RoomRevealDuration));
        }
    }
}
