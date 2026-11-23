using GameJam.Audio;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameJam.Shelter;
namespace ShelterRun
{
    public sealed class FarmingPlayer : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Transform interactPrompt;
        [SerializeField] SurvivorAnimation artworkSource;
        SurvivorAnimation.Artwork artwork;
        readonly SurvivorAnimation.Playback playback = new SurvivorAnimation.Playback();
        [SerializeField] CircleCollider2D feet;
        FarmingSession session;
        Vector2 facing = Vector2.right;

        public FarmingInventory Inventory { get; private set; }
        public FarmingPickup Focused { get; private set; }
        public FarmingDoor2D FocusedDoor { get; private set; }
        public bool AtBunker => session != null && session.Bunker.Contains(transform.position);

        void LateUpdate()
        {
            if (!interactPrompt) return;
            bool visible = session && session.CanExplore && !GameOptions.BlocksInput && CanUseInteraction();
            if (interactPrompt.gameObject.activeSelf != visible) interactPrompt.gameObject.SetActive(visible);
            if (visible)
            {
                var bounds = sprite.bounds;
                interactPrompt.position = new Vector3(bounds.center.x, bounds.max.y + .26f, transform.position.z);
            }
        }

        bool CanUseInteraction()
        {
            if (FocusedDoor) return true;
            if (!session.CanAct) return false;
            if (Inventory.Used > 0 && AtBunker) return true;
            return Focused ? Inventory.CanAdd(Focused) : Inventory.Used > 0;
        }

        public void Initialize(FarmingSession owner)
        {
            session = owner;
            Inventory = new FarmingInventory(owner.Rules.Capacity);
            artwork = ArtworkFor(owner.StartingPersonId);
            AnimatePose(false, 0f);
        }
        internal SurvivorAnimation.Artwork ArtworkFor(string id) =>
            artworkSource.FindArtwork(id) ?? throw new InvalidOperationException("Missing farming character art for " + id);

        bool Take(FarmingPickup item)
        {
            if (!Inventory.TryAdd(item)) return false;
            item.Carry(body, Inventory.Carried.Count - 1);
            return true;
        }
        void Update()
        {
            if (GameOptions.BlocksInput) return;
            if (!session || !session.CanExplore)
            {
                if (session && session.Phase == FarmingPhase.Ready) AnimatePose(false, Time.deltaTime);
                return;
            }
            var keys = Keyboard.current;
            float horizontal = keys == null ? 0 : (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
            Move(new Vector2(horizontal, 0), Time.deltaTime);
            RefreshFocus();
            if (keys != null && keys.eKey.wasPressedThisFrame) Interact();
        }
        public void Move(Vector2 input, float deltaTime)
        {
            if (session == null || !session.CanExplore) return;
            float horizontal = Mathf.Clamp(input.x, -1, 1);
            if (horizontal != 0) { facing = new Vector2(Mathf.Sign(horizontal), 0); sprite.flipX = horizontal < 0; }
            Vector3 next = transform.position + Vector3.right * (horizontal * session.Rules.MoveSpeed * Mathf.Max(0, deltaTime));
            // A side-view walk line is authoritative. Tables are behind the lane;
            // clamping the entire step prevents wall penetration even after a hitch.
            Vector3 constrained = FarmingMap2D.Instance.Constrain(next, feet.radius);
            bool walking = !Mathf.Approximately(transform.position.x, constrained.x);
            transform.position = constrained;
            AnimatePose(walking, deltaTime);
            Physics2D.SyncTransforms();
        }
        public void Teleport(Vector3 destination)
        {
            ClearFocus(); transform.position = destination;
            Physics2D.SyncTransforms(); AnimatePose(false, 0f);
        }
        void ClearFocus()
        {
            if (Focused) Focused.SetFocused(false);
            if (FocusedDoor) FocusedDoor.SetFocused(false);
            Focused = null; FocusedDoor = null;
        }
        public void RefreshFocus()
        {
            if (session == null || Inventory == null) return;
            ClearFocus();
            var map = FarmingMap2D.Instance;
            FocusedDoor = map.NearestDoor(transform.position, session.Rules.InteractionRadius);
            if (FocusedDoor) FocusedDoor.SetFocused(true);
            else if (session.CanAct)
            {
                float best = session.Rules.InteractionRadius;
                foreach (var item in session.Pickups)
                {
                    if (!item || item.State != PickupState.Available || !map.CanReach(transform.position, item.transform.position)) continue;
                    float distance = Mathf.Abs(item.transform.position.x - transform.position.x);
                    if (distance > best) continue;
                    best = distance; Focused = item;
                }
                if (Focused) Focused.SetFocused(true);
            }
            session.Bunker.SetReady(AtBunker && Inventory.Used > 0);
        }
        public void Interact()
        {
            if (session == null || !session.CanExplore) return;
            RefreshFocus();
            if (FocusedDoor && !(AtBunker && Inventory.Used > 0))
            {
                FarmingMap2D.Instance.Traverse(FocusedDoor);
                RefreshFocus();
                return;
            }
            if (!session.CanAct) return;
            if (AtBunker && Inventory.Used > 0)
            {
                while (Inventory.Carried.Count > 0)
                {
                    var item = Inventory.RemoveLast(); session.Deposit(item);
                    item.Throw(session.Bunker.LandingPosition, true, session.Rules);
                }
                session.Notify("벙커에 투입했습니다");
            }
            else if (Focused)
            {
                if (!Take(Focused))
                { session.Notify("운반 공간이 부족합니다"); return; }
                session.Notify(Focused.Record.Type == FarmingObjectType.Person ? "사람을 들었습니다" : "물자를 챙겼습니다");
                if (Focused.Record.Type == FarmingObjectType.Item) GameAudio.Play(GameSound.FarmPickup);
                Focused = null;
            }
            else if (Inventory.Used > 0)
            {
                var item = Inventory.RemoveLast();
                Vector3 destination = FarmingMap2D.Instance.Constrain(transform.position + (Vector3)facing * session.Rules.DropDistance, feet.radius);
                bool deposit = session.Bunker.Contains(destination);
                if (deposit) session.Deposit(item);
                item.Throw(deposit ? session.Bunker.LandingPosition : destination, deposit, session.Rules);
                session.Notify(deposit ? "벙커에 투입했습니다" : "내려놓았습니다 · E로 다시 들기");
            }
            for (int i = 0; i < Inventory.Carried.Count; i++) Inventory.Carried[i].PlaceInHands(i);
        }
        void AnimatePose(bool walking, float elapsed)
        {
            if (artwork == null) return;
            var pose = walking ? SurvivorAnimation.Pose.Walk : SurvivorAnimation.Pose.Idle;
            playback.Sample(artwork, pose, SurvivorAnimation.Pose.Idle, sprite, elapsed);
        }
        public void StopInteraction() { ClearFocus(); session.Bunker.SetReady(false); AnimatePose(false, 0f); }
    }
}
