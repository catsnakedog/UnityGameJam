using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterSpeechAnchor : MonoBehaviour
    {
        [SerializeField] SpriteRenderer head;
        [SerializeField] Renderer nameplate;
        [SerializeField] RectTransform gauge;
        [Tooltip("World-space distance from the top-left of the visible head to the bubble.")]
        [SerializeField, Min(0)] float gap = 0.07f;

        internal static Vector3 DefaultPosition(Transform speaker) =>
            speaker.position + new Vector3(-0.35f, 1.8f, 0f);

        internal Vector3 Position(float height)
        {
            if (!head || !head.sprite) return DefaultPosition(transform);
            var bounds = head.bounds;
            var point = new Vector3(bounds.min.x - gap, bounds.max.y + gap, transform.position.z);
            if (gauge && gauge.gameObject.activeInHierarchy)
                ClearLabel(ref point, height, gauge.TransformPoint(gauge.rect.min), gauge.TransformPoint(gauge.rect.max));
            if (nameplate && nameplate.enabled && nameplate.gameObject.activeInHierarchy)
                ClearLabel(ref point, height, nameplate.bounds.min, nameplate.bounds.max);
            return point;
        }

        // The panel extends left and up from its tail. Take the shorter detour around each label.
        void ClearLabel(ref Vector3 point, float height, Vector2 min, Vector2 max)
        {
            if (point.x <= min.x - gap || point.y >= max.y + gap || point.y + height <= min.y - gap) return;
            float left = min.x - gap, down = min.y - gap - height;
            if (point.x - left < point.y - down) point.x = left;
            else point.y = down;
        }
    }
}
