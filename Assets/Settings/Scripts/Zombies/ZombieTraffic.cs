using UnityEngine;

namespace UnityGameJam.Zombies
{
    public static class ZombieTraffic
    {
        // Sweep the whole segment, so a long frame cannot tunnel through a waiting zombie.
        public static float SafeFraction(Vector2 from, Vector2 to, Vector2 obstacle, float spacing)
        {
            Vector2 delta = to - from, offset = from - obstacle;
            float a = delta.sqrMagnitude, b = Vector2.Dot(offset, delta);
            if (a < 0.0000001f || b >= 0) return 1f; // Stationary, tangent, or moving away.
            float c = offset.sqrMagnitude - spacing * spacing;
            if (c <= 0) return 0;
            float discriminant = b * b - a * c;
            if (discriminant < 0) return 1f;
            return Mathf.Clamp01((-b - Mathf.Sqrt(discriminant)) / a);
        }
    }
}
