using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (manager == null) return;
            subscribed = manager;
            subscribed.Changed += Refresh;
        }

        void Unsubscribe()
        {
            if (subscribed != null) subscribed.Changed -= Refresh;
            subscribed = null;
        }

        void Refresh()
        {
            if (fields == null || status == null) return;
            foreach (var field in fields) field.Refresh(manager != null ? manager.Progress : default);
            if (manager == null) { status.text = "개발 준비 중"; return; }
            switch (manager.Phase)
            {
                case GameDevPhase.Loading: status.text = "개발 준비 중"; break;
                case GameDevPhase.Developing: status.text = "세 분야를 완성해 게임을 개발하세요."; break;
                case GameDevPhase.AwaitingScore: status.text = string.IsNullOrEmpty(manager.LastError) ? "개발 종료 · 결과 집계 대기" : "결과 집계를 다시 시도해 주세요."; break;
                case GameDevPhase.Ended:
                    status.text = $"{manager.Ending.DisplayName}  ·  {manager.TotalScore:0.000}\n{manager.Ending.Description}";
                    break;
                case GameDevPhase.Failed: status.text = "개발 데이터를 확인해 주세요."; break;
            }
        }
    }
}
