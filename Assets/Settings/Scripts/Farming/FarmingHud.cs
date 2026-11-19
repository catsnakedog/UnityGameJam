using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShelterRun
{
    public sealed class FarmingHud : MonoBehaviour
    {
        [SerializeField] TMP_Text missionTitle;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Image timeFill;
        [SerializeField] TMP_Text capacityText;
        [SerializeField] TMP_Text rescuedText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text toastText;
        [SerializeField] GameObject overlay;
        [SerializeField] TMP_Text overlayTitle;
        [SerializeField] TMP_Text overlayBody;
        [SerializeField] TMP_Text actionText;
        [SerializeField] Button actionButton;
        [SerializeField] Transform slotParent;
        [SerializeField] FarmingCarrySlot slotPrefab;
        readonly List<FarmingCarrySlot> slots = new List<FarmingCarrySlot>();
        FarmingSession session;
        float toastUntil;
        string failure;
        string rescueTitle;

        public void Bind(FarmingSession value)
        {
            session = value;
            capacityText.text = "운반";
            actionButton.onClick.RemoveListener(OnAction);
            actionButton.onClick.AddListener(OnAction);
        }
        public void Prepare(FarmingRules rules)
        {
            rescueTitle = $"{rules.Duration:0.##}초의 구조";
            PrepareSlots(rules.Capacity);
        }
        public void PrepareSlots(int capacity)
        {
            foreach (var slot in slots) if (slot) Destroy(slot.gameObject);
            slots.Clear();
            for (int i = 0; i < capacity; i++) slots.Add(Instantiate(slotPrefab, slotParent));
        }
        void OnAction()
        {
            if (session.Phase == FarmingPhase.Ready) session.Begin();
            else if (session.Phase == FarmingPhase.Finished) session.EnterShelter();
        }
        public void Notify(string value)
        {
            if (!toastText) return;
            toastText.text = value;
            toastUntil = Time.unscaledTime + 2;
        }
        public void ShowFailure(string value) { failure = value; }

        void LateUpdate()
        {
            if (session == null) return;
            bool running = session.Phase == FarmingPhase.Running;
            bool preparing = session.Phase == FarmingPhase.Preparing;
            overlay.SetActive(!running && !preparing);
            actionButton.gameObject.SetActive(session.Phase == FarmingPhase.Ready || session.Phase == FarmingPhase.Finished);
            if (session.Phase == FarmingPhase.Loading)
            { overlayTitle.text = "준비 중"; overlayBody.text = "파밍 데이터를 불러오고 있습니다"; return; }
            if (session.Phase == FarmingPhase.Failed)
            { overlayTitle.text = "데이터를 확인해 주세요"; overlayBody.text = failure; return; }

            if (missionTitle) missionTitle.text = preparing ? "파밍 준비" : rescueTitle;
            float remaining = preparing ? session.PreparationRemaining : session.Remaining;
            timerText.text = Mathf.CeilToInt(remaining).ToString("00");
            timerText.color = !preparing && remaining <= session.Rules.WarningSeconds ? new Color(1, 0.38f, 0.28f) : new Color(1, 0.84f, 0.51f);
            timeFill.fillAmount = remaining / (preparing ? session.Rules.PreparationDuration : session.Rules.Duration);
            var inventory = session.Player.Inventory;
            if (rescuedText) rescuedText.text = $"확보   사람 {session.PeopleSaved}/{session.PeopleTotal}   ·   물자 {session.ItemsSaved}";
            int filled = 0;
            foreach (var item in inventory.Carried)
                for (int i = 0; i < item.Record.Weight && filled < slots.Count; i++) slots[filled++].Show(item.Record.Type);
            while (filled < slots.Count) slots[filled++].Show(null);

            if (toastText) toastText.gameObject.SetActive((running || preparing) && Time.unscaledTime < toastUntil);
            if (promptText) promptText.text = ActionPrompt(inventory, preparing);

            if (session.Phase == FarmingPhase.Ready)
            {
                overlayTitle.text = $"{session.Rules.Duration:0.##}초, 챙기고 구하라";
                overlayBody.text = $"당신은 이미 확보된 생존자입니다. 처음 {session.Rules.PreparationDuration:0.##}초 동안 빈손으로 방을 둘러보세요.\n준비가 끝나면 생존자와 물자를 들 수 있습니다.\n\nA / D   좌우 이동       E   문 / 들기 / 내려놓기\n들고 나온 대상은 복도 왼쪽 벙커에서 E로 투입하세요.";
                actionText.text = "E   준비 시작";
            }
            else if (session.Phase == FarmingPhase.Finished)
            {
                overlayTitle.text = "파밍 종료";
                overlayBody.text = $"당신을 포함해 생존자 {session.PeopleSaved}명과 물자 {session.ItemsSaved}개를 확보했습니다.\n\n남겨진 사람  {session.PeopleTotal - session.PeopleSaved}명\n\n들고 있던 대상은 벙커에 포함되지 않습니다.";
                actionText.text = "벙커로 이동";
            }
        }
        string ActionPrompt(FarmingInventory inventory, bool preparing)
        {
            if (session.Phase == FarmingPhase.Finished) return "벙커로 이동 버튼으로 계속   ·   [ R ] 다시 파밍";
            if (session.Player.AtBunker && inventory.Used > 0) return "[ E ]  들고 있는 대상 모두 벙커에 투입";
            if (session.Player.FocusedDoor != null) return "[ E ]  문 통과";
            if (preparing) return "A / D  이동   ·   E  문 열기   ·   준비 후 획득 가능";
            var item = session.Player.Focused;
            if (item != null)
            {
                string kind = item.Record.Type == FarmingObjectType.Person ? "사람" : "물자";
                return inventory.CanAdd(item) ? $"[ E ]  {kind} 들기" : "운반 공간이 부족합니다";
            }
            return inventory.Used > 0 ? "[ E ]  마지막 대상 내려놓기   ·   출구 문으로 복도 복귀" : "A / D  좌우 이동   ·   [ E ] 문 / 들기";
        }
        void OnDestroy() { actionButton.onClick.RemoveListener(OnAction); }
    }
}
