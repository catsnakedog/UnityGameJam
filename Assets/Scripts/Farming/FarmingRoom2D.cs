using UnityEngine;
using UnityEngine.Tilemaps;
namespace ShelterRun
{
    public sealed class FarmingRoom2D : MonoBehaviour
    {
        [SerializeField] Rect footprint;
        [SerializeField] Tilemap cover;
        [SerializeField] Tilemap[] walls;
        [SerializeField] Transform interior;
        SpriteRenderer[] furniture;
        public Rect Footprint => footprint;
        public float Reveal { get; private set; }
        public void Apply(float value)
        {
            Reveal = Mathf.Clamp01(value);
            cover.color = new Color(1, 1, 1, 1 - Reveal);
            foreach (var wall in walls) wall.color = new Color(1, 1, 1, 1 - Reveal);
            if (furniture == null) furniture = interior.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sprite in furniture) { var c = sprite.color; c.a = Reveal; sprite.color = c; }
        }
    }
}
