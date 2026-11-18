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
            if (tick % rules.HungerEveryTicks == 0) Hunger = Mathf.Max(0, Hunger - rules.HungerLoss);
        }
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
        public void ConfigureMental(BunkerRules value) { rules = value; SetMental(CurrentMental); }
        public void ResetMental()
        {
            GetComponent<ShelterWalker>().ResetRescue();
            IsFainted = false;
            IsRestingAfterRescue = false;
            SetMental(Definition.Mental);
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
            if (!room || !room.IsWalkable || walker.IsMoving || walker.IsInElevator ||
                !room.ContainsFeet(transform.position)) return;
            if (room.roomType == ShelterRoomType.Sleeping && room.IsOperational && (CanAct || IsRestingAfterRescue))
                SetMental(ClampMental((long)CurrentMental + rules.SleepMentalPerTick));
            else if (room.roomType == ShelterRoomType.ZombieInvasion && CanAct)
                SetMental(Mathf.Max(0, CurrentMental - rules.GuardMentalLossPerTick));
        }
        public int ClampMental(long value) => (int)System.Math.Max(0, System.Math.Min(rules ? rules.MentalMaximum : int.MaxValue, value));
        [SerializeField] UnityEngine.UI.Image mentalFill;

        public void RefreshStatusLabel()
        {
            var walker = GetComponent<ShelterWalker>();
            if (walker && walker.nameLabel && Definition != null)
                walker.nameLabel.text = CanAct ? Definition.DisplayName : Definition.DisplayName + " · 탈진";
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
        public void SetMental(int value)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            CurrentMental = ClampMental(value);
            if (CurrentMental == 0 && !IsFainted)
            {
                IsFainted = true;
                GetComponent<ShelterWalker>().OnFainted();
            }
            else if (IsRestingAfterRescue && CurrentMental >= rules.WakeMental)
            {
                var room = GetComponent<ShelterWalker>().CurrentRoom;
                if (room && room.roomType == ShelterRoomType.Sleeping && room.IsOperational)
                {
                    IsFainted = false;
                    IsRestingAfterRescue = false;
                }
            }
            RefreshStatusLabel();
        }
    }
}
