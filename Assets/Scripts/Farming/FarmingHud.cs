using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShelterRun
{
    public sealed class FarmingHud : MonoBehaviour
    {
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

        public void Bind(FarmingSession value)
        {
            session = value;
            actionButton.onClick.RemoveListener(OnAction);
            actionButton.onClick.AddListener(OnAction);
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
        public void Notify(string value) { toastText.text = value; toastUntil = Time.unscaledTime + 2; }
        public void ShowFailure(string value) { failure = value; }

        void LateUpdate()
        {
            if (session == null) return;
            bool running = session.Phase == FarmingPhase.Running;
            overlay.SetActive(!running);
            actionButton.gameObject.SetActive(session.Phase == FarmingPhase.Ready || session.Phase == FarmingPhase.Finished);
            if (session.Phase == FarmingPhase.Loading)
            { overlayTitle.text = "준비 중"; overlayBody.text = "파밍 데이터를 불러오고 있습니다"; return; }
            if (session.Phase == FarmingPhase.Failed)
            { overlayTitle.text = "데이터를 확인해 주세요"; overlayBody.text = failure; return; }

            timerText.text = Mathf.CeilToInt(session.Remaining).ToString("00");
            timerText.color = session.Remaining <= session.Rules.WarningSeconds ? new Color(1, 0.38f, 0.28f) : new Color(1, 0.84f, 0.51f);
            timeFill.fillAmount = session.Remaining / session.Rules.Duration;
            var inventory = session.Player.Inventory;
            capacityText.text = $"운반  {inventory.Used} / {inventory.Capacity}칸";
            rescuedText.text = $"벙커 투입   사람 {session.PeopleSaved}/{session.PeopleTotal}   ·   물자 {session.ItemsSaved}";
            int filled = 0;
            foreach (var item in inventory.Carried)
                for (int i = 0; i < item.Record.Weight && filled < slots.Count; i++) slots[filled++].Show(item.Record.Type);
            while (filled < slots.Count) slots[filled++].Show(null);

            toastText.gameObject.SetActive(running && Time.unscaledTime < toastUntil);
            if (session.Player.AtBunker && inventory.Used > 0) promptText.text = "[ E ]  들고 있는 대상 모두 벙커에 투입";
            else if (session.Player.Focused != null)
            {
                var item = session.Player.Focused;
                string kind = item.Record.Type == FarmingObjectType.Person ? "사람" : "물자";
                promptText.text = inventory.CanAdd(item) ? $"[ E ]  {kind} 들기 · {item.Record.Weight}칸" : $"공간 부족 · {kind} {item.Record.Weight}칸 필요";
            }
            else promptText.text = inventory.Used > 0 ? "[ E ]  마지막 대상 내려놓기   ·   민트색 벙커로 이동" : "W A S D  이동   ·   가까이 가서 [ E ] 들기";

            if (session.Phase == FarmingPhase.Ready)
            {
                overlayTitle.text = $"{session.Rules.Duration:0}초, 챙기고 구하라";
                overlayBody.text = $"복도 왼쪽 325호에서 사람 {session.PeopleTotal}명과 물자를 구하세요.\n문으로 들어가면 방 내부가 보입니다.\n\nW A S D   이동       E   들기 / 던지기\n한 번에 {session.Rules.Capacity}칸 · 사람 2~4칸 · 물자 1~2칸\n복도 입구의 민트색 벙커에서 E로 투입하세요.";
                actionText.text = "E   파밍 시작";
            }
            else if (session.Phase == FarmingPhase.Finished)
            {
                promptText.text = "벙커로 이동 버튼으로 계속   ·   [ R ] 다시 파밍";
                overlayTitle.text = "파밍 종료";
                overlayBody.text = $"벙커에 사람 {session.PeopleSaved}명과 물자 {session.ItemsSaved}개를 보냈습니다.\n\n남겨진 사람  {session.PeopleTotal - session.PeopleSaved}명\n미투입 운반분  {inventory.Used}칸\n\n투입하지 못한 대상은 벙커에 포함되지 않습니다.";
                actionText.text = "벙커로 이동";
            }
        }
        void OnDestroy() { actionButton.onClick.RemoveListener(OnAction); }
    }
}
