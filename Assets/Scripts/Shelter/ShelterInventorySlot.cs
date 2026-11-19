using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text caption;
        [SerializeField] GameObject highlight;
        ShelterInventory inventory;
        bool pointerInside;
        ShelterStock stock;

        public string Title => caption.text;
        public string Detail { get; private set; }

        public void Bind(ShelterInventory owner, string title, string detail, Sprite sprite, ShelterStock item = default)
        {
            stock = item;
            inventory = owner;
            caption.text = title;
            Detail = detail;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            if (pointerInside) UpdateHover();
        }
        public void OnBeginDrag(PointerEventData data) => inventory?.BeginItemDrag(this, stock, icon.sprite, data);
        public void OnDrag(PointerEventData data) => inventory?.MoveItemDrag(this, data);
        public void OnEndDrag(PointerEventData data)
        {
            inventory?.DropItem(this, data);
            var hit = data.pointerCurrentRaycast.gameObject;
            var slot = hit ? hit.GetComponentInParent<ShelterInventorySlot>() : null;
            if (slot && slot.pointerInside) slot.UpdateHover();
        }

        public void OnPointerEnter(PointerEventData data)
        {
            pointerInside = true;
            UpdateHover();
        }

        public void OnPointerExit(PointerEventData data) => ClearHover();
        void OnDisable() => ClearHover();

        void UpdateHover()
        {
            bool occupied = !string.IsNullOrEmpty(Title);
            highlight.SetActive(occupied);
            if (!inventory) return;
            if (occupied) inventory.ShowTooltip(this);
            else inventory.HideTooltip(this);
        }

        void ClearHover()
        {
            pointerInside = false;
            highlight.SetActive(false);
            if (inventory) inventory.HideTooltip(this);
        }
    }
}
