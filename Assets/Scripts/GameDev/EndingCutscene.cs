using System.Collections;
using System.Linq;
using GameJam.Data;
using TMPro;
using UnityEngine;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

namespace UnityGameJam.GameDev
{
    public sealed class EndingCutscene : MonoBehaviour
    {
        public const string SceneName = "Ending";
        public GameObject bunker, farmingRoom;
        public Transform doorway;
        public ShelterWalker[] zombies;
        public SpriteRenderer[] developers;
        public SurvivorAnimation art;
        public RectTransform bubble;
        public TMP_Text dialogue;
        Vector3[] inside, partyFeet;
        float entryDistance;
        bool incoming;
        SurvivorAnimation.Artwork[] cast;
        SurvivorAnimation.Playback[] players;
        Transform speaker;
        bool party;
        float partyTime;

        public void Prepare(string ending)
        {
            inside ??= zombies.Select(z => z.transform.position).ToArray();
            // Mirror the indoor queue across the gameplay doorway, preserving everyone's order.
            entryDistance = inside.Max(p => p.x) + inside.Min(p => p.x) - 2f * doorway.position.x;
            incoming = ending == "ending_released";
            for (int i = 0; i < zombies.Length; i++)
            {
                zombies[i].TakeCombatPosition(inside[i] + Vector3.left * (incoming ? entryDistance : 0f), !incoming);
                zombies[i].GetComponent<ZombieAnimation>().BindCastMember(i);
            }
            bunker.SetActive(true);
            farmingRoom.SetActive(false);
            bubble.gameObject.SetActive(false);
        }

        public IEnumerator Play(GameDevEndingPopup screen, string ending)
        {
            bool good = ending == "ending_breakthrough";
            yield return March(good ? "ending_satisfied" : incoming ? "invade" : "ending_bad", good || incoming ? 2f : 2.8f);
            if (good)
            {
                yield return screen.Fade(1f);
                bunker.SetActive(false);
                farmingRoom.SetActive(true);
                partyFeet = developers.Select(r => r.transform.position).ToArray();
                cast = developers.Select(r => art.FindArtwork(r.name)).ToArray();
                players = developers.Select(r => new SurvivorAnimation.Playback()).ToArray();
                party = true;
                yield return screen.Fade(0f);
                yield return Talk(developers.Select(r => r.transform).ToArray(), "ending_joy");
            }
        }

        IEnumerator March(string situation, float speed)
        {
            var lines = Lines(situation);
            float leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, .5f, 10)).x;
            float exitDistance = inside.Max(p => p.x) - leftEdge + 1f;
            float distance = incoming ? entryDistance : exitDistance;
            int line = -1;
            for (float travelled = 0f; travelled < distance;)
            {
                yield return null;
                if (GameOptions.BlocksInput) continue;
                float dt = Time.unscaledDeltaTime;
                travelled = Mathf.Min(distance, travelled + dt * speed);
                float offset = incoming ? travelled - entryDistance : -travelled;
                for (int i = 0; i < zombies.Length; i++)
                    zombies[i].ApplyMovementPosition(inside[i] + Vector3.right * offset, dt);

                // Each line belongs to the zombie passing the door, while it is still in shot.
                int next = Mathf.FloorToInt(travelled / entryDistance * lines.Length);
                if (next == line) continue;
                line = next;
                if (line >= lines.Length) { HideBubble(); continue; }
                float halfLine = entryDistance / (2f * lines.Length);
                float speakerX = doorway.position.x + (incoming ? -halfLine : halfLine);
                var passer = zombies.OrderBy(z => Mathf.Abs(z.transform.position.x - speakerX)).First();
                Say(passer.transform, lines[line].Get<string>("line"));
            }
            HideBubble();
        }

        static DataRow[] Lines(string situation) => GameData.GetTable("ShelterDialogue").Rows
            .Where(r => r.Get<string>("situation") == situation).OrderBy(r => r.Id, System.StringComparer.Ordinal).ToArray();

        IEnumerator Talk(Transform[] speakers, string situation)
        {
            var rows = Lines(situation);
            for (int i = 0; i < rows.Length; i++)
            {
                speaker = speakers[i * (speakers.Length - 1) / Mathf.Max(1,rows.Length - 1)];
                if (party)
                {
                    var people = GameData.GetTable("Characters").Rows;
                    var performer = developers.FirstOrDefault(d => people.Any(p => p.Id == d.name &&
                        p.Get<string>("role") == rows[i].Get<string>("role")));
                    if (performer) speaker = performer.transform;
                }
                Say(speaker, rows[i].Get<string>("line"));
                for (float t = 0; t < 1.25f;)
                {
                    yield return null;
                    if (!GameOptions.BlocksInput) t += Time.unscaledDeltaTime;
                }
            }
            HideBubble();
        }

        void Say(Transform actor, string text)
        {
            speaker = actor;
            dialogue.text = text;
            bubble.sizeDelta = new Vector2(dialogue.GetPreferredValues(text).x + 32f, 54f);
            bubble.gameObject.SetActive(true);
            Follow();
        }

        void HideBubble()
        {
            speaker = null;
            bubble.gameObject.SetActive(false);
        }

        void Follow()
        {
            var p = speaker.position + new Vector3(.12f, 1.85f);
            float right = Camera.main.ViewportToWorldPoint(new Vector3(1, .5f, 10)).x;
            p.x = Mathf.Min(p.x, right - bubble.rect.width * bubble.lossyScale.x - .12f);
            bubble.position = p;
        }

        void LateUpdate()
        {
            if (GameOptions.BlocksInput) return;
            if (party)
            {
                float dt = Time.unscaledDeltaTime;
                partyTime += dt;
                for (int i = 0; i < developers.Length; i++)
                {
                    float hop = Mathf.Max(0f, Mathf.Sin(partyTime * 6f + i * 1.7f));
                    developers[i].transform.position = partyFeet[i] + Vector3.up * (.34f * hop);
                    players[i].Sample(cast[i], SurvivorAnimation.Pose.Idle, SurvivorAnimation.Pose.Idle, developers[i], dt);
                }
            }
            if (speaker) Follow();
        }
    }
}
