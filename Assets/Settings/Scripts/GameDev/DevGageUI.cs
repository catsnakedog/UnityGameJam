using System;
using System.Collections.Generic;
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
            [SerializeField] DevelopmentLockHint lockHint;
            public void RefreshLock(bool locked, float threshold)
            {
                if (lockHint) lockHint.SetLocked(locked, threshold);
            }
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
        [SerializeField] Image clockMarkerPrefab;
        [SerializeField] Sprite presentationMarkerSprite;
        BunkerSimulation simulation;
        BunkerRules markerRules;
        bool rebuildMarkers;
        readonly List<(int tick, Image image)> waveMarkers = new List<(int, Image)>();
        readonly List<(int tick, Image image)> presentationMarkers = new List<(int, Image)>();
        int displayedWave = -1;
        int displayedPresentation = -1;
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
            InvalidateMarkers();
            RefreshClock();
            if (manager == null) return;
            subscribed = manager;
            subscribed.Changed += Refresh;
            subscribed.DevelopmentRestarted += InvalidateMarkers;
        }

        void Unsubscribe()
        {
            if (subscribed != null)
            {
                subscribed.Changed -= Refresh;
                subscribed.DevelopmentRestarted -= InvalidateMarkers;
            }
            subscribed = null;
        }

        void InvalidateMarkers() => rebuildMarkers = true;

        void LateUpdate() => RefreshClock();

        void RefreshClock()
        {
            if (!clockFill || !clockTime) return;
            int tick = simulation ? simulation.CurrentTick : 0;
            var rules = simulation ? simulation.Rules : null;
            int total = rules ? rules.TotalTicks : 0;
            if (rules != markerRules) RefreshLocks();
            RefreshMarkers(rules);
            if (tick == displayedTick && total == displayedTotal) return;
            displayedTick = tick;
            displayedTotal = total;
            clockFill.fillAmount = total > 0 ? (float)tick / total : 0f;
            int minutes = total > 0 ? (int)(Math.Min(tick, total) * 1440L / total) : 0;
            clockTime.text = $"{minutes / 60:00}:{minutes % 60:00}";
        }

        void RefreshMarkers(BunkerRules rules)
        {
            if (!clockMarkerPrefab || !presentationMarkerSprite) return;
            if (markerRules != rules || rebuildMarkers)
            {
                rebuildMarkers = false;
                markerRules = rules;
                displayedWave = displayedPresentation = -1;
                int waves = 0, presentations = 0;
                if (rules)
                {
                    for (long tick = rules.FirstInvasionTick; tick < rules.TotalTicks; tick += rules.InvasionEveryTicks)
                        BindMarker(waveMarkers, waves++, (int)tick, 0.40f, clockMarkerPrefab.sprite, "Invasion");
                    foreach (int tick in rules.PresentationTicks)
                        if (tick > 0 && tick < rules.TotalTicks)
                            BindMarker(presentationMarkers, presentations++, tick, 0.50f, presentationMarkerSprite, "Presentation");
                }
                RemoveUnused(waveMarkers, waves);
                RemoveUnused(presentationMarkers, presentations);
            }
            if (!rules) return;
            // Another popup can pause on this tick before one or both events have actually fired.
            UpdateVisibility(waveMarkers, simulation.LastWaveTick, ref displayedWave);
            UpdateVisibility(presentationMarkers, simulation.LastPresentationTick, ref displayedPresentation);
        }

        void BindMarker(List<(int tick, Image image)> markers, int index, int tick, float radius, Sprite sprite, string kind)
        {
            var image = index < markers.Count ? markers[index].image : Instantiate(clockMarkerPrefab, clockFill.rectTransform, false);
            image.name = $"{kind} {tick}";
            image.sprite = sprite;
            float angle = (float)tick / markerRules.TotalTicks * 2f * Mathf.PI;
            var anchor = new Vector2(0.5f + radius * Mathf.Sin(angle), 0.5f + radius * Mathf.Cos(angle));
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
            image.rectTransform.anchoredPosition = Vector2.zero;
            if (index == markers.Count) markers.Add((tick, image));
            else markers[index] = (tick, image);
        }

        static void UpdateVisibility(List<(int tick, Image image)> markers, int dispatched, ref int displayed)
        {
            if (displayed == dispatched) return;
            foreach (var marker in markers)
                marker.image.gameObject.SetActive(marker.tick > dispatched);
            displayed = dispatched;
        }

        static void RemoveUnused(List<(int tick, Image image)> markers, int used)
        {
            for (int index = markers.Count - 1; index >= used; index--)
            {
                var image = markers[index].image;
                image.gameObject.SetActive(false);
                Destroy(image.gameObject);
                markers.RemoveAt(index);
            }
        }

        void RefreshLocks()
        {
            var rules = simulation ? simulation.Rules : null;
            bool locked = rules && manager.Phase == GameDevPhase.Developing && !simulation.PlanningUnlocksDepartments;
            foreach (var field in fields) field.RefreshLock(locked, rules ? rules.PlanningUnlockPercent : 0f);
        }

        void Refresh()
        {
            if (fields == null || status == null) return;
            RefreshLocks();
            foreach (var field in fields) field.Refresh(manager != null ? manager.Progress : default);
            status.gameObject.SetActive(manager == null || manager.Phase != GameDevPhase.Developing);
            if (manager == null) { status.text = "개발 준비 중"; return; }
            switch (manager.Phase)
            {
                case GameDevPhase.Loading: status.text = "개발 준비 중"; break;
                case GameDevPhase.Developing: break;
                case GameDevPhase.AwaitingScore: status.text = string.IsNullOrEmpty(manager.LastError) ? "개발 종료 · 결과 집계 대기" : "결과 집계를 다시 시도해 주세요."; break;
                case GameDevPhase.Ended:
                    status.text = $"{manager.Ending.DisplayName}  ·  {manager.FormattedScore}\n{manager.Ending.Description}";
                    break;
                case GameDevPhase.Failed: status.text = "개발 데이터를 확인해 주세요."; break;
            }
        }
    }
}
