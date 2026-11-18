using System;

namespace UnityGameJam.GameDev
{
    /// <summary>Immutable run state in percentage points. This is not a score.</summary>
    public readonly struct GameDevProgress
    {
        public const float MaximumPercent = 100f;
        public float Planning { get; }
        public float Programming { get; }
        public float Graphics { get; }
        public bool IsComplete => Planning == MaximumPercent && Programming == MaximumPercent && Graphics == MaximumPercent;

        public GameDevProgress(float planning, float programming, float graphics)
        {
            Planning = Clamp(planning);
            Programming = Clamp(programming);
            Graphics = Clamp(graphics);
        }

        public float Get(GameDevField field)
        {
            switch (field)
            {
                case GameDevField.Planning: return Planning;
                case GameDevField.Programming: return Programming;
                case GameDevField.Graphics: return Graphics;
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }

        public GameDevProgress With(GameDevField field, float value)
        {
            switch (field)
            {
                case GameDevField.Planning: return new GameDevProgress(value, Programming, Graphics);
                case GameDevField.Programming: return new GameDevProgress(Planning, value, Graphics);
                case GameDevField.Graphics: return new GameDevProgress(Planning, Programming, value);
                default: throw new ArgumentOutOfRangeException(nameof(field));
            }
        }

        static float Clamp(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Progress must be finite.");
            return Math.Max(0f, Math.Min(MaximumPercent, value));
        }
    }
}
