using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.Shelter;

namespace UnityGameJam.GameDev
{
    [DisallowMultipleComponent]
    public sealed class DevGageUI : MonoBehaviour
    {
        [Serializable]
        public sealed class FieldView
        {
            [SerializeField] GameDevField field;
            [SerializeField] Image fill;
            [SerializeField] TMP_Text percentage;
            public void Refresh(GameDevProgress progress)
            {
                float value = progress.Get(field);
                fill.fillAmount = value / GameDevProgress.MaximumPercent;
                // Do not display 100% for a value that has not actually reached completion.
                percentage.text = (Mathf.Floor(value * 10f) / 10f).ToString("0.#") + "%";
            }
        }

        [SerializeField] GameDevManager manager;
        [SerializeField] FieldView[] fields;
        [SerializeField] TMP_Text status;
        [SerializeField] Image clockFill;
        [SerializeField] TMP_Text clockTime;
        BunkerSimulation simulation;
        int displayedTick = -1;
        int displayedTotal = -1;
        GameDevManager subscribed;

        void OnEnable() { Subscribe(); Refresh(); }
        void OnDisable() { Unsubscribe(); }

        public void Bind(GameDevManager value)
        {
            Unsubscribe();
            manager = value;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        void Subscribe()
        {
            Unsubscribe();
            simulation = manager ? manager.GetComponent<BunkerSimulation>() : null;
            displayedTick = displayedTotal = -1;
            RefreshClock();
            if (manager == null) return;
            subscribed = manager;
            subscribed.Changed += Refresh;
        }

        void Unsubscribe()
        {
            if (subscribed != null) subscribed.Changed -= Refresh;
            subscribed = null;
        }

        void LateUpdate() => RefreshClock();

        void RefreshClock()
        {
            if (!clockFill || !clockTime) return;
            int tick = simulation ? simulation.CurrentTick : 0;
            int total = simulation && simulation.Rules ? simulation.Rules.TotalTicks : 0;
            if (tick == displayedTick && total == displayedTotal) return;
            displayedTick = tick;
            displayedTotal = total;
            clockFill.fillAmount = total > 0 ? (float)tick / total : 0f;
            int minutes = total > 0 ? (int)(Math.Min(tick, total) * 1440L / total) : 0;
            clockTime.text = $"{minutes / 60:00}:{minutes % 60:00}";
        }

        void Refresh()
        {
            if (fields == null || status == null) return;
            foreach (var field in fields) field.Refresh(manager != null ? manager.Progress : default);
            status.gameObject.SetActive(manager == null || manager.Phase != GameDevPhase.Developing);
            if (manager == null) { status.text = "개발 준비 중"; return; }
            switch (manager.Phase)
            {
                case GameDevPhase.Loading: status.text = "개발 준비 중"; break;
                case GameDevPhase.Developing: break;
                case GameDevPhase.AwaitingScore: status.text = string.IsNullOrEmpty(manager.LastError) ? "개발 종료 · 결과 집계 대기" : "결과 집계를 다시 시도해 주세요."; break;
                case GameDevPhase.Ended:
                    status.text = $"{manager.Ending.DisplayName}  ·  {manager.TotalScore:0.000}\n{manager.Ending.Description}";
                    break;
                case GameDevPhase.Failed: status.text = "개발 데이터를 확인해 주세요."; break;
            }
        }
    }
}
