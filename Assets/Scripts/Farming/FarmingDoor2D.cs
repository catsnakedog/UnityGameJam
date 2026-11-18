using UnityEngine;
namespace ShelterRun
{
    public sealed class FarmingDoor2D : MonoBehaviour
    {
        [SerializeField] FarmingRoom2D area;
        [SerializeField] FarmingDoor2D target;
        [SerializeField] Transform arrival;
        [SerializeField] GameObject focusMarker;
        [SerializeField] string destinationLabel;
        public FarmingRoom2D Area => area;
        public FarmingDoor2D Target => target;
        public Vector3 Arrival => arrival.position;
        public string DestinationLabel => destinationLabel;
        public void SetFocused(bool focused) { if (focusMarker) focusMarker.SetActive(focused); }
    }
}
