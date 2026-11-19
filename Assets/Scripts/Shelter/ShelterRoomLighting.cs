using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterRoomLighting : MonoBehaviour
    {
        [SerializeField] SpriteRenderer fixtures;
        [SerializeField] Sprite lightsOn, lightsOff;
        [SerializeField] Light2D[] bulbs;
        [SerializeField, Min(1f)] float averageFlickerInterval = 45f;
        ShelterArea room;
        System.Random random;
        float remaining;
        bool lit;

        void Awake()
        {
            room = GetComponentInParent<ShelterArea>();
            random = new System.Random(unchecked(System.Environment.TickCount + GetInstanceID()));
        }

        void OnEnable()
        {
            room.OccupationChanged += SetOccupied;
            SetOccupied(room.IsOccupied);
        }

        void OnDisable() => room.OccupationChanged -= SetOccupied;

        void SetOccupied(bool occupied)
        {
            SetLit(!occupied);
            if (!occupied) ScheduleFlicker();
        }

        void ScheduleFlicker()
        {
            // Keep a short quiet period, then draw an independent wait for this room.
            remaining = averageFlickerInterval * (.15f - .85f * (float)System.Math.Log(1d - random.NextDouble()));
        }

        void Update()
        {
            if (room.IsOccupied) return;
            remaining -= Time.deltaTime;
            if (remaining > 0f) return;
            SetLit(!lit);
            if (lit) ScheduleFlicker();
            else remaining = .08f + .08f * (float)random.NextDouble();
        }

        void SetLit(bool value)
        {
            lit = value;
            fixtures.sprite = lit ? lightsOn : lightsOff;
            foreach (var bulb in bulbs) bulb.enabled = lit;
        }
    }
}
