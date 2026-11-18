using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.GameDev;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class WorkProgressFeedback : MonoBehaviour
    {
        [SerializeField] CanvasGroup bubble;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text amountLabel;
        [SerializeField] Sprite planningIcon;
        [SerializeField] Sprite graphicsIcon;
        [SerializeField] Sprite programmingIcon;
        [SerializeField, Min(0.01f)] float duration = 0.85f;
        [SerializeField] float rise = 0.28f;
        Vector3 startPosition;
        float elapsed;
        float lastDisplayed = float.NaN;

        void Awake()
        {
            startPosition = bubble.transform.localPosition;
            Hide();
        }

        internal void Show(GameDevField field, float amount)
        {
            icon.sprite = field == GameDevField.Planning ? planningIcon :
                field == GameDevField.Graphics ? graphicsIcon : programmingIcon;
            // Trim floating-point noise from subtracting two near-equal progress values.
            float displayed = Mathf.Round(amount * 1000f) / 1000f;
            bubble.gameObject.SetActive(true);
            if (displayed != lastDisplayed)
            {
                if (displayed > 0f) amountLabel.SetText("+{0}%", displayed);
                else amountLabel.SetText("<0.001%");
                CenterContents();
                lastDisplayed = displayed;
            }
            bubble.transform.localPosition = startPosition;
            bubble.alpha = 1f;
            elapsed = 0f;
            enabled = true;
        }

        void CenterContents()
        {
            const float gap = 4f;
            amountLabel.ForceMeshUpdate();
            var bounds = amountLabel.textBounds;
            var picture = icon.rectTransform;
            float iconWidth = picture.rect.width;
            float left = -(iconWidth + gap + bounds.size.x) * 0.5f;
            picture.anchoredPosition = new Vector2(left - picture.rect.xMin, picture.anchoredPosition.y);
            var label = amountLabel.rectTransform;
            label.anchoredPosition = new Vector2(left + iconWidth + gap - bounds.min.x, label.anchoredPosition.y);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Hide(); return; }
            float t = elapsed / duration;
            bubble.transform.localPosition = startPosition + Vector3.up * (rise * t);
            bubble.alpha = Mathf.Clamp01((1f - t) * 3f);
        }

        internal void Hide()
        {
            enabled = false;
            bubble.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (bubble) bubble.gameObject.SetActive(false);
        }
    }
}
