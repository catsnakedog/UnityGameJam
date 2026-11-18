using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityGameJam.Characters;
using UnityGameJam.Common;
using UnityGameJam.Zombies;

namespace UnityGameJam.Shelter
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(Camera), typeof(FixedAspectCamera))]
    public sealed class ShelterCamera : MonoBehaviour
    {
        [Header("View (1920 x 1080 / 16:9)")]
        [Tooltip("Visible world height / 2. Smaller values show less of the bunker.")]
        [Min(0.5f)] public float viewHalfHeight = 11f;
        [Min(0.5f)] public float minHalfHeight = 4f;
        [Min(0.5f)] public float maxHalfHeight = 24f;
        [Tooltip("Offset from the survivor's feet when centering or following.")]
        public Vector2 characterOffset = new Vector2(0f, 1f);

        [Header("Free camera")]
        [Min(0.1f)] public float panSpeed = 12f;
        public bool edgePan = true;
        [Tooltip("Edge scrolling band in 1920 x 1080 reference pixels.")]
        [Range(1f, 100f)] public float edgeSize = 24f;
        [Tooltip("Allow the camera centre this far outside the active room/corridor bounds.")]
        [Min(0f)] public float boundaryPadding = 1f;

        [Header("Character shortcuts")]
        [Min(0.05f)] public float doubleTapSeconds = 0.3f;
        public ShelterRoster roster;
        public ShelterCommander commander;

        public CharacterIdentity FollowTarget { get; private set; }
        public bool IsFollowing => FollowTarget && FollowTarget.isActiveAndEnabled;
        public const int ShortcutCount = 6;

        Camera view;
        ZombieInvasionController invasion;
        float zoomScale = 1f;
        Rect mapBounds;
        bool hasMapBounds;
        int previousSlot;
        double previousPressTime;

        void OnEnable()
        {
            view = GetComponent<Camera>();
            zoomScale = 1f;
            ApplyFraming();
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            // Bind within this scene, including when scenes are loaded additively.
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (!roster) roster = root.GetComponentInChildren<ShelterRoster>();
                if (!commander) commander = root.GetComponentInChildren<ShelterCommander>();
                if (!invasion) invasion = root.GetComponentInChildren<ZombieInvasionController>();
            }
            RefreshMapBounds();
        }

        void OnValidate()
        {
            minHalfHeight = Mathf.Max(0.5f, minHalfHeight);
            maxHalfHeight = Mathf.Max(minHalfHeight, maxHalfHeight);
            viewHalfHeight = Mathf.Clamp(viewHalfHeight, minHalfHeight, maxHalfHeight);
            panSpeed = Mathf.Max(0.1f, panSpeed);
            edgeSize = Mathf.Clamp(edgeSize, 1f, 100f);
            boundaryPadding = Mathf.Max(0f, boundaryPadding);
            doubleTapSeconds = Mathf.Max(0.05f, doubleTapSeconds);
        }

        void OnDisable()
        {
            Unlock();
        }

        void OnApplicationFocus(bool focused) { if (!focused) previousSlot = 0; }

        void Update()
        {
            if (!Application.isPlaying || !HasGameFocus() || InputBlocked())
            {
                previousSlot = 0;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.yKey.wasPressedThisFrame) { Unlock(); return; }
                for (int slot = 1; slot <= ShortcutCount; slot++)
                    if (keyboard[(Key)((int)Key.Digit1 + slot - 1)].wasPressedThisFrame ||
                        keyboard[(Key)((int)Key.Numpad1 + slot - 1)].wasPressedThisFrame)
                    {
                        PressCharacterSlot(slot);
                        return; // Do not edge-scroll away from a shortcut in the same frame.
                    }
            }

            var mouse = Mouse.current;
            Vector2 pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            bool inViewport = mouse != null && view.pixelRect.Contains(pointer);
            if (inViewport && scroll != 0f)
            {
                bool overUI = commander ? commander.IsPointerOverUI(pointer) :
                    EventSystem.current && EventSystem.current.IsPointerOverGameObject();
                if (!overUI)
                {
                    float size = Mathf.Clamp(view.orthographicSize * Mathf.Pow(1.15f, -scroll), minHalfHeight, maxHalfHeight);
                    zoomScale = size / viewHalfHeight;
                    ApplyFraming();
                }
            }

            if (IsFollowing) return;
            Vector2 direction = Vector2.zero;
            if (keyboard != null)
            {
                direction.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                direction.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                            - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }
            if (edgePan && inViewport && (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()))
                direction += EdgeDirection(pointer, view.pixelRect);
            Pan(direction, Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            ApplyFraming();
            if (!Application.isPlaying) return;
            if (IsFollowing) MoveCentre((Vector2)FollowTarget.transform.position + characterOffset);
            else if (FollowTarget) Unlock();
        }

        void ApplyFraming()
        {
            if (!view) return;
            view.orthographic = true;
            view.orthographicSize = Mathf.Clamp(viewHalfHeight * zoomScale, minHalfHeight, maxHalfHeight);
        }

        static bool HasGameFocus()
        {
#if UNITY_EDITOR
            var window = UnityEditor.EditorWindow.focusedWindow;
            return window && window.GetType().Name == "GameView";
#else
            return Application.isFocused;
#endif
        }

        bool InputBlocked()
        {
            if (commander && commander.InputBlocked) return true;
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected && (selected.GetComponentInParent<TMP_InputField>() || selected.GetComponentInParent<InputField>());
        }

        public CharacterIdentity GetCharacterSlot(int slot)
        {
            if (slot < 1 || slot > ShortcutCount || !roster || !roster.IsReady) return null;
            // Existing stable IDs, never the rescue order or a duplicate character table.
            string id = "person_" + slot.ToString("00");
            foreach (var character in roster.Characters)
                if (character && character.isActiveAndEnabled && character.Id == id) return character;
            return null;
        }

        public bool PressCharacterSlot(int slot)
        {
            if (InputBlocked()) { previousSlot = 0; return false; }
            var target = GetCharacterSlot(slot);
            if (!target) { previousSlot = 0; return false; }
            double now = Time.unscaledTimeAsDouble;
            bool doubleTap = previousSlot == slot && now - previousPressTime <= doubleTapSeconds;
            FollowTarget = doubleTap ? target : null;
            previousSlot = doubleTap ? 0 : slot;
            previousPressTime = now;
            MoveCentre((Vector2)target.transform.position + characterOffset);
            return true;
        }

        public void Unlock()
        {
            FollowTarget = null;
            previousSlot = 0;
        }

        public void Pan(Vector2 direction, float deltaTime)
        {
            if (IsFollowing || InputBlocked() || direction.sqrMagnitude == 0f || deltaTime <= 0f) return;
            previousSlot = 0;
            MoveCentre((Vector2)transform.position + Vector2.ClampMagnitude(direction, 1f) * panSpeed * deltaTime);
        }

        public Vector2 EdgeDirection(Vector2 pointer, Rect viewport)
        {
            if (viewport.width <= 0 || viewport.height <= 0 || !viewport.Contains(pointer)) return Vector2.zero;
            float band = edgeSize * viewport.height / FixedAspectCamera.ReferenceHeight;
            return new Vector2(pointer.x < viewport.xMin + band ? -1f : pointer.x > viewport.xMax - band ? 1f : 0f,
                pointer.y < viewport.yMin + band ? -1f : pointer.y > viewport.yMax - band ? 1f : 0f);
        }

        public void RefreshMapBounds()
        {
            hasMapBounds = false;
            foreach (var area in FindObjectsByType<ShelterArea>(FindObjectsSortMode.None))
            {
                if (area.gameObject.scene != gameObject.scene || !area.isActiveAndEnabled) continue;
                float top = area.transform.TransformPoint(new Vector3(0f, area.height)).y;
                var bounds = Rect.MinMaxRect(area.Left, area.FloorY, area.Right, top);
                mapBounds = hasMapBounds ? Rect.MinMaxRect(Mathf.Min(mapBounds.xMin, bounds.xMin), Mathf.Min(mapBounds.yMin, bounds.yMin),
                    Mathf.Max(mapBounds.xMax, bounds.xMax), Mathf.Max(mapBounds.yMax, bounds.yMax)) : bounds;
                hasMapBounds = true;
            }
        }

        void MoveCentre(Vector2 centre)
        {
            if (hasMapBounds)
            {
                float left = mapBounds.xMin, right = mapBounds.xMax;
                if (invasion) invasion.IncludeApproachingZombies(ref left, ref right);
                centre.x = Mathf.Clamp(centre.x, left - boundaryPadding, right + boundaryPadding);
                centre.y = Mathf.Clamp(centre.y, mapBounds.yMin - boundaryPadding, mapBounds.yMax + boundaryPadding);
            }
            transform.position = new Vector3(centre.x, centre.y, transform.position.z);
        }
    }
}
