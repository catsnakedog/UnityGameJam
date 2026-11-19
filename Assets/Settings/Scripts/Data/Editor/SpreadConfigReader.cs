using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameJam.Data.Editor
{
    // Editor connection metadata only; never imported into the game database.
    public static class SpreadConfigReader
    {
        // These snapshots retain their existing SO and are not common GameDatabase tables.
        public static bool UsesProjectImporter(string name) =>
            string.Equals(name, "FarmingData", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Zombies", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "ZombieRules", StringComparison.OrdinalIgnoreCase);

        public static Task<IReadOnlyList<SpreadsheetSource>> DownloadAsync(string configUrl, CancellationToken cancellation) =>
            GoogleSheets.DownloadConfigAsync(configUrl, cancellation);

        public static IReadOnlyList<SpreadsheetSource> Parse(string csv) => GoogleSheets.ParseConfig(csv);
    }
}
