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
        public int FaintTicksRemaining { get; private set; }
        public bool CanAct => CurrentMental > 0;
        public bool IsTired => rules && CanAct && CurrentMental <= rules.TiredMentalThreshold;
        BunkerRules rules;
        int guardDutyTicks;
        public void ConfigureMental(BunkerRules value) { rules = value; SetMental(CurrentMental); }
        public void ResetMental() { guardDutyTicks = 0; FaintTicksRemaining = 0; SetMental(Definition.Mental); }
        internal void AdvanceGuardDutyTick()
        {
            var walker = GetComponent<ShelterWalker>();
            var room = walker.CurrentRoom;
            if (!isActiveAndEnabled || !CanAct || walker.IsInElevator || !room || !room.IsWalkable ||
                room.roomType != ShelterRoomType.ZombieInvasion || !room.ContainsFeet(transform.position))
            { guardDutyTicks = 0; return; }
            if (++guardDutyTicks < rules.GuardTicksPerMental) return;
            guardDutyTicks = 0;
            SetMental(CurrentMental - 1);
        }
        public void AdvanceMentalTick()
        {
            if (CurrentMental == 0 && FaintTicksRemaining > 0 && --FaintTicksRemaining == 0) SetMental(rules.WakeMental);
            RefreshStatusLabel();
        }
        public int ClampMental(long value) => (int)System.Math.Max(0, System.Math.Min(rules ? rules.MentalMaximum : int.MaxValue, value));
        public void RefreshStatusLabel()
        {
            var walker = GetComponent<ShelterWalker>();
            if (walker && walker.nameLabel && Definition != null)
                walker.nameLabel.text = Definition.DisplayName + "\n멘탈 " + CurrentMental + (CanAct ? "" : " · 탈진 " + FaintTicksRemaining + "T");
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
            int previous = CurrentMental;
            CurrentMental = ClampMental(value);
            if (CurrentMental == 0) guardDutyTicks = 0;
            if (CurrentMental > 0) FaintTicksRemaining = 0;
            else if (rules && (previous > 0 || FaintTicksRemaining == 0)) FaintTicksRemaining = rules.FaintDurationTicks;
            RefreshStatusLabel();
        }
    }
}
