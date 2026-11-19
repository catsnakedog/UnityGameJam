using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGameJam.Shelter;
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
        SurvivorAnimation.Artwork artwork;
        SurvivorAnimation.Pose waitingPose;
        readonly SurvivorAnimation.Playback playback = new SurvivorAnimation.Playback();
        float displayHeight;
        FarmingDepth2D depth;
        SortingGroup group;
        float depositAlpha = 1;
        Vector3 groundPosition;
        Vector3 launchGround;
        public string RecordId => recordId;
        public FarmingRecord Record { get; private set; }
        public PickupState State { get; private set; } = PickupState.Available;
        public Vector3 AimPosition => transform.position + Vector3.up * displayHeight;
        public void Initialize(FarmingRecord record, Transform tabletop, SurvivorAnimation.Artwork characterArt)
        {
            if (record.Type == FarmingObjectType.Person && characterArt?.standing == null)
                throw new InvalidOperationException("Missing farming person art for " + record.Id);
            artwork = characterArt;
            waitingPose = artwork != null && artwork.workingUsesChair
                ? SurvivorAnimation.Pose.Type : SurvivorAnimation.Pose.Idle;
            if (artwork != null) sprite.sprite = artwork.standing;
            Record = record; depth = GetComponent<FarmingDepth2D>(); group = GetComponent<SortingGroup>();
            if (tabletop)
            {
                displayHeight = transform.InverseTransformPoint(tabletop.position).y;
                depth.enabled = false;
                group.sortingOrder = 20 + tabletop.GetSiblingIndex();
            }
            bool isItem = record.Type == FarmingObjectType.Item;
            weightLabel.gameObject.SetActive(isItem);
            if (isItem) weightLabel.text = $"{record.Weight}칸";
            SetFocused(false);
            LateUpdate();
        }
        public void SetFocused(bool value) { focusMarker.SetActive(value && State == PickupState.Available); }
        public void Carry(Transform anchor, int index)
        {
            waitingPose = SurvivorAnimation.Pose.Idle;
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
            sprite.sprite = person ? artwork.Get(SurvivorAnimation.Pose.Carried).firstFrame : standingSprite;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = person ? new Vector3(0, .58f + index * .22f, 0)
                : new Vector3(index % 2 == 0 ? -.19f : .19f, .55f + index / 2 * .24f, 0);
            visual.localRotation = Quaternion.identity;
            visual.localPosition = person ? -Vector3.Scale(visual.localScale, sprite.sprite.bounds.center) : Vector3.zero;
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
                yield return null;
            }
            transform.SetPositionAndRotation(destination, Quaternion.identity);
            if (!deposit)
            {
                visual.localRotation = Quaternion.identity;
                visual.localPosition = Vector3.zero;
                sprite.sprite = artwork != null ? artwork.standing : standingSprite;
                State = PickupState.Available;
                weightLabel.gameObject.SetActive(Record.Type == FarmingObjectType.Item);
                yield break;
            }
            for (float elapsed = 0; elapsed < rules.FadeDuration; elapsed += Time.deltaTime)
            { depositAlpha = 1 - Mathf.Clamp01(elapsed / rules.FadeDuration); yield return null; }
            State = PickupState.Deposited; gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            if (Record == null) return;
            var map = FarmingMap2D.Instance;
            float visibility = State == PickupState.Carried ? 1 : map ? map.VisibilityAt(State == PickupState.InFlight ? groundPosition : transform.position) : 1;
            sprite.color = new Color(1, 1, 1, visibility * depositAlpha);
            if (State == PickupState.Available)
            {
                if (artwork != null && visibility > 0f)
                    playback.Sample(artwork, waitingPose, SurvivorAnimation.Pose.Idle, sprite, Time.deltaTime);
                visual.localPosition = Vector3.up * displayHeight;
                focusMarker.transform.localPosition = Vector3.up * displayHeight;
                if (Record.Type == FarmingObjectType.Item)
                {
                    var color = weightLabel.color;
                    color.a = visibility;
                    weightLabel.color = color;
                }
            }
        }
    }
}
