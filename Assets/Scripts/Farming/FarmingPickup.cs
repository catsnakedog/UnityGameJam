using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
namespace ShelterRun
{
    public sealed class FarmingPickup : MonoBehaviour
    {
        [SerializeField] string recordId;
        [SerializeField] Transform visual;
        [SerializeField] GameObject focusMarker;
        [SerializeField] TMP_Text weightLabel;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Sprite standingSprite;
        [SerializeField] Sprite lyingSprite;
        [SerializeField] float displayHeight;
        FarmingDepth2D depth;
        SortingGroup group;
        float depositAlpha = 1;
        float labelHeight;
        Vector3 groundPosition;
        Vector3 launchGround;
        public string RecordId => recordId;
        public FarmingRecord Record { get; private set; }
        public PickupState State { get; private set; } = PickupState.Available;
        public Vector3 AimPosition => transform.position + Vector3.up * displayHeight;
        public void Initialize(FarmingRecord record)
        {
            Record = record; depth = GetComponent<FarmingDepth2D>(); group = GetComponent<SortingGroup>();
            labelHeight = weightLabel.transform.localPosition.y;
            weightLabel.text = $"{record.Weight}칸"; SetFocused(false);
        }
        public void SetFocused(bool value) { focusMarker.SetActive(value && State == PickupState.Available); }
        public void Carry(Transform anchor, int index)
        {
            State = PickupState.Carried; SetFocused(false); weightLabel.gameObject.SetActive(false);
            displayHeight = 0;
            transform.SetParent(anchor, false);
            depth.enabled = false;
            group.sortingOrder = 10 + index;
            PlaceInHands(index);
        }
        public void PlaceInHands(int index)
        {
            bool person = Record.Type == FarmingObjectType.Person;
            sprite.sprite = person ? lyingSprite : standingSprite;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = person ? new Vector3(0, .58f + index * .22f, 0)
                : new Vector3(index % 2 == 0 ? -.19f : .19f, .55f + index / 2 * .24f, 0);
            visual.localPosition = Vector3.zero;
        }
        public void Throw(Vector3 destination, bool deposit, FarmingRules rules)
        {
            var carrier = GetComponentInParent<FarmingPlayer>();
            launchGround = carrier ? carrier.transform.position : transform.position;
            transform.SetParent(null, true); State = PickupState.InFlight;
            depth.enabled = true;
            StartCoroutine(AnimateThrow(destination, deposit, rules));
        }
        IEnumerator AnimateThrow(Vector3 destination, bool deposit, FarmingRules rules)
        {
            Vector3 start = transform.position;
            for (float elapsed = 0; elapsed < rules.ThrowDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / rules.ThrowDuration);
                groundPosition = Vector3.Lerp(launchGround, destination, t);
                float arc = Mathf.Sin(t * Mathf.PI) * rules.ThrowArc;
                transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * arc;
                depth.GroundOffset = transform.position.y - groundPosition.y;
                yield return null;
            }
            transform.SetPositionAndRotation(destination, Quaternion.identity); depth.GroundOffset = 0;
            if (!deposit)
            {
                sprite.sprite = standingSprite;
                State = PickupState.Available; weightLabel.gameObject.SetActive(true); yield break;
            }
            for (float elapsed = 0; elapsed < rules.FadeDuration; elapsed += Time.deltaTime)
            { depositAlpha = 1 - Mathf.Clamp01(elapsed / rules.FadeDuration); yield return null; }
            State = PickupState.Deposited; gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            if (Record == null) return;
            var map = FarmingMap2D.Instance;
            var room = map ? map.RoomAt(State == PickupState.InFlight ? groundPosition : transform.position) : null;
            float visibility = State == PickupState.Carried ? 1 : room ? room.Reveal : 1;
            sprite.color = new Color(1, 1, 1, visibility * depositAlpha);
            var labelColor = weightLabel.color; labelColor.a = visibility; weightLabel.color = labelColor;
            if (State == PickupState.Available)
            {
                visual.localPosition = Vector3.up * displayHeight;
                focusMarker.transform.localPosition = Vector3.up * displayHeight;
                weightLabel.transform.localPosition = Vector3.up * (labelHeight + displayHeight);
            }
        }
    }
}
