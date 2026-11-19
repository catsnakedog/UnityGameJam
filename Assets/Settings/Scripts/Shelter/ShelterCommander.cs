using GameJam.Audio;
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
        public event System.Action<ShelterWalker> SelectionChanged;
        public string Message { get; private set; } = string.Empty;
        private ShelterArea rejected;
        private float rejectedUntil;
        readonly HashSet<object> inputBlocks = new HashSet<object>();
        readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
        int inputReleasedFrame = -1;
        public bool InputBlocked => GameOptions.BlocksInput || inputBlocks.Count > 0 || inputReleasedFrame == Time.frameCount;
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
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                ClickScreen(Mouse.current.position.ReadValue());
            if (rejected != null && Time.unscaledTime >= rejectedUntil) ClearRejected();
            if (rejected == null && Selected != null && Selected.Destination != null)
                Message = Selected.characterName + "  >  " + Selected.Destination.displayName + "  /  " + Selected.Status;
            else if (rejected == null && Selected != null && Selected.Status == "NO ROUTE") Message = "이동할 수 없습니다";
            if (statusLabel != null) statusLabel.text = Message;
            if (navigation == null) return;
            foreach (var area in navigation.Areas)
                area.ShowHighlight(Selected != null && Selected.Destination == area || area == rejected && Time.unscaledTime < rejectedUntil,
                    area == rejected && Time.unscaledTime < rejectedUntil);
        }

        internal bool IsPointerOverUI(Vector2 screen)
        {
            var events = EventSystem.current;
            if (!events) return false;
            events.RaycastAll(new PointerEventData(events) { position = screen }, pointerHits);
            foreach (var hit in pointerHits)
                if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
            return false;
        }

        void RegisterPointerSelection(ShelterWalker survivor)
        {
            if (view && view.TryGetComponent<ShelterCamera>(out var camera)) camera.RegisterPointerSelection(survivor);
        }

        public bool ClickScreen(Vector2 screen)
        {
            if (view == null) view = Camera.main;
            if (view == null || !view.pixelRect.Contains(screen) || IsPointerOverUI(screen))
            {
                RegisterPointerSelection(null);
                return false;
            }
            var ray = view.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.z) < 0.001f) return false;
            return ClickWorld(ray.GetPoint(-ray.origin.z / ray.direction.z));
        }

        public ShelterWalker SurvivorAtScreen(Vector2 screen)
        {
            if (!view || !view.pixelRect.Contains(screen)) return null;
            var ray = view.ScreenPointToRay(screen);
            return Mathf.Abs(ray.direction.z) < .001f ? null : SurvivorAt(ray.GetPoint(-ray.origin.z / ray.direction.z));
        }
        ShelterWalker SurvivorAt(Vector2 point, bool pickingForRescue = false)
        {
            ShelterWalker hit = null;
            float nearest = float.PositiveInfinity;
            bool hitIsPatient = false;
            foreach (var actor in FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None))
            {
                if (actor.gameObject.scene != gameObject.scene || !actor.playerSelectable || !actor.isActiveAndEnabled || !actor.ContainsClick(point)) continue;
                float distance = Vector2.Distance(point, (Vector2)actor.transform.position + Vector2.up);
                bool patient = pickingForRescue && actor.CanBePickedUp;
                if (hitIsPatient && !patient) continue;
                if (patient == hitIsPatient && distance >= nearest) continue;
                hit = actor;
                nearest = distance;
                hitIsPatient = patient;
            }
            return hit;
        }
        // Same dispatch is used by mouse input and automated scene checks.
        public bool ClickWorld(Vector2 point)
        {
            if (InputBlocked) { RegisterPointerSelection(null); return false; }
            var hit = SurvivorAt(point, Selected && !Selected.IsMovementLocked && !Selected.IsRescuing);
            if (hit != null)
            {
                if (Selected && Selected != hit && !Selected.IsMovementLocked && hit.NeedsRescue)
                {
                    ClearRejected();
                    RegisterPointerSelection(null);
                    bool accepted = Selected.TryRescue(hit);
                    GameAudio.Play(accepted ? GameSound.Drop : GameSound.Wrong);
                    Message = accepted ? hit.characterName + " 구조 중 · 수면실로 이동" : Selected.RoomOrderFailure ?? "현재 구조할 수 없습니다";
                    return accepted;
                }
                GameAudio.Play(GameSound.Pickup);
                SelectSurvivor(hit);
                RegisterPointerSelection(hit);
                return true;
            }
            RegisterPointerSelection(null);
            if (navigation == null) return false;
            navigation.Refresh();
            ShelterArea destination = null;
            foreach (var area in FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID))
            {
                if (area.gameObject.scene != gameObject.scene || !area.isActiveAndEnabled || !area.ContainsClick(point)) continue;
                if (area.kind == ShelterArea.AreaKind.Room) { destination = area; break; }
                if (!destination) destination = area;
            }
            if (!destination) return false;
            if (!Selected) { Message = string.Empty; return false; }
            ClearRejected();
            var tent = destination.roomType == ShelterRoomType.Sleeping
                ? System.Array.Find(destination.GetComponentsInChildren<ShelterTent>(), t => t.Contains(point)) : null;
            if (!(tent && Selected.UseTent(tent)) && !Selected.MoveTo(destination))
            {
                GameAudio.Play(GameSound.Wrong);
                rejected = destination;
                rejectedUntil = Time.unscaledTime + 2f;
                destination.ShowHighlight(true, true);
                Message = Selected.RoomOrderFailure ?? (Selected.IsRescuing ? "부상자를 구조하고 있습니다" : Selected.IsMovementLocked ? "전투 중이거나 탈진하여 이동할 수 없습니다" : "NO ROUTE TO " + destination.displayName);
                return false;
            }
            GameAudio.Play(GameSound.Drop);
            SetSelection(null, Selected.characterName + "  >  " + destination.displayName);
            return true;
        }

        internal void SelectSurvivor(ShelterWalker survivor)
        {
            if (!survivor || !survivor.isActiveAndEnabled || !survivor.playerSelectable) return;
            SetSelection(survivor, survivor.characterName + " 선택");
        }

        void SetSelection(ShelterWalker survivor, string message)
        {
            ClearRejected();
            if (Selected) Selected.SetSelected(false);
            Selected = survivor;
            if (Selected) Selected.SetSelected(true);
            Message = message;
            SelectionChanged?.Invoke(survivor);
        }

        private void ClearRejected()
        {
            if (rejected != null) rejected.ShowHighlight(false);
            rejected = null;
        }
    }
}
