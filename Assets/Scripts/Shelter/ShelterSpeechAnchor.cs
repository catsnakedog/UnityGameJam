using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterSpeechAnchor : MonoBehaviour
    {
        internal static readonly Vector2 DefaultOffset = new Vector2(0.55f, 1.8f);
        [Tooltip("Distance right and up from the speaker's feet to the bubble tail, in world units.")]
        [SerializeField] Vector2 offset = DefaultOffset;
        internal Vector2 Offset => offset;
    }
}
