using System.Collections.Generic;
using GameJam.Data;
using TMPro;
using UnityEngine;

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

        internal void Clear()
        {
            foreach (var bubble in pool)
            {
                bubble.rect.gameObject.SetActive(false);
                bubble.speaker = null;
                bubble.anchor = null;
                bubble.next.Clear();
            }
        }

        public static void Say(Transform speaker, string role, string situation)
        {
            if (instance && speaker && speaker.gameObject.scene == instance.gameObject.scene && GameData.IsReady && role != null)
                instance.Show(speaker, role, situation);
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
            bool ambient = situation == "work" || situation == "guard" || situation == "wait_plan";
            // Stagger idle chatter without consuming combat's random sequence.
            if (ambient && random.Next(4) != 0) return;
            string line = candidates[random.Next(candidates.Count)];
            Bubble free = null;
            foreach (var bubble in pool)
            {
                if (!bubble.rect.gameObject.activeSelf) { free = bubble; continue; }
                if (bubble.speaker != speaker) continue;
                if (ambient) return;
                if (bubble.ambient || situation == "faint")
                {
                    bubble.next.Clear();
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
                free = new Bubble { rect = rect, text = rect.GetComponentInChildren<TMP_Text>(true) };
                pool.Add(free);
            }
            free.speaker = speaker;
            speaker.TryGetComponent(out free.anchor);
            free.rect.position = ShelterSpeechAnchor.DefaultPosition(speaker);
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

        static void FollowSpeaker(Bubble bubble)
        {
            if (!bubble.speaker || !bubble.speaker.gameObject.activeInHierarchy) return;
            bubble.rect.position = bubble.anchor
                ? bubble.anchor.Position(bubble.rect.rect.height * bubble.rect.lossyScale.y)
                : ShelterSpeechAnchor.DefaultPosition(bubble.speaker);
        }

        void LateUpdate()
        {
            foreach (var bubble in pool)
            {
                if (!bubble.rect.gameObject.activeSelf) continue;
                FollowSpeaker(bubble);
                if (Time.time < bubble.expires) continue;
                if (bubble.next.Count > 0) Display(bubble, bubble.next.Dequeue(), false);
                else
                {
                    bubble.rect.gameObject.SetActive(false);
                    bubble.speaker = null;
                    bubble.anchor = null;
                }
            }
        }
    }
}
