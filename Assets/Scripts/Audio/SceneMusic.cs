using UnityEngine;

namespace GameJam.Audio
{
    [AddComponentMenu("Audio/Scene Music")]
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(0f)] private float fadeSeconds = 0.5f;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool loop = true;
        private void Start() { Play(); }
        public void Play()
        {
            if (clip != null && SoundManager.Instance != null)
                SoundManager.Instance.PlayMusic(clip, fadeSeconds, volume, loop);
        }
        // Music intentionally persists until another scene requests a track or StopMusic.
    }
}
