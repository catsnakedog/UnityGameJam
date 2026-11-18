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
            if (displayed > 0f) amountLabel.SetText("+{0}%", displayed);
            else amountLabel.SetText("<0.001%");
            bubble.transform.localPosition = startPosition;
            bubble.alpha = 1f;
            elapsed = 0f;
            bubble.gameObject.SetActive(true);
            enabled = true;
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
