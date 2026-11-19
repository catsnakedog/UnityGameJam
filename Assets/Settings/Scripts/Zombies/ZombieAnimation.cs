using System;
using UnityEngine;
using UnityGameJam.Shelter;

namespace UnityGameJam.Zombies
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShelterWalker))]
    public sealed class ZombieAnimation : MonoBehaviour
    {
        [Serializable]
        class Variant
        {
            public string zombieId;
            public SurvivorAnimation.Motion idle, walk, hit;
        }

        [SerializeField] Variant[] variants;
        [SerializeField] SpriteRenderer image;
        internal float HeadY => image.bounds.max.y;
        readonly SurvivorAnimation.Playback playback = new SurvivorAnimation.Playback();
        ShelterWalker walker;
        Variant art;
        float previousX, hitRemaining;
        bool cutscene;

        public void Bind(string zombieId)
        {
            cutscene = false;
            Select(Array.Find(variants, entry => entry.zombieId == zombieId));
        }

        public void BindCastMember(int index)
        {
            cutscene = true;
            Select(variants[index % variants.Length]);
        }

        void Select(Variant variant)
        {
            walker = GetComponent<ShelterWalker>();
            art = variant;
            if (art == null) { Debug.LogError("Missing zombie animation variant.", this); return; }
            previousX = transform.position.x;
            hitRemaining = 0f;
            playback.Restart();
            image.flipX = walker.FacingLeft;
            playback.Sample(art.idle, art.idle.firstFrame, false, image, 0f);
        }

        public void PlayHit()
        {
            hitRemaining = art.hit.clip.length;
            playback.Restart();
            playback.Sample(art.hit, art.idle.firstFrame, true, image, 0f);
        }

        void LateUpdate()
        {
            if (art == null || GameOptions.BlocksInput || (!cutscene && walker.AnimationPaused)) return;
            float dt = cutscene ? Time.unscaledDeltaTime : Time.deltaTime;
            bool walking = !walker.IsInElevator && transform.position.x != previousX && Mathf.Abs(walker.Velocity.x) > .01f;
            previousX = transform.position.x;
            image.flipX = walker.FacingLeft;
            bool hit = hitRemaining > 0f;
            playback.Sample(hit ? art.hit : walking ? art.walk : art.idle, art.idle.firstFrame, hit, image, dt);
            hitRemaining = Mathf.Max(0f, hitRemaining - dt);
        }
    }
}
