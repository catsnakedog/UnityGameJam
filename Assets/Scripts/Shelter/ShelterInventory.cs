using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityGameJam.Characters;
using ShelterRun;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class ShelterInventory : MonoBehaviour
    {
        [Serializable]
        struct ItemArt
        {
            public Sprite icon;
            [Tooltip("파밍 물자의 한글 UI 이름. 벙커 소비 물자는 카탈로그 이름을 사용합니다.")]
            public string pickupCaption;
        }

        [SerializeField] ShelterSupplies supplies;
        [SerializeField] ShelterCommander commander;
        [SerializeField] Image dragIcon;
        [SerializeField] Button bagButton, previousPage, nextPage;
        [SerializeField] GameObject panel, pageNavigation;
        [SerializeField] TMP_Text totalLabel, footer, pageLabel;
        [SerializeField] ShelterInventorySlot[] slots;
        [SerializeField] ItemArt[] artwork;
        [SerializeField] RectTransform tooltip;
        [SerializeField] TMP_Text tooltipTitle, tooltipDetail;
        ShelterInventorySlot hovered;
        ShelterInventorySlot dragSource;
        ShelterStock dragged;
        int dragRun;
        EquippedItemView draggedView;
        long page;

        void OnEnable()
        {
            panel.SetActive(false);
            EndDragVisual();
            tooltip.gameObject.SetActive(false);
            bagButton.onClick.AddListener(Toggle);
            previousPage.onClick.AddListener(Previous);
            nextPage.onClick.AddListener(Next);
            supplies.Changed += Refresh;
        }

        void OnDisable()
        {
            EndDragVisual();
            supplies.Changed -= Refresh;
            bagButton.onClick.RemoveListener(Toggle);
            previousPage.onClick.RemoveListener(Previous);
            nextPage.onClick.RemoveListener(Next);
            panel.SetActive(false);
            tooltip.gameObject.SetActive(false);
            hovered = null;
        }

        void Toggle()
        {
            EndDragVisual();
            page = 0;
            panel.SetActive(!panel.activeSelf);
            Refresh();
        }

        void Previous() { page--; Refresh(); }
        void Next() { page++; Refresh(); }

        void Refresh()
        {
            if (!panel.activeSelf) return;
            var stock = supplies.OwnedItems().ToList();
            long total = stock.Sum(entry => (long)entry.Count);
            long lastPage = Math.Max(0, total - 1) / slots.Length;
            page = Math.Max(0, Math.Min(page, lastPage));
            totalLabel.text = $"보유 {total:N0}개";
            footer.text = total == 0 ? "보관한 아이템이 없습니다." : "생존자에게 끌어 사용하거나 장착하세요.";
            pageNavigation.SetActive(lastPage > 0);
            previousPage.interactable = page > 0;
            nextPage.interactable = page < lastPage;
            pageLabel.text = $"{page + 1} / {lastPage + 1}";

            // Skip whole stacks before the page. Bind at most one page even for large rewards.
            long skip = page * slots.Length;
            int cell = 0;
            foreach (var entry in stock)
            {
                if (skip >= entry.Count) { skip -= entry.Count; continue; }
                var art = Array.Find(artwork, candidate => candidate.icon && candidate.icon.name == entry.Id);
                string title = entry.Supply?.DisplayName;
                if (string.IsNullOrEmpty(title))
                    title = string.IsNullOrEmpty(art.pickupCaption) ? entry.Id : art.pickupCaption;
                string detail;
                if (entry.Id == FarmingDataKeys.SpaghettiCan)
                    detail = $"허기 +{supplies.Rules.FoodRestore}칸\n남은 사용 {entry.Instance?.RemainingServings ?? supplies.Rules.ServingsPerCan}/{supplies.Rules.ServingsPerCan}회";
                else if (entry.Supply?.IsEquipment == true)
                    detail = "생존자에게 장착 · 교체\n카드에서 두 번 눌러 해제";
                else if (entry.Supply != null)
                    detail = $"멘탈 +{entry.Supply.MentalRestore}\n보유 {entry.Count:N0}개";
                else
                    detail = $"운반 비용 {entry.Pickup.Weight}칸\n보관 물자";
                int visible = (int)Math.Min(entry.Count - skip, slots.Length - cell);
                for (int i = 0; i < visible; i++) slots[cell++].Bind(this, title, detail, art.icon, entry);
                if (cell == slots.Length) break;
                skip = 0;
            }
            while (cell < slots.Length) slots[cell++].Bind(this, string.Empty, string.Empty, null);
        }

        internal void ShowTooltip(ShelterInventorySlot slot)
        {
            if (dragSource) return;
            hovered = slot;
            var rect = (RectTransform)slot.transform;
            tooltip.position = rect.TransformPoint(new Vector2(rect.rect.xMin - 10f, rect.rect.center.y));
            tooltipTitle.text = slot.Title;
            tooltipDetail.text = slot.Detail;
            tooltip.gameObject.SetActive(true);
        }
        internal void BeginItemDrag(ShelterInventorySlot source, ShelterStock stock, Sprite sprite, PointerEventData data)
        {
            if (dragSource || commander.InputBlocked || data.button != PointerEventData.InputButton.Left ||
                (stock.Instance == null && stock.Supply == null)) return;
            dragSource = source;
            dragged = stock;
            dragRun = supplies.RunVersion;
            draggedView = new EquippedItemView(source.Title, sprite);
            tooltip.gameObject.SetActive(false);
            hovered = null;
            dragIcon.sprite = sprite;
            dragIcon.gameObject.SetActive(sprite);
            MoveItemDrag(source, data);
        }
        internal void MoveItemDrag(ShelterInventorySlot source, PointerEventData data)
        {
            if (source != dragSource) return;
            var parent = (RectTransform)dragIcon.transform.parent;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, data.position, data.pressEventCamera, out var position))
                dragIcon.transform.position = position;
        }
        internal void DropItem(ShelterInventorySlot source, PointerEventData data)
        {
            if (source != dragSource) return;
            var item = dragged;
            var view = draggedView;
            EndDragVisual();
            if (commander.InputBlocked || !panel.activeInHierarchy || dragRun != supplies.RunVersion) return;
            CharacterIdentity target = null;
            var hit = data.pointerCurrentRaycast;
            if (hit.module is GraphicRaycaster && hit.gameObject)
            {
                var card = hit.gameObject.GetComponentInParent<ShelterCharacterCard>();
                if (card) target = card.SelectedCharacter;
            }
            else
            {
                var walker = commander.SurvivorAtScreen(data.position);
                if (walker) target = walker.GetComponent<CharacterIdentity>();
            }
            if (target && !supplies.TryUse(item, target, view)) footer.text = "지금은 이 아이템을 사용할 수 없습니다.";
        }
        void EndDragVisual()
        {
            dragSource = null;
            dragged = default;
            draggedView = default;
            dragIcon.gameObject.SetActive(false);
        }

        internal void HideTooltip(ShelterInventorySlot slot)
        {
            if (hovered != slot) return;
            hovered = null;
            tooltip.gameObject.SetActive(false);
        }
    }
}
