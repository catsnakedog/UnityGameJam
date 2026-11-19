using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class TitleScenery : MonoBehaviour
{
    [System.Serializable]
    class Performer
    {
        public Texture2D location;
        public AnimationClip motion;
        [Tooltip("Foot position in background pixels, measured from the bottom left.")]
        public Vector2 feet;
        public bool faceLeft;
        [Tooltip("Signed horizontal distance in background pixels to the other end of the walk.")]
        public float walkDistance;
    }

    [SerializeField] RawImage current, incoming;
    [SerializeField] Texture2D[] locations;
    [SerializeField] Performer[] performers;
    [SerializeField] Image[] currentActors, incomingActors;
    [SerializeField] SpriteRenderer sampler;
    [SerializeField, Min(1f)] float secondsPerView = 8f;
    [SerializeField, Min(.1f)] float dissolveSeconds = 1.6f;
    int index;
    float elapsed;

    void OnEnable() => Draw();

    void Update()
    {
        if (GameOptions.BlocksInput || locations.Length == 0) return;
        elapsed += Time.deltaTime;
        while (elapsed >= secondsPerView)
        {
            elapsed -= secondsPerView;
            index = (index + 1) % locations.Length;
        }
        Draw();
    }

    void Draw()
    {
        if (!current || !incoming || locations == null || locations.Length == 0) return;
        var viewport = current.rectTransform.rect.size;
        if (viewport.x <= 0 || viewport.y <= 0) return;
        float fade = Mathf.Min(dissolveSeconds, secondsPerView);
        float overlap = elapsed - (secondsPerView - fade);
        float duration = secondsPerView + fade;
        Frame(current, currentActors, index, elapsed + fade, duration, viewport, 1f);
        bool blending = overlap >= 0 && locations.Length > 1;
        incoming.gameObject.SetActive(blending);
        if (blending)
            Frame(incoming, incomingActors, (index + 1) % locations.Length, overlap, duration, viewport,
                Mathf.SmoothStep(0, 1, overlap / fade));
    }

    void Frame(RawImage image, Image[] actors, int location, float time, float duration, Vector2 viewport, float alpha)
    {
        var texture = locations[location];
        image.texture = texture;
        float aspectRatio = viewport.x / viewport.y * texture.height / texture.width;
        float width = .92f * Mathf.Min(1f, aspectRatio);
        float height = .92f * Mathf.Min(1f, 1f / aspectRatio);
        float travel = Mathf.Min(1f - width, width * .18f);
        float direction = location % 2 == 0 ? 1f : -1f;
        float center = .5f + (time / duration - .5f) * travel * direction;
        image.uvRect = new Rect(center - width * .5f, (.96f - height) * .5f, width, height);
        image.enabled = true;
        image.canvasRenderer.SetAlpha(alpha);
        int slot = 0;
        foreach (var person in performers)
        {
            if (person.location != texture || slot >= actors.Length) continue;
            var actor = actors[slot++];
            person.motion.SampleAnimation(sampler.gameObject, Mathf.Repeat(time + .37f * slot, person.motion.length));
            var frame = sampler.sprite;
            var feet = person.feet;
            bool flip = person.faceLeft;
            if (person.walkDistance != 0f)
            {
                float legTime = Mathf.Abs(person.walkDistance) / 18f;
                float leg = Mathf.Repeat(time / legTime, 2f);
                feet.x += person.walkDistance * Mathf.PingPong(leg, 1f);
                flip = (leg < 1f) == (person.walkDistance < 0f);
            }
            var rect = actor.rectTransform;
            Vector2 uv = new Vector2(feet.x / texture.width, feet.y / texture.height);
            // Anchors can be outside the cropped viewport while the camera pans.
            rect.anchorMin = rect.anchorMax = (uv - image.uvRect.min) / image.uvRect.size;
            rect.pivot = frame.pivot / frame.rect.size;
            rect.sizeDelta = frame.rect.size * (viewport.x / (width * texture.width));
            rect.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
            actor.sprite = frame;
            actor.enabled = true;
            actor.canvasRenderer.SetAlpha(alpha);
        }
        for (; slot < actors.Length; slot++) actors[slot].enabled = false;
    }
}
