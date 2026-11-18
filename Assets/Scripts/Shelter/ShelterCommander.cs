using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterCommander : MonoBehaviour
    {
        public ShelterNavigation navigation;
        public Camera view;
        public TextMesh statusLabel;
        public ShelterWalker Selected { get; private set; }
        public string Message { get; private set; } = "CLICK A SURVIVOR, THEN CLICK A ROOM";
        private ShelterArea rejected;
        private float rejectedUntil;
        readonly HashSet<object> inputBlocks = new HashSet<object>();
        int inputReleasedFrame = -1;
        public bool InputBlocked => inputBlocks.Count > 0 || inputReleasedFrame == Time.frameCount;
        public void AcquireInputBlock(object owner) { if (owner != null) inputBlocks.Add(owner); }
        public void ReleaseInputBlock(object owner) { if (inputBlocks.Remove(owner)) inputReleasedFrame = Time.frameCount; }
        public void Report(string message) { Message = message; }

        private void Start()
        {
            if (navigation == null) navigation = GetComponent<ShelterNavigation>();
            if (view == null) view = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                ClickScreen(Mouse.current.position.ReadValue());
            if (rejected != null && Time.unscaledTime >= rejectedUntil) ClearRejected();
            if (rejected == null && Selected != null && Selected.Destination != null)
                Message = Selected.characterName + "  >  " + Selected.Destination.displayName + "  /  " + Selected.Status;
            else if (rejected == null && Selected != null && Selected.Status == "NO ROUTE") Message = "NO ROUTE - CHECK ROOM / CORRIDOR / LADDER CONNECTIONS";
            if (statusLabel != null) statusLabel.text = Message;
            if (navigation == null) return;
            foreach (var area in navigation.Areas)
                area.ShowHighlight(Selected != null && Selected.Destination == area || area == rejected && Time.unscaledTime < rejectedUntil,
                    area == rejected && Time.unscaledTime < rejectedUntil);
        }

        public bool ClickScreen(Vector2 screen)
        {
            if (view == null) view = Camera.main;
            if (view == null || !view.pixelRect.Contains(screen)) return false;
            var ray = view.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 0.001f) return false;
            return ClickWorld(ray.GetPoint(-ray.origin.z / ray.direction.z));
        }

        // Same dispatch is used by mouse input and automated scene checks.
        public bool ClickWorld(Vector2 point)
        {
            if (InputBlocked) return false;
            ShelterWalker hit = null;
            float nearest = float.PositiveInfinity;
            foreach (var actor in FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None))
            {
                if (actor.gameObject.scene != gameObject.scene || !actor.playerSelectable || !actor.isActiveAndEnabled || !actor.ContainsClick(point)) continue;
                float distance = Vector2.Distance(point, (Vector2)actor.transform.position + Vector2.up);
                if (distance < nearest) { hit = actor; nearest = distance; }
            }
            if (hit != null)
            {
                ClearRejected();
                if (Selected != null) Selected.SetSelected(false);
                Selected = hit;
                Selected.SetSelected(true);
                Message = hit.characterName + " SELECTED - CLICK A ROOM";
                return true;
            }
            if (navigation == null) return false;
            navigation.Refresh();
            foreach (var area in FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID))
            {
                if (area.gameObject.scene != gameObject.scene || !area.isActiveAndEnabled || !area.ContainsClick(point)) continue;
                if (Selected == null) { Message = "SELECT A SURVIVOR FIRST"; return false; }
                if (!Selected.MoveTo(area))
                {
                    ClearRejected();
                    rejected = area;
                    rejectedUntil = Time.unscaledTime + 2f;
                    area.ShowHighlight(true, true);
                    Message = Selected.IsMovementLocked ? "전투 중이거나 탈진하여 이동할 수 없습니다" : "NO ROUTE TO " + area.displayName;
                    return false;
                }
                ClearRejected();
                Message = Selected.characterName + "  >  " + area.displayName;
                return true;
            }
            return false;
        }

        private void ClearRejected()
        {
            if (rejected != null) rejected.ShowHighlight(false);
            rejected = null;
        }
    }
}
