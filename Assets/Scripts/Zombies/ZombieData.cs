using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameJam.Zombies
{
    [Serializable]
    public sealed class ZombieDefinition
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] int firstWave;
        [SerializeField] int combat;
        [SerializeField] int hp;
        public string Id => id;
        public string DisplayName => displayName;
        public int FirstWave => firstWave;
        public int Combat => combat;
        public int Hp => hp;
    }

    public sealed class ZombieData : ScriptableObject
    {
        public const string Address = "data/zombies";
        [SerializeField] ZombieDefinition[] entries;
        [SerializeField, Min(2)] int diceSides;
        [SerializeField, Min(1)] int combatEveryTicks;
        [SerializeField, Min(1)] int attemptsPerDefender;
        [SerializeField, Min(1)] int damageToZombie;
        [SerializeField, Min(1)] int mentalDamage;
        [SerializeField, Min(0)] int mentalRecovery;
        [SerializeField, Min(0.1f)] float walkSpeed;
        [SerializeField, Min(0.1f)] float climbSpeed;
        [SerializeField, Min(0.01f)] float encounterDistance;
        [SerializeField, Min(0.01f)] float spawnOffset;
        public IReadOnlyList<ZombieDefinition> Entries => Array.AsReadOnly(entries);
        public int DiceSides => diceSides;
        public int CombatEveryTicks => combatEveryTicks;
        public int AttemptsPerDefender => attemptsPerDefender;
        public int DamageToZombie => damageToZombie;
        public int MentalDamage => mentalDamage;
        public int MentalRecovery => mentalRecovery;
        public float WalkSpeed => walkSpeed;
        public float ClimbSpeed => climbSpeed;
        public float EncounterDistance => encounterDistance;
        public float SpawnOffset => spawnOffset;

        public ZombieDefinition ForWave(int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave));
            ZombieDefinition best = null;
            foreach (var entry in entries)
                if (entry.FirstWave <= wave && (best == null || entry.FirstWave > best.FirstWave)) best = entry;
            return best ?? throw new InvalidOperationException("data.zombies has no first wave.");
        }

        public void Validate()
        {
            if (diceSides < 2 || combatEveryTicks < 1 || attemptsPerDefender < 1 || damageToZombie < 1 || mentalDamage < 1 || mentalRecovery < 0 ||
                !Positive(walkSpeed) || !Positive(climbSpeed) || !Positive(encounterDistance) || !Positive(spawnOffset))
                throw new InvalidOperationException("Invalid data.zombies combat/movement rules.");
            if (entries == null || entries.Length == 0) throw new InvalidOperationException("Empty data.zombies.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var waves = new HashSet<int>();
            foreach (var entry in entries)
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id) || string.IsNullOrWhiteSpace(entry.DisplayName) ||
                    entry.FirstWave < 1 || !waves.Add(entry.FirstWave) || entry.Combat < 1 || entry.Combat > diceSides || entry.Hp < 1)
                    throw new InvalidOperationException("Invalid/duplicate data.zombies entry.");
            if (!waves.Contains(1)) throw new InvalidOperationException("data.zombies requires firstWave=1.");
        }
        static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
