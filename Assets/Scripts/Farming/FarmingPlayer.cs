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
        Vector2 facing = Vector2.down;
        // Numerical collision tolerance, not a movement/balance setting.
        const float CollisionSkin = .01f;
        readonly System.Collections.Generic.List<RaycastHit2D> hits = new System.Collections.Generic.List<RaycastHit2D>(16);
        readonly System.Collections.Generic.List<Collider2D> overlaps = new System.Collections.Generic.List<Collider2D>(16);
        Vector2 lastSafePosition;
        bool hasSafePosition;
        float walkPhase;
        public FarmingInventory Inventory { get; private set; }
        public FarmingPickup Focused { get; private set; }
        public bool AtBunker => session != null && session.Bunker.Contains(transform.position);
        public void Initialize(FarmingSession owner)
        {
            session = owner; Inventory = new FarmingInventory(owner.Rules.Capacity);
            hasSafePosition = false;
        }
        void Update()
        {
            if (session == null || !session.CanAct) return;
            var keys = Keyboard.current;
            Vector2 move = Vector2.zero;
            if (keys != null)
            {
                move.x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                move.y = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            }
            Move(move, Time.deltaTime);
            RefreshFocus();
            if (keys != null && keys.eKey.wasPressedThisFrame) Interact();
            AnimatePose(move.sqrMagnitude > 0);
        }
        public void Move(Vector2 input, float deltaTime)
        {
            if (session == null || !session.CanAct) return;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
            // Casts cannot escape an existing overlap: every hit starts at distance zero.
            // Resolve penetration even without input, before sweeping the requested movement.
            Physics2D.SyncTransforms();
            if (!ResolveOverlaps(filter)) return;
            Vector2 direction = Vector2.ClampMagnitude(input, 1);
            if (direction.sqrMagnitude < .001f) return;
            facing = direction.normalized;
            sprite.flipX = direction.x < 0;
            // Continuous circle sweep and wall sliding, including large frame deltas.
            Vector2 remaining = direction * (session.Rules.MoveSpeed * Mathf.Max(0, deltaTime));
            Vector2 position = transform.position;
            for (int pass = 0; pass < 3 && remaining.sqrMagnitude > .000001f; pass++)
            {
                int count = Physics2D.CircleCast(position + FeetOffset, FeetRadius, remaining.normalized, filter, hits, remaining.magnitude + CollisionSkin);
                float distance = remaining.magnitude; Vector2 normal = Vector2.zero;
                for (int i = 0; i < count; i++)
                    if (BlocksMovement(hits[i].collider) && Vector2.Dot(remaining, hits[i].normal) < 0 && hits[i].distance < distance)
                    { distance = Mathf.Max(0, hits[i].distance - CollisionSkin); normal = hits[i].normal; }
                var step = remaining.normalized * distance;
                if (FarmingMap2D.Instance.IsFloor(position + step)) position += step;
                remaining -= step;
                if (normal == Vector2.zero) break;
                remaining -= normal * Mathf.Min(0, Vector2.Dot(remaining, normal));
            }
            transform.position = position;
            Physics2D.SyncTransforms();
            ResolveOverlaps(filter);
        }
        Vector2 FeetOffset => (Vector2)feet.transform.TransformPoint(feet.offset) - (Vector2)transform.position;
        float FeetRadius => feet.radius * Mathf.Max(Mathf.Abs(feet.transform.lossyScale.x), Mathf.Abs(feet.transform.lossyScale.y));
        bool BlocksMovement(Collider2D other) => other && !other.isTrigger && !other.transform.IsChildOf(transform);
        bool ResolveOverlaps(ContactFilter2D filter)
        {
            for (int pass = 0; pass < 8; pass++)
            {
                int count = Physics2D.OverlapCircle((Vector2)transform.position + FeetOffset, FeetRadius, filter, overlaps);
                bool penetrated = false;
                for (int i = 0; i < count; i++)
                {
                    if (!BlocksMovement(overlaps[i])) continue;
                    var separation = feet.Distance(overlaps[i]);
                    if (!separation.isValid || !separation.isOverlapped) continue;
                    penetrated = true;
                    Vector2 corrected = (Vector2)transform.position + separation.normal * (separation.distance - CollisionSkin);
                    // Never resolve an outer wall by pushing the player off the map.
                    if (!FarmingMap2D.Instance.IsFloor(corrected)) continue;
                    transform.position = corrected;
                    Physics2D.SyncTransforms();
                }
                if (!penetrated && FarmingMap2D.Instance.IsFloor(transform.position))
                {
                    lastSafePosition = transform.position; hasSafePosition = true;
                    return true;
                }
            }
            // Conflicting corner contacts must not leave a permanently trapped player.
            if (hasSafePosition)
            {
                transform.position = lastSafePosition;
                Physics2D.SyncTransforms();
            }
            return false;
        }
        public void RefreshFocus()
        {
            if (session == null || Inventory == null) return;
            if (Focused) Focused.SetFocused(false);
            Focused = null;
            float best = session.Rules.InteractionRadius * session.Rules.InteractionRadius;
            foreach (var item in session.Pickups)
            {
                if (!item || item.State != PickupState.Available) continue;
                float distance = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
                if (distance > best || !FarmingMap2D.Instance.CanReach(transform.position, item.transform.position)) continue;
                best = distance; Focused = item;
            }
            if (Focused) Focused.SetFocused(true);
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
            else if (Focused != null)
            {
                if (!Inventory.TryAdd(Focused))
                { session.Notify($"공간 부족 · {Focused.Record.Weight}칸 필요 / {Inventory.Capacity - Inventory.Used}칸 남음"); return; }
                Focused.Carry(body, Inventory.Carried.Count - 1);
                session.Notify(Focused.Record.Type == FarmingObjectType.Person ? "사람을 들었습니다" : "물자를 챙겼습니다");
                Focused = null;
            }
            else if (Inventory.Used > 0)
            {
                var item = Inventory.RemoveLast(); Vector3 destination = FindDropPosition();
                bool deposit = session.Bunker.Contains(destination);
                if (deposit) session.Deposit(item);
                item.Throw(deposit ? session.Bunker.LandingPosition : destination, deposit, session.Rules);
                session.Notify(deposit ? "벙커에 투입했습니다" : "내려놓았습니다 · E로 다시 들기");
            }
            for (int i = 0; i < Inventory.Carried.Count; i++) Inventory.Carried[i].PlaceInHands(i);
            AnimatePose(false);
        }
        Vector3 FindDropPosition()
        {
            Vector2 start = transform.position;
            float distance = session.Rules.DropDistance;
            foreach (var hit in Physics2D.CircleCastAll(start, feet.radius, facing, distance))
                if (!hit.collider.isTrigger) distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .03f));
            Vector3 target = start + facing * distance;
            return FarmingMap2D.Instance.IsFloor(target) ? target : transform.position;
        }
        void AnimatePose(bool walking)
        {
            walkPhase += walking ? Time.deltaTime * 11 : 0;
            body.localPosition = new Vector3(0, walking ? Mathf.Abs(Mathf.Sin(walkPhase)) * .035f : 0, 0);
            sprite.sprite = Inventory != null && Inventory.Used > 0 ? carrySprite : idleSprite;
        }
        public void StopInteraction()
        {
            if (Focused) Focused.SetFocused(false);
            Focused = null;
            session.Bunker.SetReady(false); AnimatePose(false);
        }
    }
}
