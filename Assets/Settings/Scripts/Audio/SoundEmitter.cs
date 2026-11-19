using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Audio
{
    /// <summary>Connect Play/Stop to a Button, animation event or other UnityEvent.</summary>
    [AddComponentMenu("Audio/Sound Emitter")]
    public sealed class SoundEmitter : MonoBehaviour
    {
        [SerializeField] private SoundCue cue;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool followTransform;
        [Tooltip("Loops always stop on disable. Enable this to also stop one-shots.")]
        [SerializeField] private bool stopOnDisable;
        private readonly List<SoundHandle> owned = new List<SoundHandle>();
        private readonly List<SoundHandle> loops = new List<SoundHandle>();

        private void OnEnable() { if (playOnEnable) Play(); }
        public void Play()
        {
            if (cue == null || SoundManager.Instance == null) return;
            owned.RemoveAll(handle => !handle.IsValid);
            loops.RemoveAll(handle => !handle.IsValid);
            SoundHandle playback = SoundManager.Instance.Play(cue, transform.position, followTransform ? transform : null);
            if (!playback.IsValid) return;
            owned.Add(playback);
            if (cue.loop) loops.Add(playback);
        }
        public void Stop()
        {
            foreach (SoundHandle handle in owned) handle.Stop();
            owned.Clear();
            loops.Clear();
        }
        private void OnDisable()
        {
            if (stopOnDisable) Stop();
            else { foreach (SoundHandle handle in loops) handle.Stop(); loops.Clear(); }
        }
    }
}
