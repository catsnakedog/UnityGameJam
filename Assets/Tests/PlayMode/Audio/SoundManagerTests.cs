using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameJam.Audio.Tests
{
    public sealed class SoundManagerTests
    {
        private SoundManager manager;
        private AudioClip clip, other;
        private float oldTimeScale;
        private bool oldListenerPause;
        private readonly List<Object> temporary = new List<Object>();
        private readonly string[] keys = { "Master", "Music", "Sfx", "UI", "Ambience", "Muted" };
        private readonly Dictionary<string, float> savedPrefs = new Dictionary<string, float>();

        [SetUp]
        public void SetUp()
        {
            oldTimeScale = Time.timeScale;
            oldListenerPause = AudioListener.pause;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            savedPrefs.Clear();
            foreach (string key in keys)
                if (PlayerPrefs.HasKey("GameJam.Audio." + key))
                    savedPrefs[key] = key == "Muted" ? PlayerPrefs.GetInt("GameJam.Audio." + key) : PlayerPrefs.GetFloat("GameJam.Audio." + key);
            if (SoundManager.TryGetInstance(out SoundManager previous)) Object.DestroyImmediate(previous.gameObject);
            manager = SoundManager.Instance;
            SetField("pauseOnFocusLoss", false);
            manager.SetPaused(false);
            manager.SetMasterVolume(1f);
            manager.SetMusicVolume(1f);
            manager.SetSfxVolume(1f);
            manager.SetUIVolume(1f);
            manager.SetAmbienceVolume(1f);
            manager.SetMuted(false);
            clip = AudioClip.Create("Audio test A", 44100 * 3, 1, 44100, false);
            other = AudioClip.Create("Audio test B", 44100 * 3, 1, 44100, false);
            temporary.Add(clip);
            temporary.Add(other);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = oldTimeScale;
            AudioListener.pause = oldListenerPause;
            if (manager != null) Object.DestroyImmediate(manager.gameObject);
            foreach (Object item in temporary) if (item != null) Object.DestroyImmediate(item);
            temporary.Clear();
            foreach (string key in keys)
            {
                string full = "GameJam.Audio." + key;
                if (!savedPrefs.TryGetValue(key, out float value)) PlayerPrefs.DeleteKey(full);
                else if (key == "Muted") PlayerPrefs.SetInt(full, (int)value);
                else PlayerPrefs.SetFloat(full, value);
            }
            PlayerPrefs.Save();
        }

        private void SetField(string name, object value)
            => typeof(SoundManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, value);
        private AudioSource[] Sources => manager.GetComponentsInChildren<AudioSource>();
        private SoundCue Cue()
        {
            SoundCue cue = ScriptableObject.CreateInstance<SoundCue>();
            cue.clips = new[] { clip };
            temporary.Add(cue);
            return cue;
        }

        [Test]
        public void AutoBootstrapLoadsConfiguredPrefab()
        {
            Assert.NotNull(Resources.Load<SoundManager>(SoundManager.ResourceName));
            Assert.AreSame(manager, SoundManager.Instance);
            Assert.AreEqual("DontDestroyOnLoad", manager.gameObject.scene.name);
            Assert.GreaterOrEqual(manager.VoiceCapacity, 1);
        }

        [UnityTest]
        public IEnumerator DuplicateManagerDoesNotReplaceSingleton()
        {
            GameObject duplicate = new GameObject("Duplicate manager test");
            temporary.Add(duplicate);
            duplicate.AddComponent<SoundManager>();
            yield return null;
            Assert.AreSame(manager, SoundManager.Instance);
            Assert.IsNull(duplicate.GetComponent<SoundManager>());
        }

        [Test]
        public void EmptyClipsAreSafeAndDoNotConsumeVoices()
        {
            Assert.IsFalse(manager.PlaySfx(null).IsValid);
            SoundCue cue = Cue();
            cue.clips = new AudioClip[] { null, null };
            Assert.IsFalse(manager.Play(cue).IsValid);
            manager.PlayMusic(null);
            Assert.AreEqual(0, manager.ActiveVoiceCount);
        }

        [Test]
        public void OldHandleCannotStopReusedSource()
        {
            SoundHandle old = manager.PlaySfx(clip);
            old.Stop();
            SoundHandle current = manager.PlaySfx(other);
            old.Stop();
            Assert.IsFalse(old.IsValid);
            Assert.IsTrue(current.IsValid);
            Assert.AreEqual(1, manager.ActiveVoiceCount);
        }

        [Test]
        public void PoolStealsOldestEligibleVoiceAndProtectsImportantVoices()
        {
            SetField("maxVoices", manager.VoiceCapacity);
            SoundHandle oldest = manager.PlayClip(clip, priority: 200);
            for (int i = 1; i < manager.VoiceCapacity; i++) manager.PlayClip(clip, priority: 10);
            Assert.IsFalse(manager.PlayClip(other, priority: 250).IsValid);
            SoundHandle incoming = manager.PlayClip(other, priority: 100);
            Assert.IsTrue(incoming.IsValid);
            Assert.IsFalse(oldest.IsValid);
            Assert.AreEqual(manager.VoiceCapacity, manager.ActiveVoiceCount);
        }

        [Test]
        public void FullLoopPoolRejectsPlaybackInsteadOfBreakingLoops()
        {
            SetField("maxVoices", manager.VoiceCapacity);
            for (int i = 0; i < manager.VoiceCapacity; i++) manager.PlayClip(clip, loop: true);
            Assert.IsFalse(manager.PlaySfx(other).IsValid);
            Assert.AreEqual(manager.VoiceCapacity, manager.ActiveVoiceCount);
        }

        [UnityTest]
        public IEnumerator PausePreservesEffectsWhileUIContinues()
        {
            SoundHandle effect = manager.PlaySfx(clip);
            manager.SetPaused(true);
            SoundHandle duringPause = manager.PlaySfx(other);
            SoundHandle ui = manager.PlayUI(other);
            yield return null;
            Assert.IsTrue(effect.IsValid);
            Assert.IsTrue(duringPause.IsValid);
            Assert.IsTrue(ui.IsValid);
            Assert.AreEqual(1, Sources.Count(source => source.isPlaying));
            manager.SetPaused(false);
            yield return null;
            Assert.AreEqual(3, Sources.Count(source => source.isPlaying));
        }

        [UnityTest]
        public IEnumerator ListenerPauseDoesNotRecyclePausedEffects()
        {
            SoundHandle handle = manager.PlaySfx(clip);
            AudioListener.pause = true;
            yield return null;
            Assert.IsTrue(handle.IsValid);
            AudioListener.pause = false;
            yield return null;
            Assert.IsTrue(handle.IsValid);
        }

        [UnityTest]
        public IEnumerator CompletedOneShotReturnsToPool()
        {
            AudioClip shortClip = AudioClip.Create("Short audio test", 2205, 1, 44100, false);
            temporary.Add(shortClip);
            SoundHandle handle = manager.PlaySfx(shortClip);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsFalse(handle.IsValid);
            Assert.AreEqual(0, manager.ActiveVoiceCount);
        }

        [Test]
        public void ChannelVolumesAndMuteApplyToAlreadyPlayingSources()
        {
            manager.PlaySfx(clip, 0.8f);
            manager.PlayUI(other, 0.6f);
            manager.SetMasterVolume(0.5f);
            manager.SetSfxVolume(0.25f);
            Assert.That(Sources.Single(source => source.clip == clip).volume, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(Sources.Single(source => source.clip == other).volume, Is.EqualTo(0.3f).Within(0.001f));
            manager.SetMuted(true);
            Assert.IsTrue(Sources.Where(source => source.clip != null).All(source => source.volume == 0f));
            manager.SetMuted(false);
            Assert.That(Sources.Single(source => source.clip == clip).volume, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void SettingsSurviveManagerRecreationAndClampInvalidInput()
        {
            manager.SetMasterVolume(0.37f);
            manager.SetMusicVolume(0.43f);
            manager.SetMuted(true);
            manager.SaveSettings();
            Object.DestroyImmediate(manager.gameObject);
            manager = SoundManager.Instance;
            Assert.That(manager.MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(manager.MusicVolume, Is.EqualTo(0.43f).Within(0.001f));
            Assert.IsTrue(manager.Muted);
            manager.SetSfxVolume(-1f);
            manager.SetUIVolume(2f);
            manager.SetAmbienceVolume(float.NaN);
            Assert.AreEqual(0f, manager.SfxVolume);
            Assert.AreEqual(1f, manager.UIVolume);
            Assert.AreEqual(0f, manager.AmbienceVolume);
        }

        [UnityTest]
        public IEnumerator MusicCrossfadeWorksAtZeroTimeScaleAndCanBeInterrupted()
        {
            manager.PlayMusic(clip, 0f);
            Time.timeScale = 0f;
            manager.PlayMusic(other, 0.1f);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(other, manager.CurrentMusic);
            Assert.AreEqual(1, Sources.Count(source => source.clip != null));
            Assert.That(Sources.Single(source => source.clip == other).volume, Is.EqualTo(1f).Within(0.001f));
            manager.StopMusic(1f);
            manager.PlayMusic(clip, 0f);
            Assert.AreEqual(clip, manager.CurrentMusic);
            Assert.AreEqual(1, Sources.Count(source => source.clip != null));
            manager.StopMusic(0f);
            Assert.IsNull(manager.CurrentMusic);
            Assert.AreEqual(0, Sources.Count(source => source.clip != null));
        }

        [UnityTest]
        public IEnumerator ReplayingCurrentMusicDoesNotRestartTrack()
        {
            manager.PlayMusic(clip, 0f);
            yield return new WaitForSecondsRealtime(0.12f);
            AudioSource source = Sources.Single(item => item.clip == clip);
            int before = source.timeSamples;
            Assert.Greater(before, 0);
            manager.PlayMusic(clip, 0f);
            Assert.GreaterOrEqual(source.timeSamples, before);
        }

        [UnityTest]
        public IEnumerator SilentMusicKeepsItsPlaybackPositionWhenVolumeIsRestored()
        {
            manager.PlayMusic(clip, 0f, 0f);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(clip, manager.CurrentMusic);
            AudioSource source = Sources.Single(item => item.clip == clip);
            int before = source.timeSamples;
            Assert.Greater(before, 0);
            manager.PlayMusic(clip, 0f, 1f);
            Assert.GreaterOrEqual(source.timeSamples, before);
            Assert.AreEqual(1f, source.volume);
        }

        [UnityTest]
        public IEnumerator PausingMusicFreezesCrossfadeUntilResumed()
        {
            manager.PlayMusic(clip, 0f);
            manager.SetPaused(true);
            manager.PlayMusic(other, 0.1f);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(2, Sources.Count(source => source.clip != null));
            Assert.AreEqual(0f, Sources.Single(source => source.clip == other).volume);
            manager.SetPaused(false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(1, Sources.Count(source => source.clip != null));
            Assert.AreEqual(other, manager.CurrentMusic);
        }

        [UnityTest]
        public IEnumerator CueCooldownAndInstanceLimitPreventSpam()
        {
            SoundCue cue = Cue();
            cue.cooldown = 0.1f;
            cue.maxInstances = 1;
            SoundHandle first = manager.Play(cue);
            Assert.IsTrue(first.IsValid);
            first.Stop();
            Assert.IsFalse(manager.Play(cue).IsValid);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.IsTrue(manager.Play(cue).IsValid);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.IsFalse(manager.Play(cue).IsValid);
        }

        [UnityTest]
        public IEnumerator FollowingSoundStopsWhenOwnerIsDestroyed()
        {
            GameObject target = new GameObject("Audio follow test");
            temporary.Add(target);
            SoundHandle handle = manager.PlayClip(clip, loop: true, follow: target.transform, spatialBlend: 1f);
            target.transform.position = new Vector3(2, 3, 4);
            yield return null;
            Assert.AreEqual(target.transform.position, Sources.Single(source => source.clip == clip).transform.position);
            Object.Destroy(target);
            yield return null;
            yield return null;
            Assert.IsFalse(handle.IsValid);
        }

        [UnityTest]
        public IEnumerator ActiveSceneChangeKeepsMusicAndClearsEffects()
        {
            Scene original = SceneManager.GetActiveScene();
            Scene next = SceneManager.CreateScene("Audio scene transition test");
            manager.PlayMusic(clip, 0f);
            SoundHandle effect = manager.PlaySfx(other);
            try
            {
                Assert.IsTrue(SceneManager.SetActiveScene(next));
                Assert.AreSame(manager, SoundManager.Instance);
                Assert.AreEqual(clip, manager.CurrentMusic);
                Assert.IsFalse(effect.IsValid);
            }
            finally { SceneManager.SetActiveScene(original); }
            yield return SceneManager.UnloadSceneAsync(next);
        }
    }
}
