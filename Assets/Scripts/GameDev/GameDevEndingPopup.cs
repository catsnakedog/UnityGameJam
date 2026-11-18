using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityGameJam.Shelter;

namespace UnityGameJam.GameDev
{
    [DisallowMultipleComponent]
    public sealed class GameDevEndingPopup : MonoBehaviour
    {
        [SerializeField] GameDevManager manager;
        [SerializeField] BunkerSimulation simulation;
        [SerializeField] ShelterCommander commander;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title, body, score;
        [SerializeField] Button confirm;
        [SerializeField] ScrollRect scroll;
        GameDevEnding dismissed;
        GameObject previousSelection;
        int openedFrame;
        public bool IsOpen { get; private set; }

        void Awake() { panel.SetActive(false); }

        void OnEnable()
        {
            if (!manager || !simulation || !commander || !panel || !title || !body || !score || !confirm || !scroll)
            {
                Debug.LogError("Ending popup references are missing.", this);
                return;
            }
            confirm.onClick.AddListener(Confirm);
            manager.Changed += Refresh;
            manager.DevelopmentRestarted += ResetRun;
            Refresh();
        }

        void OnDisable()
        {
            if (confirm) confirm.onClick.RemoveListener(Confirm);
            if (manager) { manager.Changed -= Refresh; manager.DevelopmentRestarted -= ResetRun; }
            Hide();
        }

        void ResetRun() { dismissed = null; Hide(); }

        void Refresh()
        {
            if (manager.Phase != GameDevPhase.Ended)
            {
                dismissed = null;
                Hide();
                return;
            }
            if (IsOpen || manager.Ending == null || !manager.TotalScore.HasValue || dismissed == manager.Ending) return;
            title.text = manager.Ending.DisplayName;
            body.text = manager.Ending.Description;
            score.text = $"평가 점수  {manager.TotalScore.Value:0.000}";
            simulation.AcquirePause(this);
            commander.AcquireInputBlock(this);
            previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            panel.SetActive(true);
            IsOpen = true;
            openedFrame = Time.frameCount;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(confirm.gameObject);
        }

        void Update()
        {
            if (!IsOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) Confirm();
        }

        public void Confirm()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            dismissed = manager.Ending;
            Hide();
        }

        void Hide()
        {
            if (simulation) simulation.ReleasePause(this);
            if (commander) commander.ReleaseInputBlock(this);
            if (panel) panel.SetActive(false);
            if (!IsOpen) return;
            IsOpen = false;
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(previousSelection && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }
    }
}
