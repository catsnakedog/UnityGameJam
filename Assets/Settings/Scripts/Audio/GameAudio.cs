using UnityEngine;

namespace GameJam.Audio
{
    // Values match the numbered rows in the sound table and the SoundManager prefab.
    public enum GameSound
    {
        Title = 1, Prologue = 2, Farming = 3, Bunker = 4, Victory = 5, Fail = 6,
        Pickup = 7, Drop = 8, Wrong = 9, Click = 10, Laptop = 11, Hour = 12,
        Progress = 13, Sleep = 14, Wake = 15, Faint = 16, Alarm = 17, DoorBust = 18,
        MeleeHit = 19, MeleeMiss = 20, ZombieDown = 21, FarmPickup = 22, Tick = 23,
        Hatch = 24, Stamp = 25, Eat = 26, ZombieSteps = 27, CanThrow = 28,
        CanHit = 29, CanMiss = 30, DoorBang = 31, Defend = 32
    }

    public static class GameAudio
    {
        public static SoundHandle Play(GameSound id, Transform follow = null, float pitch = 1f)
        {
            var manager = SoundManager.Instance;
            var cue = manager ? manager.Cue(id) : null;
            if (!cue || cue.clips.Length == 0) return default;
            if (id == GameSound.Progress)
            {
                if (manager.ProgressFrame == Time.frameCount) return default;
                manager.ProgressFrame = Time.frameCount;
            }
            return manager.Play(cue, follow ? follow.position : default, follow, pitch);
        }

        public static void Click() => Play(GameSound.Click);

        public static void Music(GameSound id)
        {
            var manager = SoundManager.Instance;
            var cue = manager ? manager.Cue(id) : null;
            if (cue && cue.clips.Length > 0) manager.PlayMusic(cue.clips[0], volume: cue.volume, loop: cue.loop);
        }

        public static void DuckBunker(bool duck)
        {
            if (!SoundManager.TryGetInstance(out var manager)) return;
            var cue = manager.Cue(GameSound.Bunker);
            if (cue && cue.clips.Length > 0 && manager.CurrentMusic == cue.clips[0])
                manager.PlayMusic(cue.clips[0], .2f, cue.volume * (duck ? .4f : 1f), cue.loop);
        }
    }
}
