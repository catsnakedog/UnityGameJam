using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameJam.Data;

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
        public ZombieDefinition() { }

        internal ZombieDefinition(DataRow row)
        {
            id = row.Get<string>("id");
            displayName = row.Get<string>("displayName");
            firstWave = row.Get<int>("firstWave");
            combat = row.Get<int>("combat");
            hp = row.Get<int>("hp");
        }

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
        [SerializeField, Min(0.01f)] float minimumSpacing;
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
        public float MinimumSpacing => minimumSpacing;

        // A catalog-only connection uses the packaged rules until the rules tab is linked.
        public static ZombieData FromTables(DataTable catalog, DataTable rules, ZombieData packaged = null)
        {
            RequireColumns(catalog, "id:string", "displayName:string", "firstWave:int", "combat:int", "hp:int");
            if (rules == null && !packaged) throw new FormatException("ZombieRules or packaged zombie rules are required.");
            var result = packaged ? Instantiate(packaged) : CreateInstance<ZombieData>();
            try
            {
                result.entries = catalog.Rows.Select(row => new ZombieDefinition(row)).ToArray();
                if (rules != null) result.ReadRules(rules);
                result.Validate();
                return result;
            }
            catch
            {
                if (Application.isPlaying) Destroy(result); else DestroyImmediate(result);
                throw;
            }
        }

        void ReadRules(DataTable rules)
        {
            RequireColumns(rules, "id:string", "diceSides:int", "combatEveryTicks:int", "attemptsPerDefender:int",
                "damageToZombie:int", "mentalDamage:int", "mentalRecovery:int", "walkSpeed:float", "climbSpeed:float",
                "encounterDistance:float", "spawnOffset:float", "minimumSpacing:float");
            if (rules.Count != 1 || !rules.TryGet("default", out var row))
                throw new FormatException("ZombieRules requires exactly one row with id=default.");
            diceSides = row.Get<int>("diceSides");
            combatEveryTicks = row.Get<int>("combatEveryTicks");
            attemptsPerDefender = row.Get<int>("attemptsPerDefender");
            damageToZombie = row.Get<int>("damageToZombie");
            mentalDamage = row.Get<int>("mentalDamage");
            mentalRecovery = row.Get<int>("mentalRecovery");
            walkSpeed = row.Get<float>("walkSpeed");
            climbSpeed = row.Get<float>("climbSpeed");
            encounterDistance = row.Get<float>("encounterDistance");
            spawnOffset = row.Get<float>("spawnOffset");
            minimumSpacing = row.Get<float>("minimumSpacing");
        }

        static void RequireColumns(DataTable table, params string[] expected)
        {
            if (table.Columns.Count != expected.Length || expected.Any(field =>
                !table.Columns.Any(c => c.Name + ":" + c.Type == field)) || table.Columns[table.KeyIndex].Name != "id")
                throw new FormatException(table.Name + " schema must match Docs/DataSpecs/Zombies.md / ZombieRules.md.");
        }

        public static void ValidateMigration(ZombieData next, ZombieData current)
        {
            next.Validate();
            if (!current) throw new InvalidOperationException("Existing ZombieData is missing. Install the zombie system first.");
            var missing = current.Entries.Where(e => next.Entries.All(n => n.Id != e.Id)).Select(e => e.Id).ToArray();
            if (missing.Length > 0) throw new InvalidOperationException("Zombie IDs require migration before removal: " + string.Join(", ", missing));
        }

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
                !Positive(walkSpeed) || !Positive(climbSpeed) || !Positive(encounterDistance) || !Positive(spawnOffset) || !Positive(minimumSpacing))
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
