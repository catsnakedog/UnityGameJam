using UnityEngine;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class SurvivorEmotion : MonoBehaviour
    {
        [SerializeField] SpriteRenderer icon;
        [SerializeField] SpriteRenderer characterSprite;
        [SerializeField] RectTransform mentalGauge;
        [SerializeField] Sprite anger;
        [SerializeField] Sprite gloom;
        [Tooltip("Offset in pixels from the character's top-right corner to the mark's bottom-left corner.")]
        [SerializeField] Vector2Int cornerOffsetPixels = new Vector2Int(1, -5);
        [SerializeField, Min(0.1f)] float duration = 1.35f;

        float elapsed;

        void Awake() => Hide();

        public void ShowAnger() => Show(anger);
        public void ShowGloom() => Show(gloom);

        void Show(Sprite sprite)
        {
            if (!sprite || !isActiveAndEnabled || !characterSprite || !characterSprite.sprite) return;
            elapsed = 0f;
            icon.sprite = sprite;
            icon.color = Color.white;
            icon.enabled = true;
            Place(Vector2.zero);
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
                offset = Vector2.down * Mathf.Min(2, Mathf.FloorToInt(elapsed / 0.22f));
            }
            Place(offset);
        }

        void Place(Vector2 motionPixels)
        {
            Vector2 pixel = (Vector2)icon.transform.lossyScale / icon.sprite.pixelsPerUnit;
            Vector2 corner = characterSprite.bounds.max;
            Vector2 inset = cornerOffsetPixels;
            var mark = icon.bounds;
            if (mentalGauge && mentalGauge.gameObject.activeInHierarchy)
            {
                var bar = mentalGauge.rect;
                var barEdge = mentalGauge.TransformPoint(new Vector3(bar.xMax, bar.yMin));
                if (corner.y + inset.y * pixel.y + mark.size.y > barEdge.y)
                    inset.x = Mathf.Max(inset.x, Mathf.Ceil((barEdge.x - corner.x) / pixel.x) + 1f);
            }
            Vector2 bottomLeft = corner + Vector2.Scale(inset + motionPixels, pixel);
            icon.transform.position += (Vector3)(bottomLeft - (Vector2)mark.min);
        }

        public void Hide() => icon.enabled = false;
        void OnDisable() { if (icon) Hide(); }
    }
}
