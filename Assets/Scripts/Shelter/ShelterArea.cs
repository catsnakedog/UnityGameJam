using UnityEngine;
using System.Collections.Generic;

namespace UnityGameJam.Shelter
{
    [ExecuteAlways]
    public sealed class ShelterArea : MonoBehaviour
    {
        public enum AreaKind { Room, Corridor }
        public AreaKind kind;
        public ShelterRoomType roomType;
        public string displayName = "ROOM";
        [Min(0.5f)] public float width = 8.65f;
        [Min(0.5f)] public float height = 3.8f;
        [Tooltip("Disable to close this room/corridor to navigation.")]
        public bool accessible = true;
        public Transform backgroundVisual;
        public Transform floorVisual;
        public TextMesh nameLabel;
        public SpriteRenderer highlight;
        readonly HashSet<object> occupiers = new HashSet<object>();
        public bool IsOccupied => occupiers.Count > 0;
        public bool IsOperational => IsWalkable && !IsOccupied;
        public void SetOccupied(object owner, bool occupied)
        {
            if (owner == null) return;
            if (occupied) occupiers.Add(owner); else occupiers.Remove(owner);
            RefreshVisuals();
        }

        public float Left => transform.TransformPoint(new Vector3(-width * 0.5f, 0f)).x;
        public float Right => transform.TransformPoint(new Vector3(width * 0.5f, 0f)).x;
        public float FloorY => transform.position.y;
        public bool IsWalkable => isActiveAndEnabled && accessible && transform.lossyScale.x > 0f &&
            transform.lossyScale.y > 0f && Vector3.Dot(transform.right, Vector3.right) > 0.999f;
        public Vector2 Centre => new Vector2((Left + Right) * 0.5f, FloorY);

        private void OnEnable() => RefreshVisuals();
        private void OnValidate() => RefreshVisuals();

        public bool ContainsClick(Vector2 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return Mathf.Abs(local.x) <= width * 0.5f && local.y >= 0f && local.y <= height;
        }

        public Vector2 ArrivalPoint(float offset)
        {
            float margin = Mathf.Min(0.35f, (Right - Left) * 0.2f);
            return new Vector2(Mathf.Clamp(Centre.x + offset, Left + margin, Right - margin), FloorY);
        }

        public void RefreshVisuals()
        {
            width = Mathf.Max(0.5f, width);
            height = Mathf.Max(0.5f, height);
            if (backgroundVisual != null)
            {
                backgroundVisual.localPosition = new Vector3(0f, height * 0.5f, 0f);
                backgroundVisual.localScale = new Vector3(width - 0.1f, height - 0.12f, 1f);
            }
            if (floorVisual != null)
            {
                floorVisual.localPosition = new Vector3(0f, -0.12f, 0f);
                floorVisual.localScale = new Vector3(width, 0.24f, 1f);
            }
            if (nameLabel != null)
            {
                nameLabel.text = IsOccupied ? displayName + " [점거 / 기능 정지]" : displayName;
                nameLabel.color = IsOccupied ? new Color(1f, 0.35f, 0.28f) : new Color(0.75f, 0.8f, 0.76f);
                nameLabel.transform.localPosition = new Vector3(-width * 0.5f + 0.4f, height - 0.62f, 0f);
            }
            if (highlight != null)
            {
                highlight.transform.localPosition = new Vector3(0f, 0.045f, 0f);
                highlight.transform.localScale = new Vector3(width - 0.15f, 0.07f, 1f);
            }
        }

        public void ShowHighlight(bool show, bool rejected = false)
        {
            if (highlight == null) return;
            highlight.enabled = show || IsOccupied;
            highlight.color = rejected || IsOccupied ? new Color(0.94f, 0.35f, 0.28f) : new Color(0.88f, 0.73f, 0.4f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = accessible ? Color.cyan : Color.red;
            Gizmos.DrawLine(new Vector3(Left, FloorY), new Vector3(Right, FloorY));
            Gizmos.DrawWireSphere(new Vector3(Left, FloorY), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(Right, FloorY), 0.1f);
        }
    }
}
