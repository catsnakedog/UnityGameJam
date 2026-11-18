using System.Collections.Generic;
using UnityEngine;

namespace ShelterRun
{
    public static class FarmingRunTransfer
    {
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
            People = System.Array.Empty<string>();
            Items = System.Array.Empty<FarmingRecord>();
            HasResult = false;
        }
    }
}
