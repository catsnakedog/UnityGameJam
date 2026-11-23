using System;
using ShelterRun;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class BunkerTutorial : MonoBehaviour
    {
        public TutorialBroadcast dialogue;
        public BunkerSimulation simulation;
        public ShelterCommander commander;
        public Camera view;
        public ShelterArea common, planning, programming, graphics, sleeping, guard;
        public ShelterInventory inventory;
        public RectTransform bagPanel, clock, progressPanel;
        Canvas clockLayer, progressLayer;

        Vector3 initialPosition;
        ShelterArea room;
        RectTransform hud;
        bool presenting, bagOpen;

        void Awake()
        {
            if (!FarmingRunTransfer.BunkerTutorialPending)
            {
                gameObject.SetActive(false);
                return;
            }
            initialPosition = view.transform.position;
            presenting = true;
            clockLayer = clock.gameObject.AddComponent<Canvas>();
            progressLayer = progressPanel.gameObject.AddComponent<Canvas>();
            simulation.AcquirePause(this);
            commander.AcquireInputBlock(this);
            dialogue.gameObject.SetActive(true);
        }

        async void Start()
        {
            try
            {
                await simulation.InitializeAsync();
                if (!this || !presenting) return;
                dialogue.Open("bunker_", "클릭하여 벙커 운영 시작", Focus, Finish);
            }
            catch (Exception error)
            {
                if (!this || !presenting) return;
                Debug.LogException(error, this);
                dialogue.ShowError("클릭하여 벙커로", Finish);
            }
        }

        void Focus(string situation)
        {
            room = situation switch
            {
                "bunker_people" => common,
                "bunker_planning" => planning,
                "bunker_programming" => programming,
                "bunker_graphics" => graphics,
                "bunker_rest" => sleeping,
                "bunker_guard" => guard,
                _ => null
            };
            hud = situation == "bunker_bag" ? bagPanel : situation == "bunker_clock" ? clock : null;
            // Lift the actual HUD above the ceiling strip for the line that explains it.
            SetHighlighted(clockLayer, hud == clock);
            SetHighlighted(progressLayer, situation == "bunker_graphics");
            if (bagOpen != (hud == bagPanel))
            {
                bagOpen = hud == bagPanel;
                inventory.SetOpen(bagOpen);
            }
            if (room)
            {
                var centre = room.roomArtwork.bounds.center;
                // Keep the room below the speech bubble and to its left.
                view.transform.position = new Vector3(centre.x + view.orthographicSize * .36f,
                    centre.y + view.orthographicSize * .38f, initialPosition.z);
            }
            else view.transform.position = initialPosition;
            RefreshHighlight();
        }

        void LateUpdate() => RefreshHighlight();

        void SetHighlighted(Canvas layer, bool highlighted)
        {
            layer.overrideSorting = highlighted;
            if (highlighted) layer.sortingOrder = dialogue.GetComponentInParent<Canvas>().sortingOrder + 1;
        }

        void RefreshHighlight()
        {
            if (hud) dialogue.Highlight(hud, 12, hud == clock);
            else if (room)
            {
                var bounds = room.roomArtwork.bounds;
                Vector2 min = view.WorldToViewportPoint(bounds.min);
                Vector2 max = view.WorldToViewportPoint(bounds.max);
                dialogue.Highlight(min - Vector2.one * .012f, max + Vector2.one * .012f);
            }
            else dialogue.Highlight(Vector2.zero, Vector2.one);
        }

        void Finish()
        {
            FarmingRunTransfer.FinishBunkerTutorial();
            gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (!presenting) return;
            presenting = false;
            if (bagOpen && inventory) inventory.SetOpen(false);
            if (clockLayer) Destroy(clockLayer);
            if (progressLayer) Destroy(progressLayer);
            if (view) view.transform.position = initialPosition;
            if (commander) commander.ReleaseInputBlock(this);
            if (simulation) simulation.ReleasePause(this);
        }
    }
}
