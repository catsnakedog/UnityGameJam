using System;
using UnityEngine;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class SurvivorAnimation : MonoBehaviour
    {
        public enum Pose { Idle, TiredIdle, Walk, TiredWalk, Sit, Type, Sleep, Faint, Attack, Hit, Carried }

        [Serializable]
        public class Motion
        {
            public AnimationClip clip;
            public Sprite firstFrame;
        }

        [Serializable]
        public class Artwork
        {
            public string characterId;
            public Sprite standing;
            [Tooltip("The working animation shows this character seated on a chair.")]
            public bool workingUsesChair;
            public Motion[] motions;

            public Motion Get(Pose pose) => motions != null && (int)pose < motions.Length ? motions[(int)pose] : null;
        }

        // Each renderer advances independently through the same authored poses and frame times.
        public sealed class Playback
        {
            AnimationClip current;
            float elapsed;

            public void Restart() { current = null; elapsed = 0f; }

            public void Sample(Artwork artwork, Pose pose, Pose idle, SpriteRenderer image, float dt)
            {
                var motion = artwork.Get(pose);
                if (motion?.firstFrame == null && pose == Pose.TiredWalk) motion = artwork.Get(Pose.Walk);
                if (motion?.firstFrame == null && pose != Pose.Faint) motion = artwork.Get(idle);
                if (motion?.firstFrame == null && pose != Pose.Faint) motion = artwork.Get(Pose.Idle);
                Sample(motion, artwork.standing, pose == Pose.Faint || pose == Pose.Attack || pose == Pose.Hit, image, dt);
            }

            public void Sample(Motion motion, Sprite fallback, bool holdLast, SpriteRenderer image, float dt)
            {
                var clip = motion?.clip;
                if (current != clip) { current = clip; elapsed = 0f; }
                if (clip && clip.length > 0f)
                {
                    float time = holdLast
                        ? Mathf.Min(elapsed, Mathf.Max(0f, clip.length - 0.5f / clip.frameRate))
                        : Mathf.Repeat(elapsed, clip.length);
                    clip.SampleAnimation(image.gameObject, time);
                    elapsed += dt;
                }
                else image.sprite = motion?.firstFrame ? motion.firstFrame : fallback;
            }
        }

        [SerializeField] SpriteRenderer image;
        internal SpriteRenderer BodyRenderer => image;
        [SerializeField] Artwork[] characters;
        CharacterIdentity actor;
        ShelterWalker walker;
        Artwork art;
        ShelterArea workingRoom;
        ShelterArea waitingRoom;
        bool playWorkIntro;
        Pose combatPose;
        bool combatFacesLeft;
        float seatedTime, combatRemaining;
        bool fainted;
        float faintElapsed;
        const float FaintFadeDuration = .32f;
        readonly Playback playback = new Playback();

        public Artwork FindArtwork(string characterId) => Array.Find(characters, entry => entry.characterId == characterId);

        public void Bind(CharacterIdentity owner, ShelterWalker movement)
        {
            actor = owner;
            walker = movement;
            fainted = false;
            combatRemaining = 0f;
            SetOpacity(1f);
            art = FindArtwork(owner.Id);
            if (art == null) { Debug.LogError("Missing survivor artwork for " + owner.Id, this); return; }
            owner.PlaceStatusAboveHead(art.standing, image.transform, walker.nameLabel);
            if (walker.visual) walker.visual.gameObject.SetActive(false);
            Refresh(0f);
        }

        public void PlayAttack(float targetX) => PlayCombat(Pose.Attack, targetX);
        public void PlayHit(float sourceX) => PlayCombat(Pose.Hit, sourceX);

        void PlayCombat(Pose pose, float targetX)
        {
            if (art == null || !actor.CanAct) return;
            var motion = art.Get(pose);
            if (motion?.firstFrame == null) return;
            combatPose = pose;
            combatFacesLeft = targetX < transform.position.x;
            combatRemaining = motion.clip ? motion.clip.length : .1f;
            playback.Restart();
        }

        void SetOpacity(float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        void LateUpdate()
        {
            if (art != null && !walker.AnimationPaused) Refresh(Time.deltaTime);
        }

        void Refresh(float dt)
        {
            var assignedRoom = walker.TryGetAssignedWorkstation(out var station) ? walker.CurrentRoom : null;
            bool canWork = assignedRoom && walker.CanDoDevelopmentWork;
            if (!canWork) waitingRoom = assignedRoom;
            var room = canWork && station.IsAt(transform.position.x) ? assignedRoom : null;
            if (workingRoom != room)
            {
                playWorkIntro = room && room != waitingRoom;
                workingRoom = room;
                seatedTime = 0f;
                if (room) waitingRoom = null;
            }

            var idle = actor.IsTired ? Pose.TiredIdle : Pose.Idle;
            Pose pose = idle;
            if (!actor.CanAct)
            {
                combatRemaining = 0f;
                if (!fainted)
                {
                    fainted = true;
                    faintElapsed = 0f;
                    playback.Restart();
                }
                if (faintElapsed < FaintFadeDuration)
                {
                    faintElapsed = Mathf.Min(FaintFadeDuration, faintElapsed + dt);
                    float blend = faintElapsed * 2f / FaintFadeDuration;
                    SetOpacity(Mathf.Abs(1f - blend));
                    if (blend < 1f) return; // Hold the last visible frame during fade-out.
                }
                pose = Pose.Faint;
            }
            else if (combatRemaining > 0f) pose = combatPose;
            else if (!walker.IsInElevator && Mathf.Abs(walker.Velocity.x) > 0.01f)
                pose = actor.IsTired ? Pose.TiredWalk : Pose.Walk;
            else if (workingRoom)
            {
                var sit = art.Get(Pose.Sit);
                bool hasTyping = art.Get(Pose.Type)?.firstFrame != null;
                bool entering = playWorkIntro && (seatedTime == 0f || (sit.clip && seatedTime < sit.clip.length));
                bool sitting = sit?.firstFrame != null && (!hasTyping || entering);
                pose = sitting ? Pose.Sit : Pose.Type;
                seatedTime += dt;
            }
            else if (walker.InTent)
                pose = Pose.Sleep;

            bool working = pose == Pose.Sit || pose == Pose.Type;
            if (fainted && actor.CanAct)
            {
                fainted = false;
                SetOpacity(1f);
            }
            if (pose != Pose.Faint)
                image.flipX = pose == Pose.Attack || pose == Pose.Hit ? combatFacesLeft :
                    working && art.Get(pose)?.firstFrame != null ? station.flipArtwork : walker.FacingLeft;
            combatRemaining = Mathf.Max(0f, combatRemaining - dt);
            playback.Sample(art, pose, idle, image, dt);
        }
    }
}
