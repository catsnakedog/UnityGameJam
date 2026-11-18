using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace GameJam.Audio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Audio/Sound Manager")]
    public sealed class SoundManager : MonoBehaviour
    {
        public const string ResourceName = "SoundManager";
        private const string PrefsKey = "GameJam.Audio.";
        private static SoundManager instance;
        private static bool quitting;

        [Header("Voice pool")]
        [SerializeField, Min(1)] private int initialVoices = 8;
        [SerializeField, Min(1)] private int maxVoices = 32;
        [SerializeField] private bool stopEffectsOnSceneChange = true;
        [SerializeField] private bool pauseOnFocusLoss = true;
        [Header("Default volumes (saved player settings take precedence)")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.8f;
        [SerializeField] private bool muted;
        [Header("Optional mixer routing")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;

        private sealed class Voice
        {
            public AudioSource source;
            public SoundCue cue;
            public Transform follow;
            public bool following, active, paused;
            public float gain;
            public SoundChannel channel;
            public ulong generation;
        }

        private sealed class MusicVoice
        {
            public AudioSource source;
            public float gain, from, target;
            public bool paused;
        }

        private readonly List<Voice> voices = new List<Voice>();
        private readonly Dictionary<SoundCue, double> cooldowns = new Dictionary<SoundCue, double>();
        private readonly MusicVoice[] music = new MusicVoice[2];
        private int currentMusic = -1;
        private float fadeElapsed, fadeDuration;
        private ulong nextGeneration;
        private bool initialized, userPaused, focusLost, appPaused, settingsDirty;
        private double saveAt;

        public static SoundManager Instance
        {
            get
            {
                if (quitting || !Application.isPlaying) return null;
                if (instance != null) return instance;
                instance = FindFirstObjectByType<SoundManager>();
                if (instance != null) { instance.Initialize(); return instance; }
                SoundManager prefab = Resources.Load<SoundManager>(ResourceName);
                if (prefab != null) Instantiate(prefab).name = ResourceName;
                else new GameObject(ResourceName).AddComponent<SoundManager>();
                return instance;
            }
        }

        public static bool TryGetInstance(out SoundManager manager)
        {
            manager = instance;
            return manager != null && !quitting;
        }

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public float UIVolume => uiVolume;
        public float AmbienceVolume => ambienceVolume;
        public bool Muted => muted;
        public bool IsPaused => userPaused;
        public AudioClip CurrentMusic => currentMusic < 0 ? null : music[currentMusic].source.clip;
        public int ActiveVoiceCount { get { int count = 0; foreach (Voice v in voices) if (v.active) count++; return count; } }
        public int VoiceCapacity => voices.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; quitting = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { _ = Instance; }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            maxVoices = Mathf.Max(1, maxVoices);
            initialVoices = Mathf.Clamp(initialVoices, 1, maxVoices);
            for (int i = 0; i < 2; i++)
                music[i] = new MusicVoice { source = CreateSource("Music " + i, musicGroup) };
            for (int i = 0; i < initialVoices; i++) AddVoice();
            ReloadSettings();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private int AddVoice()
        {
            voices.Add(new Voice { source = CreateSource("Effect " + voices.Count, sfxGroup) });
            return voices.Count - 1;
        }

        public SoundHandle PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
            => PlayClip(clip, SoundChannel.Sfx, volume, pitch);

        public SoundHandle PlayUI(AudioClip clip, float volume = 1f)
            => PlayClip(clip, SoundChannel.UI, volume);

        public SoundHandle PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
            => PlayClip(clip, SoundChannel.Sfx, volume, position: position, spatialBlend: 1f);

        public SoundHandle PlayClip(AudioClip clip, SoundChannel channel = SoundChannel.Sfx,
            float volume = 1f, float pitch = 1f, bool loop = false, Vector3 position = default,
            Transform follow = null, float spatialBlend = 0f, int priority = 128)
            => StartVoice(clip, channel, volume, pitch, loop, position, follow, spatialBlend,
                priority, 1f, 30f, null);

        public SoundHandle Play(SoundCue cue, Vector3 position = default, Transform follow = null)
        {
            if (cue == null) return default;
            ReclaimFinishedVoices();
            if (cooldowns.TryGetValue(cue, out double readyAt) && Time.unscaledTimeAsDouble < readyAt)
                return default;
            if (cue.maxInstances > 0)
            {
                int count = 0;
                foreach (Voice voice in voices) if (voice.active && voice.cue == cue) count++;
                if (count >= cue.maxInstances) return default;
            }
            AudioClip clip = cue.PickClip();
            float low = Mathf.Clamp(Finite(cue.minPitch, 1f), 0.01f, 3f);
            float high = Mathf.Clamp(Finite(cue.maxPitch, low), low, 3f);
            SoundHandle handle = StartVoice(clip, cue.channel, cue.volume, Random.Range(low, high),
                cue.loop, position, follow, cue.spatialBlend, cue.priority,
                cue.minDistance, cue.maxDistance, cue);
            if (handle.IsValid && cue.cooldown > 0f)
                cooldowns[cue] = Time.unscaledTimeAsDouble + Finite(cue.cooldown, 0f);
            return handle;
        }

        private SoundHandle StartVoice(AudioClip clip, SoundChannel channel, float volume, float pitch,
            bool loop, Vector3 position, Transform follow, float spatialBlend, int priority,
            float minDistance, float maxDistance, SoundCue cue)
        {
            if (!initialized || !isActiveAndEnabled || clip == null) return default;
            ReclaimFinishedVoices();
            priority = Mathf.Clamp(priority, 0, 256);
            int index = AcquireVoice(priority);
            if (index < 0) return default;
            Voice voice = voices[index];
            Release(voice);
            voice.active = true;
            voice.generation = ++nextGeneration;
            voice.cue = cue;
            voice.channel = channel;
            voice.gain = Unit(volume);
            voice.follow = follow;
            voice.following = follow != null;
            AudioSource source = voice.source;
            source.clip = clip;
            source.loop = loop;
            source.pitch = Mathf.Clamp(Finite(pitch, 1f), 0.01f, 3f);
            source.priority = priority;
            source.spatialBlend = channel == SoundChannel.UI ? 0f : Unit(spatialBlend);
            source.minDistance = Mathf.Max(0.01f, Finite(minDistance, 1f));
            source.maxDistance = Mathf.Max(source.minDistance, Finite(maxDistance, 30f));
            source.rolloffMode = AudioRolloffMode.Linear;
            source.ignoreListenerPause = channel == SoundChannel.UI;
            source.outputAudioMixerGroup = Group(channel);
            source.transform.position = follow != null ? follow.position : position;
            source.volume = voice.gain * ChannelVolume(channel) * EffectiveMaster;
            source.Play();
            voice.paused = ShouldPause(channel);
            if (voice.paused) source.Pause();
            return new SoundHandle(this, index, voice.generation);
        }

        private int AcquireVoice(int priority)
        {
            for (int i = 0; i < voices.Count; i++) if (!voices[i].active) return i;
            if (voices.Count < maxVoices) return AddVoice();
            int candidate = -1;
            for (int i = 0; i < voices.Count; i++)
            {
                Voice voice = voices[i];
                if (voice.source.loop || voice.source.priority < priority) continue;
                if (candidate < 0 || voice.source.priority > voices[candidate].source.priority ||
                    (voice.source.priority == voices[candidate].source.priority &&
                     voice.generation < voices[candidate].generation)) candidate = i;
            }
            return candidate;
        }

        internal bool IsHandleValid(int slot, ulong generation)
            => slot >= 0 && slot < voices.Count && voices[slot].active && voices[slot].generation == generation;

        internal void StopHandle(int slot, ulong generation)
        {
            if (IsHandleValid(slot, generation)) Release(voices[slot]);
        }

        private static void Release(Voice voice)
        {
            voice.source.Stop();
            voice.source.clip = null;
            voice.active = voice.paused = voice.following = false;
            voice.cue = null;
            voice.follow = null;
        }

        public void StopAllEffects()
        {
            foreach (Voice voice in voices) Release(voice);
        }

        public void StopChannel(SoundChannel channel)
        {
            foreach (Voice voice in voices) if (voice.active && voice.channel == channel) Release(voice);
        }

        public void PlayMusic(AudioClip clip, float fadeSeconds = 0.5f, float volume = 1f, bool loop = true)
        {
            if (clip == null || !initialized || !isActiveAndEnabled) return;
            int target = currentMusic;
            bool same = target >= 0 && music[target].source.clip == clip &&
                (music[target].source.isPlaying || music[target].paused);
            if (!same)
            {
                target = currentMusic == 0 ? 1 : 0;
                MusicVoice voice = music[target];
                voice.source.Stop();
                voice.source.clip = clip;
                voice.gain = 0f;
                voice.source.volume = 0f;
                voice.source.priority = 0;
                voice.source.Play();
                voice.paused = ShouldPause(SoundChannel.Sfx);
                if (voice.paused) voice.source.Pause();
            }
            music[target].source.loop = loop;
            currentMusic = target;
            BeginMusicFade(target, Unit(volume), fadeSeconds);
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
            if (!initialized) return;
            currentMusic = -1;
            BeginMusicFade(-1, 0f, fadeSeconds);
        }

        private void BeginMusicFade(int target, float gain, float seconds)
        {
            fadeElapsed = 0f;
            fadeDuration = Mathf.Max(0f, Finite(seconds, 0f));
            for (int i = 0; i < music.Length; i++)
            {
                music[i].from = music[i].gain;
                music[i].target = i == target ? gain : 0f;
            }
            if (fadeDuration == 0f) UpdateMusicFade(1f);
            ApplyVolumes();
        }

        private void UpdateMusicFade(float t)
        {
            for (int i = 0; i < music.Length; i++)
            {
                MusicVoice voice = music[i];
                voice.gain = Mathf.Lerp(voice.from, voice.target, t);
                if (t >= 1f && i != currentMusic && voice.target == 0f)
                {
                    voice.source.Stop();
                    voice.source.clip = null;
                    voice.paused = false;
                }
            }
        }

        public void SetPaused(bool paused) { userPaused = paused; ApplyPause(); }
        private bool ShouldPause(SoundChannel channel)
            => appPaused || (pauseOnFocusLoss && focusLost) || (userPaused && channel != SoundChannel.UI);

        private void ApplyPause()
        {
            if (!initialized) return;
            foreach (Voice voice in voices)
                if (voice.active) SetSourcePaused(voice.source, ref voice.paused, ShouldPause(voice.channel));
            foreach (MusicVoice voice in music)
                if (voice.source.clip != null)
                    SetSourcePaused(voice.source, ref voice.paused, ShouldPause(SoundChannel.Sfx));
        }

        private static void SetSourcePaused(AudioSource source, ref bool wasPaused, bool pause)
        {
            if (wasPaused == pause) return;
            if (pause) source.Pause(); else source.UnPause();
            wasPaused = pause;
        }

        private void Update()
        {
            if (!initialized) return;
            ReclaimFinishedVoices();
            foreach (Voice voice in voices)
            {
                if (!voice.active || !voice.following) continue;
                if (voice.follow == null) { Release(voice); continue; }
                voice.source.transform.position = voice.follow.position;
            }
            if (fadeDuration > 0f && !ShouldPause(SoundChannel.Sfx) && !AudioListener.pause)
            {
                fadeElapsed = Mathf.Min(fadeDuration, fadeElapsed + Time.unscaledDeltaTime);
                UpdateMusicFade(fadeElapsed / fadeDuration);
                if (fadeElapsed >= fadeDuration) fadeDuration = 0f;
            }
            ApplyVolumes();
            if (settingsDirty && Time.unscaledTimeAsDouble >= saveAt) SaveSettings();
        }

        private void ReclaimFinishedVoices()
        {
            foreach (Voice voice in voices)
                if (voice.active && !voice.paused &&
                    !(AudioListener.pause && !voice.source.ignoreListenerPause) && !voice.source.isPlaying)
                    Release(voice);
        }

        private float EffectiveMaster => muted ? 0f : masterVolume;
        private float ChannelVolume(SoundChannel channel)
            => channel == SoundChannel.UI ? uiVolume : channel == SoundChannel.Ambience ? ambienceVolume : sfxVolume;
        private AudioMixerGroup Group(SoundChannel channel)
            => channel == SoundChannel.UI ? uiGroup : channel == SoundChannel.Ambience ? ambienceGroup : sfxGroup;

        private void ApplyVolumes()
        {
            if (!initialized) return;
            foreach (Voice voice in voices)
                if (voice.active) voice.source.volume = voice.gain * ChannelVolume(voice.channel) * EffectiveMaster;
            foreach (MusicVoice voice in music)
                voice.source.volume = voice.gain * musicVolume * EffectiveMaster;
        }

        public void SetMasterVolume(float value) { masterVolume = Unit(value); SettingsChanged(); }
        public void SetMusicVolume(float value) { musicVolume = Unit(value); SettingsChanged(); }
        public void SetSfxVolume(float value) { sfxVolume = Unit(value); SettingsChanged(); }
        public void SetUIVolume(float value) { uiVolume = Unit(value); SettingsChanged(); }
        public void SetAmbienceVolume(float value) { ambienceVolume = Unit(value); SettingsChanged(); }
        public void SetMuted(bool value) { muted = value; SettingsChanged(); }

        private void SettingsChanged()
        {
            PlayerPrefs.SetFloat(PrefsKey + "Master", masterVolume);
            PlayerPrefs.SetFloat(PrefsKey + "Music", musicVolume);
            PlayerPrefs.SetFloat(PrefsKey + "Sfx", sfxVolume);
            PlayerPrefs.SetFloat(PrefsKey + "UI", uiVolume);
            PlayerPrefs.SetFloat(PrefsKey + "Ambience", ambienceVolume);
            PlayerPrefs.SetInt(PrefsKey + "Muted", muted ? 1 : 0);
            settingsDirty = true;
            saveAt = Time.unscaledTimeAsDouble + 0.5;
            ApplyVolumes();
        }

        public void SaveSettings() { if (settingsDirty) PlayerPrefs.Save(); settingsDirty = false; }

        public void ReloadSettings()
        {
            masterVolume = Unit(PlayerPrefs.GetFloat(PrefsKey + "Master", masterVolume));
            musicVolume = Unit(PlayerPrefs.GetFloat(PrefsKey + "Music", musicVolume));
            sfxVolume = Unit(PlayerPrefs.GetFloat(PrefsKey + "Sfx", sfxVolume));
            uiVolume = Unit(PlayerPrefs.GetFloat(PrefsKey + "UI", uiVolume));
            ambienceVolume = Unit(PlayerPrefs.GetFloat(PrefsKey + "Ambience", ambienceVolume));
            muted = PlayerPrefs.GetInt(PrefsKey + "Muted", muted ? 1 : 0) != 0;
            ApplyVolumes();
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            if (stopEffectsOnSceneChange) StopAllEffects();
            cooldowns.Clear();
        }

        private void OnApplicationFocus(bool focused)
        {
            focusLost = !focused;
            ApplyPause();
            if (!focused) SaveSettings();
        }

        private void OnApplicationPause(bool paused)
        {
            appPaused = paused;
            ApplyPause();
            if (paused) SaveSettings();
        }

        private void OnApplicationQuit() { if (instance == this) quitting = true; SaveSettings(); }
        private void OnDisable()
        {
            if (!initialized) return;
            StopAllEffects();
            StopMusic(0f);
            SaveSettings();
        }
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (instance == this) instance = null;
        }

        private static float Finite(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        private static float Unit(float value) => Mathf.Clamp01(Finite(value, 0f));
    }
}
