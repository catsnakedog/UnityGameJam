using System.Collections.Generic;
using UnityEngine;

namespace ShelterRun
{
    public static class FarmingRunTransfer
    {
        static string[] people = System.Array.Empty<string>();
        public static IReadOnlyList<string> People => System.Array.AsReadOnly(people);
        public static bool HasResult { get; private set; }
        public static void Capture(IEnumerable<string> deposited, FarmingDatabase database)
        {
            var result = new List<string>();
            var unique = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var id in deposited)
                if (database.Get(id).Type == FarmingObjectType.Person && unique.Add(id)) result.Add(id);
            people = result.ToArray();
            HasResult = true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { people = System.Array.Empty<string>(); HasResult = false; }
    }
}
