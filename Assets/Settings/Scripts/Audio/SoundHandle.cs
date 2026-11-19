namespace GameJam.Audio
{
    /// <summary>Identifies one playback, even after its pooled AudioSource is reused.</summary>
    public readonly struct SoundHandle
    {
        private readonly SoundManager owner;
        private readonly int slot;
        private readonly ulong generation;

        internal SoundHandle(SoundManager owner, int slot, ulong generation)
        {
            this.owner = owner;
            this.slot = slot;
            this.generation = generation;
        }

        /// <summary>True while playing or paused; false after completion, stop or stealing.</summary>
        public bool IsValid => owner != null && owner.IsHandleValid(slot, generation);
        public void Stop() { if (owner != null) owner.StopHandle(slot, generation); }
    }
}
