using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityGameJam.GameDev
{
    public sealed class DevelopmentLockHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] GameObject padlock;
        [SerializeField] GameObject tooltip;
        [SerializeField] TMP_Text caption;
        bool hovered;
        float requiredPlanning = -1f;

        public void SetLocked(bool locked, float threshold)
        {
            if (locked && requiredPlanning != threshold)
            {
                requiredPlanning = threshold;
                caption.text = $"기획 {threshold:0.##}% 초과 시 작업을 진행할 수 있습니다.";
            }
            padlock.SetActive(locked);
            tooltip.SetActive(locked && hovered);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            tooltip.SetActive(padlock.activeSelf);
        }

        public void OnPointerExit(PointerEventData eventData) => ClearHover();
        void OnDisable() => ClearHover();
        void ClearHover()
        {
            hovered = false;
            if (tooltip) tooltip.SetActive(false);
        }
    }
}
