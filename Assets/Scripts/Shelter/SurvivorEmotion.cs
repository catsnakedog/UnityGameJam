using UnityEngine;
using System.Collections.Generic;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class SurvivorEmotion : MonoBehaviour
    {
        public enum Cause { MentalChange, Work, GuardDuty, Rest, ItemUse, Combat, Breach, Victory, Presentation, Event }
        [SerializeField] SpriteRenderer icon;
        [SerializeField] SpriteRenderer characterSprite;
        [SerializeField] RectTransform mentalGauge;
        [SerializeField] Sprite anger;
        [SerializeField] Sprite gloom;
        [SerializeField] Sprite joy;
        [Tooltip("Offset of the mark's center from the top of the character, in sprite pixels. Positive X is screen right.")]
        [SerializeField] Vector2Int headOffsetPixels = new Vector2Int(3, -1);
        [SerializeField, Min(0.1f)] float duration = 1.35f;
        [SerializeField, Min(0f)] float repeatCooldown = 10f;

        float elapsed;
        readonly Dictionary<(Cause, Sprite), double> nextAllowed = new Dictionary<(Cause, Sprite), double>();

        void Awake() => Hide();

        public void ShowAnger() => Show(anger, Cause.Combat);
        public void ShowGloom(Cause cause = Cause.MentalChange) => Show(gloom, cause);
        public void ShowJoy(Cause cause = Cause.MentalChange) => Show(joy, cause);

        void Show(Sprite sprite, Cause cause)
        {
            if (!sprite || !isActiveAndEnabled || !characterSprite || !characterSprite.sprite || icon.forceRenderingOff) return;
            var key = (cause, sprite);
            double now = Time.timeAsDouble;
            if (nextAllowed.TryGetValue(key, out double allowed) && now < allowed) return;
            nextAllowed[key] = now + repeatCooldown;
            elapsed = 0f;
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.enabled = true;
            Place(sprite == joy ? Vector2.down * 2 : Vector2.zero);
        }

        // SurvivorAnimation samples the current pose and facing in LateUpdate, before this component.
        void LateUpdate()
        {
            if (!icon.enabled) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Hide(); return; }

            // Move in source pixels so the mark stays sharp next to the character art.
            Vector2 offset;
            if (icon.sprite == anger)
            {
                int beat = Mathf.FloorToInt(elapsed / 0.075f);
                offset = beat < 4 ? Vector2.right * (beat % 2 == 0 ? 1 : -1) : Vector2.zero;
            }
            else
            {
                int step = Mathf.Min(2, Mathf.FloorToInt(elapsed / 0.22f));
                offset = Vector2.down * (icon.sprite == joy ? 2 - step : step);
            }
            Place(offset);
        }

        void Place(Vector2 motionPixels)
        {
            Vector2 pixel = (Vector2)icon.transform.lossyScale / icon.sprite.pixelsPerUnit;
            var crown = new Vector2(characterSprite.transform.position.x, characterSprite.bounds.max.y);
            var mark = icon.bounds;
            Vector2 center = crown + Vector2.Scale(headOffsetPixels, pixel);
            if (mentalGauge && mentalGauge.gameObject.activeInHierarchy)
            {
                float barBottom = mentalGauge.TransformPoint(mentalGauge.rect.min).y;
                float overlap = center.y + mark.extents.y + pixel.y - barBottom;
                if (overlap > 0f) center.y -= Mathf.Ceil(overlap / pixel.y) * pixel.y;
            }
            center += Vector2.Scale(motionPixels, pixel);
            icon.transform.position += (Vector3)(center - (Vector2)mark.center);
        }

        public void Hide() => icon.enabled = false;
        internal void ResetForRun()
        {
            nextAllowed.Clear();
            Hide();
        }
        void OnDisable() { if (icon) Hide(); }
    }
}
