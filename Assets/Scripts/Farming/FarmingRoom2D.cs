using TMPro;
using UnityEngine;
namespace ShelterRun
{
    public sealed class FarmingRoom2D : MonoBehaviour
    {
        [SerializeField] Rect footprint;
        [SerializeField] Transform walkLeft;
        [SerializeField] Transform walkRight;
        [SerializeField] Transform cameraAnchor;
        [SerializeField] Transform interior;
        SpriteRenderer[] furniture;
        TMP_Text[] labels;
        public Rect Footprint => footprint;
        public float Left => walkLeft.position.x;
        public float Right => walkRight.position.x;
        public float GroundY => walkLeft.position.y;
        public float CameraY => cameraAnchor.position.y;
        public float Reveal { get; private set; }
        public Vector3 Constrain(Vector3 point, float margin = 0)
            => new Vector3(Mathf.Clamp(point.x, Left + margin, Right - margin), GroundY, 0);
        public void Apply(float value)
        {
            Reveal = Mathf.Clamp01(value);
            if (!interior) return;
            if (furniture == null) furniture = interior.GetComponentsInChildren<SpriteRenderer>(true);
            if (labels == null) labels = interior.GetComponentsInChildren<TMP_Text>(true);
            // Carried objects leave this hierarchy. Their visibility belongs to the carrier.
            foreach (var sprite in furniture)
            {
                if (!sprite || !sprite.transform.IsChildOf(interior) || sprite.GetComponentInParent<FarmingPickup>()) continue;
                var color = sprite.color; color.a = Reveal; sprite.color = color;
            }
            foreach (var label in labels)
            {
                if (!label || !label.transform.IsChildOf(interior) || label.GetComponentInParent<FarmingPickup>()) continue;
                var color = label.color; color.a = Reveal; label.color = color;
            }
        }
    }
}
