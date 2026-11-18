using UnityEngine;
using UnityGameJam.Shelter;

namespace UnityGameJam.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterIdentity : MonoBehaviour
    {
        public CharacterDefinition Definition { get; private set; }
        public string Id => Definition?.Id;
        public int CurrentMental { get; private set; }
        public int FaintTicksRemaining { get; private set; }
        public bool CanAct => CurrentMental > 0;
        BunkerRules rules;
        public void ConfigureMental(BunkerRules value) { rules = value; SetMental(CurrentMental); }
        public void ResetMental() { FaintTicksRemaining = 0; SetMental(Definition.Mental); }
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
            if (CurrentMental > 0) FaintTicksRemaining = 0;
            else if (rules && (previous > 0 || FaintTicksRemaining == 0)) FaintTicksRemaining = rules.FaintDurationTicks;
            RefreshStatusLabel();
        }
    }
}
