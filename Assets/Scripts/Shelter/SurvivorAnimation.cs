using System;
using UnityEngine;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    [DisallowMultipleComponent]
    public sealed class SurvivorAnimation : MonoBehaviour
    {
        public enum Pose { Idle, TiredIdle, Walk, TiredWalk, Sit, Type, Sleep, Faint, RangedAttack }

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
                var clip = motion?.clip;
                if (current != clip) { current = clip; elapsed = 0f; }
                if (clip && clip.length > 0f)
                {
                    // Keep the last faint frame until recovery. Other loops use the authored key times.
                    float time = pose == Pose.Faint || pose == Pose.RangedAttack
                        ? Mathf.Min(elapsed, Mathf.Max(0f, clip.length - 0.5f / clip.frameRate))
                        : Mathf.Repeat(elapsed, clip.length);
                    clip.SampleAnimation(image.gameObject, time);
                    elapsed += dt;
                }
                else image.sprite = motion?.firstFrame ? motion.firstFrame : artwork.standing;
            }
        }

        [SerializeField] SpriteRenderer image;
        [SerializeField] Artwork[] characters;
        CharacterIdentity actor;
        ShelterWalker walker;
        Artwork art;
        ShelterArea workingRoom;
        float seatedTime, shotRemaining;
        bool shotFacesLeft;
        Vector3 standingPosition;
        Quaternion standingRotation;
        bool wasCarried;
        readonly Playback playback = new Playback();

        public Artwork FindArtwork(string characterId) => Array.Find(characters, entry => entry.characterId == characterId);

        public void Bind(CharacterIdentity owner, ShelterWalker movement)
        {
            actor = owner;
            walker = movement;
            standingPosition = image.transform.localPosition;
            standingRotation = image.transform.localRotation;
            art = FindArtwork(owner.Id);
            if (art == null) { Debug.LogError("Missing survivor artwork for " + owner.Id, this); return; }
            if (walker.visual) walker.visual.gameObject.SetActive(false);
            Refresh(0f);
        }

        public void PlayRangedAttack(float targetX)
        {
            if (art == null || !actor.CanAct) return;
            var attack = art.Get(Pose.RangedAttack);
            if (attack?.firstFrame == null) return;
            shotFacesLeft = targetX < transform.position.x;
            shotRemaining = attack.clip ? attack.clip.length : Time.deltaTime;
            playback.Restart();
        }

        void LateUpdate()
        {
            if (art != null && !walker.AnimationPaused) Refresh(Time.deltaTime);
        }

        void Refresh(float dt)
        {
            var room = walker.CanAnimateDevelopment ? walker.CurrentRoom : null;
            if (workingRoom != room) { workingRoom = room; seatedTime = 0f; }

            var idle = actor.IsTired ? Pose.TiredIdle : Pose.Idle;
            Pose pose = idle;
            if (!actor.CanAct)
            {
                pose = Pose.Faint;
                shotRemaining = 0f;
            }
            else if (shotRemaining > 0f) pose = Pose.RangedAttack;
            else if (!walker.IsInElevator && Mathf.Abs(walker.Velocity.x) > 0.01f)
                pose = actor.IsTired ? Pose.TiredWalk : Pose.Walk;
            else if (workingRoom)
            {
                var sit = art.Get(Pose.Sit);
                bool hasTyping = art.Get(Pose.Type)?.firstFrame != null;
                bool sitting = sit?.firstFrame != null && (!hasTyping || seatedTime == 0f || (sit.clip && seatedTime < sit.clip.length));
                pose = sitting ? Pose.Sit : Pose.Type;
                seatedTime += dt;
            }
            else if (walker.CanWorkInCurrentRoom && walker.CurrentRoom.roomType == ShelterRoomType.Sleeping)
                pose = Pose.Sleep;

            image.flipX = pose == Pose.RangedAttack ? shotFacesLeft : walker.FacingLeft;
            shotRemaining = Mathf.Max(0f, shotRemaining - dt);
            playback.Sample(art, pose, idle, image, dt);
            // With only a standing drawing available, show the patient lying across the rescuer's arms.
            bool carried = walker.IsBeingCarried && art.Get(Pose.Faint)?.firstFrame == null;
            if (carried && image.sprite)
            {
                image.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                image.transform.localPosition = -(image.transform.localRotation * image.sprite.bounds.center);
            }
            else if (wasCarried)
            {
                image.transform.localPosition = standingPosition;
                image.transform.localRotation = standingRotation;
            }
            wasCarried = carried;
        }
    }
}
