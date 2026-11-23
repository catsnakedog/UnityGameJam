using System.Collections.Generic;
using UnityEngine;
using UnityGameJam.Shelter;

namespace ShelterRun
{
    public static class FarmingRunTransfer
    {
        public static string SelectedStarter { get; private set; }
        public static BunkerDifficulty Difficulty { get; private set; } = BunkerDifficulty.Hard;
        public static bool BunkerTutorialPending { get; private set; }
        public static void BeginRun(string personId, BunkerDifficulty difficulty = BunkerDifficulty.Hard, bool tutorial = false)
        {
            Reset();
            SelectedStarter = personId;
            Difficulty = difficulty;
            BunkerTutorialPending = tutorial;
        }
        public static void FinishBunkerTutorial() => BunkerTutorialPending = false;
        public static IReadOnlyList<string> People { get; private set; } = System.Array.Empty<string>();
        public static IReadOnlyList<FarmingRecord> Items { get; private set; } = System.Array.Empty<FarmingRecord>();
        public static bool HasResult { get; private set; }
        public static void Capture(IEnumerable<string> securedIds, FarmingDatabase database)
        {
            var people = new List<string>();
            var items = new List<FarmingRecord>();
            var unique = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var id in securedIds)
            {
                var record = database.Get(id);
                if (record.Type == FarmingObjectType.Item) items.Add(record);
                else if (unique.Add(id)) people.Add(id);
            }
            People = people.AsReadOnly();
            Items = items.AsReadOnly();
            HasResult = true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            SelectedStarter = null;
            BunkerTutorialPending = false;
            Difficulty = BunkerDifficulty.Hard;
            ClearResults();
        }
        internal static void ClearResults()
        {
            People = System.Array.Empty<string>();
            Items = System.Array.Empty<FarmingRecord>();
            HasResult = false;
        }
    }
}
