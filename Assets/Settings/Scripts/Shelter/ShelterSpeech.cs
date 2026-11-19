using System.Collections.Generic;
using GameJam.Data;
using TMPro;
using UnityEngine;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent, DefaultExecutionOrder(110)]
    public sealed class ShelterSpeech : MonoBehaviour
    {
        [SerializeField] RectTransform bubblePrefab;
        static ShelterSpeech instance;
        readonly System.Random random = new System.Random();
        readonly Dictionary<(string, string), List<string>> lines = new Dictionary<(string, string), List<string>>();
        readonly List<Bubble> pool = new List<Bubble>();
        DataTable source;

        sealed class Bubble
        {
            public RectTransform rect;
            public TMP_Text text;
            public Transform speaker;
            public ShelterSpeechAnchor anchor;
            public ShelterWalker walker;
            public ShelterArea workRoom;
            public Canvas canvas;
            public int walkingOrder;
            public float expires;
            public bool ambient;
            public readonly Queue<string> next = new Queue<string>(3);
        }

        void OnEnable() => instance = this;
        void OnDisable()
        {
            if (instance == this) instance = null;
            Clear();
        }

        internal void Clear() => ClearBubbles(null);

        void ClearBubbles(Transform speaker)
        {
            foreach (var bubble in pool)
            {
                if (!speaker || bubble.speaker == speaker) Hide(bubble);
            }
        }

        static void Hide(Bubble bubble)
        {
            bubble.rect.gameObject.SetActive(false);
            bubble.speaker = null;
            bubble.anchor = null;
            bubble.walker = null;
            bubble.workRoom = null;
            bubble.next.Clear();
        }

        public static void Say(Transform speaker, string role, string situation)
        {
            if (!instance || !speaker || speaker.gameObject.scene != instance.gameObject.scene) return;
            if (speaker.TryGetComponent<ShelterWalker>(out var walker) && walker.InTent) return;
            if (situation == "work" && (!walker || !walker.CanDoDevelopmentWork)) return;
            if (speaker.TryGetComponent<CharacterIdentity>(out var actor))
            {
                if (situation == "faint")
                {
                    if (!actor.IsFainted) return;
                    instance.ClearBubbles(speaker);
                }
                else if (!actor.CanAct) return;
            }
            if (GameData.IsReady && role != null) instance.Show(speaker, role, situation);
        }

        void Show(Transform speaker, string role, string situation)
        {
            var table = GameData.GetTable("ShelterDialogue");
            if (source != table)
            {
                lines.Clear();
                foreach (var row in table.Rows)
                {
                    var key = (row.Get<string>("role"), row.Get<string>("situation"));
                    if (!lines.TryGetValue(key, out var group)) lines.Add(key, group = new List<string>());
                    group.Add(row.Get<string>("line"));
                }
                source = table;
            }
            if (!lines.TryGetValue((role, situation), out var candidates)) return;
            bool ambient = situation == "work" || situation == "guard";
            // Stagger idle chatter without consuming combat's random sequence.
            if (ambient && random.Next(4) != 0) return;
            string line = candidates[random.Next(candidates.Count)];
            Bubble free = null;
            foreach (var bubble in pool)
            {
                if (!bubble.rect.gameObject.activeSelf) { free = bubble; continue; }
                if (bubble.speaker != speaker) continue;
                if (ambient) return;
                if (bubble.ambient)
                {
                    bubble.next.Clear();
                    bubble.workRoom = null;
                    Display(bubble, line, false);
                }
                else
                {
                    if (bubble.next.Count >= 3) bubble.next.Dequeue();
                    bubble.next.Enqueue(line);
                }
                return;
            }
            if (free == null)
            {
                var rect = Instantiate(bubblePrefab, transform);
                var canvas = rect.GetComponent<Canvas>();
                free = new Bubble { rect = rect, text = rect.GetComponentInChildren<TMP_Text>(true), canvas = canvas, walkingOrder = canvas.sortingOrder };
                pool.Add(free);
            }
            free.speaker = speaker;
            speaker.TryGetComponent(out free.anchor);
            speaker.TryGetComponent(out free.walker);
            free.workRoom = situation == "work" ? free.walker.CurrentRoom : null;
            free.rect.position = SpeakerPosition(free);
            Display(free, line, ambient);
        }

        static void Display(Bubble bubble, string line, bool ambient)
        {
            bubble.ambient = ambient;
            bubble.text.SetText(line);
            Vector2 padding = -bubble.text.rectTransform.sizeDelta;
            float width = Mathf.Clamp(Mathf.Ceil(bubble.text.GetPreferredValues(line).x + padding.x), 48f, 235f);
            float height = bubble.text.GetPreferredValues(line, width - padding.x, Mathf.Infinity).y;
            bubble.rect.sizeDelta = new Vector2(width, Mathf.Ceil(height + padding.y));
            FollowSpeaker(bubble);
            bubble.rect.gameObject.SetActive(true);
            bubble.expires = Time.time + Mathf.Clamp(line.Length * 0.10f, 1.25f, 2.4f);
        }

        static Vector3 SpeakerPosition(Bubble bubble) =>
            bubble.speaker.position + (Vector3)(bubble.anchor ? bubble.anchor.Offset : ShelterSpeechAnchor.DefaultOffset);

        static void FollowSpeaker(Bubble bubble)
        {
            if (bubble.speaker && bubble.speaker.gameObject.activeInHierarchy)
            {
                bubble.rect.position = SpeakerPosition(bubble);
                int order = bubble.walkingOrder + (bubble.walker ? bubble.walker.RideOrderOffset : 0);
                if (bubble.canvas.sortingOrder != order) bubble.canvas.sortingOrder = order;
            }
        }

        void LateUpdate()
        {
            foreach (var bubble in pool)
            {
                if (!bubble.rect.gameObject.activeSelf) continue;
                if (bubble.walker && bubble.walker.InTent) { Hide(bubble); continue; }
                if (bubble.workRoom && (!bubble.walker || bubble.walker.CurrentRoom != bubble.workRoom || !bubble.walker.CanDoDevelopmentWork))
                {
                    Hide(bubble);
                    continue;
                }
                FollowSpeaker(bubble);
                if (Time.time < bubble.expires) continue;
                if (bubble.next.Count > 0) Display(bubble, bubble.next.Dequeue(), false);
                else Hide(bubble);
            }
        }
    }
}
