using UnityEngine;

namespace UnityGameJam.Shelter
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ShelterTent : MonoBehaviour
    {
        [SerializeField] Sprite openSprite;
        [SerializeField] Sprite closedSprite;
        public ShelterArea Room => GetComponentInParent<ShelterArea>();
        public float Offset => transform.position.x - Room.Centre.x;
        public ShelterWalker Guest { get; private set; }
        public bool Occupied { get; private set; }

        public bool Contains(Vector2 point)
        {
            var bounds = GetComponent<SpriteRenderer>().bounds;
            return bounds.Contains(new Vector3(point.x, point.y, bounds.center.z));
        }

        internal bool AvailableTo(ShelterWalker walker, BunkerRules rules)
        {
            if (!isActiveAndEnabled || !rules || !Room.IsOperational || (Guest && Guest != walker)) return false;
            int otherGuests = 0;
            foreach (var tent in Room.GetComponentsInChildren<ShelterTent>())
                if (tent != this && tent.Guest && tent.Guest != walker) otherGuests++;
            return otherGuests < rules.SleepingRoomCapacity;
        }

        internal void Reserve(ShelterWalker guest) => Guest = guest;

        internal void Enter()
        {
            Occupied = true;
            GetComponent<SpriteRenderer>().sprite = closedSprite;
        }

        internal void Release()
        {
            Guest = null;
            Occupied = false;
            GetComponent<SpriteRenderer>().sprite = openSprite;
        }

        void OnDisable()
        {
            if (Guest) Guest.LeaveTent();
        }
    }
}
