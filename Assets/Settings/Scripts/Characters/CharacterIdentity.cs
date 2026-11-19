using GameJam.Audio;
using UnityEngine;
using UnityGameJam.Shelter;

namespace UnityGameJam.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterIdentity : MonoBehaviour, ICharacterEquipmentView
    {
        public int Hunger { get; private set; }
        public ShelterPickup EquippedPickup { get; private set; }
        ShelterItemDefinition equippedDefinition;
        public int Combat => checked(Definition.Combat + (equippedDefinition?.PlusAttack ?? 0));
        public int Planning => checked(Definition.Planning + (equippedDefinition?.PlusSkill ?? 0));
        public int Programming => checked(Definition.Programming + (equippedDefinition?.PlusSkill ?? 0));
        public int Art => checked(Definition.Art + (equippedDefinition?.PlusSkill ?? 0));
        EquippedItemView[] equipment = System.Array.Empty<EquippedItemView>();
        public System.Collections.Generic.IReadOnlyList<EquippedItemView> EquippedItems => equipment;
        public event System.Action EquipmentChanged;
        internal void SetEquipment(ShelterPickup item, EquippedItemView view, ShelterItemDefinition definition = null)
        {
            EquippedPickup = item;
            equippedDefinition = definition;
            equipment = item == null ? System.Array.Empty<EquippedItemView>() : new[] { view };
            EquipmentChanged?.Invoke();
        }
        internal void ResetSupplies()
        {
            Hunger = BunkerRules.HungerCapacity;
            if (EquippedPickup != null) SetEquipment(null, default);
        }
        internal void AdvanceHungerTick(int tick)
        {
            if (tick % rules.HungerEveryTicks != 0) return;
            int before = Hunger;
            Hunger = Mathf.Max(0, Hunger - rules.HungerLoss);
            if (before > 0 && Hunger == 0 && CanAct) Speak("hungry");
        }
        internal void Speak(string situation) => ShelterSpeech.Say(transform, Definition?.Role, situation);
        internal bool Eat()
        {
            if (!rules || Hunger == BunkerRules.HungerCapacity) return false;
            Hunger = Mathf.Min(BunkerRules.HungerCapacity, Hunger + rules.FoodRestore);
            return true;
        }
        public CharacterDefinition Definition { get; private set; }
        public string Id => Definition?.Id;
        public int CurrentMental { get; private set; }
        public bool IsFainted { get; private set; }
        public bool IsRestingAfterRescue { get; private set; }
        public bool CanAct => !IsFainted && CurrentMental > 0;
        public bool IsTired => rules && CanAct && CurrentMental <= rules.TiredMentalThreshold;
        BunkerRules rules;
        bool sleeping;
        internal BunkerRules Rules => rules;
        public void ConfigureMental(BunkerRules value) { rules = value; SetMental(CurrentMental); }
        public void ResetMental()
        {
            GetComponent<ShelterWalker>().LeaveTent();
            GetComponent<ShelterWalker>().ResetRescue();
            IsFainted = false;
            IsRestingAfterRescue = false;
            sleeping = false;
            SetMental(Definition.Mental);
            if (TryGetComponent<SurvivorEmotion>(out var emotion)) emotion.ResetForRun();
        }
        internal void BeginBedRest()
        {
            IsRestingAfterRescue = true;
            SetMental(CurrentMental);
        }
        public void AdvanceMentalTick()
        {
            if (!rules || !isActiveAndEnabled) return;
            var walker = GetComponent<ShelterWalker>();
            var room = walker.CurrentRoom;
            bool settled = room && room.IsWalkable && !walker.IsMoving && !walker.IsInElevator && room.ContainsFeet(transform.position);
            bool inBed = settled && walker.InTent && room.IsOperational;
            if (inBed && !sleeping && CanAct) Speak("sleep");
            sleeping = inBed;
            if (!settled) return;
            if (inBed && (CanAct || IsRestingAfterRescue))
                ApplyMentalChange(rules.SleepMentalPerTick, SurvivorEmotion.Cause.Rest);
            else if (room.roomType == ShelterRoomType.ZombieInvasion && CanAct)
                ApplyMentalChange(-rules.GuardMentalLossPerTick, SurvivorEmotion.Cause.GuardDuty);
        }
        internal int MentalAfterChange(int before, int delta)
        {
            int multiplier = delta < 0 && Hunger == 0 && rules ? rules.EmptyHungerMentalLossMultiplier : 1;
            return ClampMental(before + (long)delta * multiplier);
        }
        internal int ApplyMentalChange(int delta, SurvivorEmotion.Cause cause)
        {
            int before = CurrentMental;
            SetMental(MentalAfterChange(before, delta), cause);
            return CurrentMental - before;
        }
        public int ClampMental(long value) => (int)System.Math.Max(0, System.Math.Min(rules ? rules.MentalMaximum : int.MaxValue, value));
        [SerializeField] UnityEngine.UI.Image mentalFill;
        [SerializeField, Min(0)] int headClearancePixels = 2;
        RectTransform statusGauge;

        internal void PlaceStatusAboveHead(Sprite standing, Transform artwork, TextMesh nameLabel)
        {
            if (!standing || !mentalFill) return;
            var canvas = mentalFill.GetComponentInParent<Canvas>(true);
            if (!canvas) return;

            // Use the standing art once so breathing and walking never move the labels.
            statusGauge = (RectTransform)canvas.transform;
            float clearance = (float)headClearancePixels / standing.pixelsPerUnit;
            float targetY = artwork.TransformPoint(Vector3.up * (standing.bounds.max.y + clearance)).y;
            float currentY = statusGauge.TransformPoint(statusGauge.rect.min).y;
            var offset = Vector3.up * (targetY - currentY);
            statusGauge.position += offset;
            if (nameLabel) nameLabel.transform.position += offset;
        }

        public void RefreshStatusLabel()
        {
            var walker = GetComponent<ShelterWalker>();
            if (walker && walker.nameLabel && Definition != null)
            {
                var label = walker.nameLabel;
                label.text = CanAct ? Definition.DisplayName : Definition.DisplayName + " · 탈진";
                // Let the longer faint label extend left, clear of the speaker's bubble.
                var anchor = CanAct ? TextAnchor.LowerCenter : TextAnchor.LowerRight;
                if (statusGauge && label.anchor != anchor)
                {
                    label.anchor = anchor;
                    var position = label.transform.position;
                    float x = CanAct ? statusGauge.rect.center.x : statusGauge.rect.xMax;
                    position.x = statusGauge.TransformPoint(new Vector3(x, 0f)).x;
                    label.transform.position = position;
                }
            }
            if (mentalFill)
                mentalFill.fillAmount = rules ? (float)CurrentMental / rules.MentalMaximum : 0f;
        }
        public void Initialize(CharacterDefinition definition)
        {
            Definition = definition;
            CurrentMental = definition.Mental;
            var walker = GetComponent<ShelterWalker>();
            walker.characterName = definition.DisplayName;
            if (walker.nameLabel) walker.nameLabel.text = definition.DisplayName;
            RefreshStatusLabel();
        }
        public void SetMental(int value) => SetMental(value, SurvivorEmotion.Cause.MentalChange);
        internal void SetMental(int value, SurvivorEmotion.Cause cause)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            bool wasTired = IsTired;
            int before = CurrentMental;
            CurrentMental = ClampMental(value);
            if (CurrentMental == 0 && !IsFainted)
            {
                GameAudio.Play(GameSound.Faint);
                IsFainted = true;
                if (TryGetComponent<SurvivorEmotion>(out var emotion)) emotion.Hide();
                Speak("faint");
                GetComponent<ShelterWalker>().OnFainted();
                if (GetComponent<ShelterWalker>().InTent) IsRestingAfterRescue = true;
            }
            else if (IsRestingAfterRescue && CurrentMental >= rules.WakeMental)
            {
                var room = GetComponent<ShelterWalker>().CurrentRoom;
                if (room && room.roomType == ShelterRoomType.Sleeping && room.IsOperational)
                {
                    if (!GetComponent<ShelterWalker>().InTent || CurrentMental < rules.MentalMaximum) GameAudio.Play(GameSound.Wake);
                    IsFainted = false;
                    IsRestingAfterRescue = false;
                }
            }
            else if (!wasTired && IsTired)
            {
                Speak("low_mental");
            }
            if (rules && CurrentMental >= rules.MentalMaximum) GetComponent<ShelterWalker>().LeaveTent();
            // The combat exchange displays anger for this hit, including when its repeat is suppressed.
            if (cause != SurvivorEmotion.Cause.Combat && CanAct && before != CurrentMental && TryGetComponent<SurvivorEmotion>(out var reaction))
            {
                if (CurrentMental > before) reaction.ShowJoy(cause);
                else reaction.ShowGloom(cause);
            }
            RefreshStatusLabel();
        }
    }
}
