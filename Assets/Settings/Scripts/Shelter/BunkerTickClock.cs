using System;

namespace UnityGameJam.Shelter
{
    public sealed class BunkerTickClock
    {
        readonly BunkerRules rules;
        double elapsed;
        bool advancing;
        public int CurrentTick { get; private set; }
        public double FractionalTick => CurrentTick + Math.Min(1, elapsed / rules.SecondsPerTick);
        public bool IsComplete => CurrentTick >= rules.TotalTicks;
        public bool Paused { get; set; }
        public event Action<int> Tick;
        public BunkerTickClock(BunkerRules rules) { rules.Validate(); this.rules = rules; }
        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (advancing || Paused || IsComplete) return;
            rules.Validate();
            advancing = true;
            try
            {
                elapsed += seconds;
                while (!Paused && !IsComplete && elapsed >= rules.SecondsPerTick)
                {
                    elapsed -= rules.SecondsPerTick;
                    CurrentTick++;
                    Tick?.Invoke(CurrentTick);
                }
                if (IsComplete) elapsed = 0;
            }
            finally { advancing = false; }
        }
    }
}
