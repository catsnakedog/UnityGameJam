using UnityEngine;

namespace UnityGameJam.Shelter
{
    // The transform marks foot height. Side-view movement stays on this walkable lane.
    public sealed class ShelterFloor : MonoBehaviour
    {
        public float leftEdge = -12.5f;
        public float rightEdge = 12.5f;
        public float Height => transform.position.y;

        public float ClampX(float x) => Mathf.Clamp(x, leftEdge, rightEdge);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.75f, 0.5f);
            Gizmos.DrawLine(new Vector3(leftEdge, Height), new Vector3(rightEdge, Height));
        }
    }
}
