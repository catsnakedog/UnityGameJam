using UnityEngine;
namespace ShelterRun
{
    public sealed class FarmingBunker : MonoBehaviour
    {
        [SerializeField] PolygonCollider2D depositZone;
        [SerializeField] Transform landingPoint;
        [SerializeField] GameObject readyMarker;
        public Vector3 LandingPosition => landingPoint.position;
        public bool Contains(Vector3 worldPoint) => depositZone.OverlapPoint(worldPoint);
        public void SetReady(bool value) { readyMarker.SetActive(value); }
    }
}
