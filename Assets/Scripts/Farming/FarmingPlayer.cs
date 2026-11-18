using UnityEngine;
using UnityEngine.InputSystem;
namespace ShelterRun
{
    public sealed class FarmingPlayer : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Sprite idleSprite;
        [SerializeField] Sprite carrySprite;
        [SerializeField] CircleCollider2D feet;
        FarmingSession session;
        Vector2 facing = Vector2.right;
        float walkPhase;
        public FarmingInventory Inventory { get; private set; }
        public FarmingPickup Focused { get; private set; }
        public FarmingDoor2D FocusedDoor { get; private set; }
        public bool AtBunker => session != null && session.Bunker.Contains(transform.position);
        public void Initialize(FarmingSession owner)
        { session = owner; Inventory = new FarmingInventory(owner.Rules.Capacity); }
        void Update()
        {
            if (session == null || !session.CanAct) return;
            var keys = Keyboard.current;
            float horizontal = keys == null ? 0 : (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
            Move(new Vector2(horizontal, 0), Time.deltaTime);
            RefreshFocus();
            if (keys != null && keys.eKey.wasPressedThisFrame) Interact();
            AnimatePose(horizontal != 0);
        }
        public void Move(Vector2 input, float deltaTime)
        {
            if (session == null || !session.CanAct) return;
            float horizontal = Mathf.Clamp(input.x, -1, 1);
            if (horizontal != 0) { facing = new Vector2(Mathf.Sign(horizontal), 0); sprite.flipX = horizontal < 0; }
            Vector3 next = transform.position + Vector3.right * (horizontal * session.Rules.MoveSpeed * Mathf.Max(0, deltaTime));
            // A side-view walk line is authoritative. Tables are behind the lane;
            // clamping the entire step prevents wall penetration even after a hitch.
            transform.position = FarmingMap2D.Instance.Constrain(next, feet.radius);
            Physics2D.SyncTransforms();
        }
        public void Teleport(Vector3 destination)
        {
            ClearFocus(); transform.position = destination;
            Physics2D.SyncTransforms(); AnimatePose(false);
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
            else
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
            if (session == null || !session.CanAct) return;
            RefreshFocus();
            if (AtBunker && Inventory.Used > 0)
            {
                while (Inventory.Carried.Count > 0)
                {
                    var item = Inventory.RemoveLast(); session.Deposit(item);
                    item.Throw(session.Bunker.LandingPosition, true, session.Rules);
                }
                session.Notify("벙커에 투입했습니다");
            }
            else if (FocusedDoor)
            {
                FarmingMap2D.Instance.Traverse(FocusedDoor);
                RefreshFocus(); return;
            }
            else if (Focused)
            {
                if (!Inventory.TryAdd(Focused))
                { session.Notify($"공간 부족 · {Focused.Record.Weight}칸 필요 / {Inventory.Capacity - Inventory.Used}칸 남음"); return; }
                Focused.Carry(body, Inventory.Carried.Count - 1);
                session.Notify(Focused.Record.Type == FarmingObjectType.Person ? "사람을 들었습니다" : "물자를 챙겼습니다");
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
            AnimatePose(false);
        }
        void AnimatePose(bool walking)
        {
            walkPhase += walking ? Time.deltaTime * 11 : 0;
            body.localPosition = new Vector3(0, walking ? Mathf.Abs(Mathf.Sin(walkPhase)) * .035f : 0, 0);
            sprite.sprite = Inventory != null && Inventory.Used > 0 ? carrySprite : idleSprite;
        }
        public void StopInteraction() { ClearFocus(); session.Bunker.SetReady(false); AnimatePose(false); }
    }
}
