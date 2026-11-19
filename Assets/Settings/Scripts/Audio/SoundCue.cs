using UnityEngine;

namespace GameJam.Audio
{
    public enum SoundChannel { Sfx, UI, Ambience }

    /// <summary>A reusable sound definition. Assign clips in the Inspector.</summary>
    [CreateAssetMenu(menuName = "Audio/Sound Cue", fileName = "SoundCue")]
    public sealed class SoundCue : ScriptableObject
    {
        public AudioClip[] clips = new AudioClip[0];
        public SoundChannel channel = SoundChannel.Sfx;
        [Range(0f, 1f)] public float volume = 1f;
        [Min(0.01f)] public float minPitch = 1f;
        [Min(0.01f)] public float maxPitch = 1f;
        public bool loop;
        [Tooltip("Smaller values are more important. Loops are never stolen by the pool.")]
        [Range(0, 256)] public int priority = 128;
        [Min(0f)] public float cooldown;
        [Tooltip("0 allows any number, up to the manager's pool limit.")]
        [Min(0)] public int maxInstances;
        [Range(0f, 1f)] public float spatialBlend;
        [Min(0.01f)] public float minDistance = 1f;
        [Min(0.01f)] public float maxDistance = 30f;

        internal AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            // Reservoir sampling ignores empty slots without allocating a temporary array.
            AudioClip selected = null;
            int count = 0;
            foreach (AudioClip clip in clips)
                if (clip != null && Random.Range(0, ++count) == 0) selected = clip;
            return selected;
        }
    }
}
