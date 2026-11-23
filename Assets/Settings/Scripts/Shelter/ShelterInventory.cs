using GameJam.Audio;
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
    public sealed class ShelterInventory : MonoBehaviour, IScrollHandler
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
        [SerializeField] GameObject footerPaper;
        [SerializeField] RectTransform tooltip;
        [SerializeField] TMP_Text tooltipTitle, tooltipDetail;
        ShelterInventorySlot hovered;
        ShelterInventorySlot dragSource;
        ShelterStock dragged;
        int dragRun;
        EquippedItemView draggedView;
        const int Columns = 2;
        long firstRow;

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

        void Toggle() => SetOpen(!panel.activeSelf);

        public void SetOpen(bool open)
        {
            EndDragVisual();
            firstRow = 0;
            panel.SetActive(open);
            Refresh();
        }

        void Previous() { firstRow--; Refresh(); }
        void Next() { firstRow++; Refresh(); }

        public void OnScroll(PointerEventData data)
        {
            if (!panel.activeSelf || dragSource || data.scrollDelta.y == 0f) return;
            if (data.scrollDelta.y > 0f) Previous(); else Next();
            data.Use();
        }

        void ShowFooter(string message)
        {
            footer.text = message;
            bool visible = !string.IsNullOrEmpty(message);
            footer.gameObject.SetActive(visible);
            if (footerPaper) footerPaper.SetActive(visible);
        }

        void Refresh()
        {
            if (!panel.activeSelf) return;
            var stock = supplies.OwnedItems().ToList();
            long total = stock.Sum(entry => (long)entry.Count);
            long lastRow = Math.Max(0, (total + Columns - 1) / Columns - slots.Length / Columns);
            firstRow = Math.Max(0, Math.Min(firstRow, lastRow));
            totalLabel.text = $"보유 {total:N0}개";
            ShowFooter(total == 0 ? "보관한 아이템이 없습니다." : string.Empty);
            pageNavigation.SetActive(lastRow > 0);
            previousPage.interactable = firstRow > 0;
            nextPage.interactable = firstRow < lastRow;
            pageLabel.text = $"{firstRow + 1} / {lastRow + 1}";

            // Skip whole stacks before the page. Bind at most one page even for large rewards.
            long skip = firstRow * Columns;
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
                {
                    var gear = entry.Supply;
                    string bonus = gear.PlusAttack > 0 ? $"전투력 +{gear.PlusAttack}" : string.Empty;
                    if (gear.PlusSkill > 0)
                        bonus += (bonus.Length > 0 ? " · " : string.Empty) + $"개발력 +{gear.PlusSkill}";
                    detail = bonus;
                }
                else if (entry.Supply != null)
                    detail = $"멘탈 +{entry.Supply.MentalRestore}\n보유 {supplies.GetCount(entry.Id):N0}개";
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
            if (dragSource || !panel.activeInHierarchy) return;
            hovered = slot;
            var rect = (RectTransform)slot.transform;
            var bag = (RectTransform)panel.transform;
            var rowCenter = bag.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            tooltip.position = bag.TransformPoint(new Vector2(bag.rect.xMin - 10f, rowCenter.y));
            tooltipTitle.text = slot.Title;
            tooltipDetail.text = slot.Detail;
            tooltip.gameObject.SetActive(true);
        }
        internal void BeginItemDrag(ShelterInventorySlot source, ShelterStock stock, Sprite sprite, PointerEventData data)
        {
            if (dragSource || commander.InputBlocked || data.button != PointerEventData.InputButton.Left ||
                (stock.Instance == null && stock.Supply == null)) return;
            GameAudio.Play(GameSound.Pickup);
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
            bool applied = target && supplies.TryUse(item, target, view);
            GameAudio.Play(applied ? GameSound.Drop : GameSound.Wrong);
            if (target && !applied)
                ShowFooter(target.IsFainted ? "기절한 생존자는 아이템을 사용할 수 없습니다." : "지금은 이 아이템을 사용할 수 없습니다.");
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
