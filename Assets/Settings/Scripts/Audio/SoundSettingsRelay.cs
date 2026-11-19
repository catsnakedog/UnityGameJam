using UnityEngine;

namespace GameJam.Audio
{
    /// <summary>Scene-local UnityEvent targets for settings sliders/toggles and pause menus.</summary>
    [AddComponentMenu("Audio/Sound Settings Relay")]
    public sealed class SoundSettingsRelay : MonoBehaviour
    {
        public void SetMasterVolume(float value) => SoundManager.Instance?.SetMasterVolume(value);
        public void SetMusicVolume(float value) => SoundManager.Instance?.SetMusicVolume(value);
        public void SetSfxVolume(float value) => SoundManager.Instance?.SetSfxVolume(value);
        public void SetUIVolume(float value) => SoundManager.Instance?.SetUIVolume(value);
        public void SetAmbienceVolume(float value) => SoundManager.Instance?.SetAmbienceVolume(value);
        public void SetMuted(bool value) => SoundManager.Instance?.SetMuted(value);
        public void SetPaused(bool value) => SoundManager.Instance?.SetPaused(value);
    }
}
