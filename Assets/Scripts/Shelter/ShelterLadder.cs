using UnityEngine;

namespace UnityGameJam.Shelter
{
    [ExecuteAlways]
    public sealed class ShelterLadder : MonoBehaviour
    {
        [Tooltip("Place the transform at the lower floor. Both ends find touching rooms/corridors automatically.")]
        [Min(0.2f)] public float height = 3.8f;
        public bool accessible = true;
        [Tooltip("Optional visual normalized to one local unit high, starting at the bottom.")]
        public Transform ladderVisual;
        public Vector2 Bottom => transform.position;
        public Vector2 Top => transform.TransformPoint(new Vector3(0f, height, 0f));
        public bool IsUsable => isActiveAndEnabled && accessible && Top.y > Bottom.y + 0.1f && Mathf.Abs(Top.x - Bottom.x) < 0.01f;

        private void OnEnable() => RefreshVisuals();
        private void OnValidate() => RefreshVisuals();
        public void RefreshVisuals()
        {
            height = Mathf.Max(0.2f, height);
            if (ladderVisual != null) ladderVisual.localScale = new Vector3(1f, height, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsUsable ? Color.yellow : Color.red;
            Gizmos.DrawLine(Bottom, Top);
            Gizmos.DrawWireSphere(Bottom, 0.12f);
            Gizmos.DrawWireSphere(Top, 0.12f);
        }
    }
}
