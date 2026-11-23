using System.Collections;
using System.Linq;
using ShelterRun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] Button confirm, retry;
        [SerializeField] ScrollRect scroll;
        [SerializeField] Image reactionFace;
        [SerializeField] Sprite angryFace, neutralFace, happyFace;
        [SerializeField] CanvasGroup blackout;
        bool presenting, leaving;
        string starter;
        public bool IsOpen => panel && panel.activeSelf;

        void Awake()
        {
            panel.SetActive(false);
            blackout.gameObject.SetActive(false);
            confirm.onClick.AddListener(() => Leave(false));
            retry.onClick.AddListener(() => Leave(true));
        }

        void OnEnable()
        {
            if (!manager) return;
            manager.Changed += Refresh;
            Refresh();
        }

        void Refresh()
        {
            if (presenting || manager.Phase != GameDevPhase.Ended || manager.Ending == null || !manager.TotalScore.HasValue) return;
            presenting = true;
            simulation.AcquirePause(this);
            commander.AcquireInputBlock(this);
            starter = FarmingRunTransfer.SelectedStarter ?? FindFirstObjectByType<ShelterRoster>()?.Characters.FirstOrDefault()?.Id;
            StartCoroutine(Present(manager.Ending, manager.FormattedScore));
        }

        IEnumerator Present(GameDevEnding ending, string formattedScore)
        {
            if (reactionFace)
                reactionFace.sprite = ending.Id == "ending_breakthrough" ? happyFace :
                    ending.Id == "ending_unfinished" ? angryFace : neutralFace;
            title.text = ending.DisplayName;
            body.text = ending.Description;
            score.text = $"평가 점수  {formattedScore}";
            yield return Fade(1f);
            DontDestroyOnLoad(gameObject);
            yield return SceneManager.LoadSceneAsync(EndingCutscene.SceneName);
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            var stage = FindFirstObjectByType<EndingCutscene>();
            stage.Prepare(ending.Id);
            yield return Fade(0f);
            yield return stage.Play(this, ending.Id);
            yield return Fade(1f);
            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
            yield return Fade(0f);
            confirm.Select();
        }

        internal IEnumerator Fade(float target)
        {
            blackout.gameObject.SetActive(true);
            while (!Mathf.Approximately(blackout.alpha, target))
            {
                if (!GameOptions.BlocksInput)
                    blackout.alpha = Mathf.MoveTowards(blackout.alpha, target, Time.unscaledDeltaTime / .4f);
                yield return null;
            }
            blackout.gameObject.SetActive(target > 0f);
        }

        void Leave(bool replay)
        {
            if (!IsOpen || leaving || blackout.gameObject.activeSelf || GameOptions.BlocksInput) return;
            leaving = true;
            if (replay) FarmingRunTransfer.BeginRun(starter, FarmingRunTransfer.Difficulty);
            else FarmingRunTransfer.Reset();
            SceneManager.LoadSceneAsync(replay ? "Farming" : TitleMenu.SceneName);
        }

        void OnDisable()
        {
            if (manager) manager.Changed -= Refresh;
            if (simulation) simulation.ReleasePause(this);
            if (commander) commander.ReleaseInputBlock(this);
        }
    }
}
