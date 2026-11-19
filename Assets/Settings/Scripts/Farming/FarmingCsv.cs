using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameJam.Data;

namespace ShelterRun
{
    public static class FarmingCsv
    {
        public static void ValidateRemovals(IEnumerable<FarmingRecord> previous, List<FarmingRecord> next)
        {
            var ids = new HashSet<string>(next.Select(row => row.Id), StringComparer.Ordinal);
            var missing = previous.Where(row => !ids.Contains(row.Id)).ToList();
            if (missing.Count > 0) throw new InvalidOperationException("Farming IDs require migration: " + string.Join(", ", missing.Select(row => row.Id)));
        }

        public static List<FarmingRecord> Parse(string text)
        {
            var rows = CsvTableParser.Parse(text);
            // The shared spreadsheet template has description / type / name rows.
            // Keep FarmingData on its existing importer and database, never the common catalog.
            if (rows.Count >= 4 && rows[0].Count == 3 &&
                rows[1].SequenceEqual(new[] { "string", "string", "int" }) &&
                rows[2].SequenceEqual(new[] { "id", "type", "weight" }))
                rows.RemoveRange(0, 2);
            if (rows.Count < 2 || !rows[0].SequenceEqual(new[] { "id", "type", "weight" }))
                throw new FormatException("FarmingData: header must be id,type,weight with at least one row.");
            var result = new List<FarmingRecord>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0])) continue;
                if (row.Count != 3) throw new FormatException($"FarmingData row {i + 1}: expected 3 columns.");
                string id = row[0];
                if (string.IsNullOrWhiteSpace(id) || id.Any(char.IsWhiteSpace) || !ids.Add(id))
                    throw new FormatException($"FarmingData row {i + 1}: invalid/duplicate id {id}.");
                if (row[1] != "Person" && row[1] != "Item") throw new FormatException($"Invalid type at {id}: {row[1]}");
                var type = row[1] == "Person" ? FarmingObjectType.Person : FarmingObjectType.Item;
                if (!int.TryParse(row[2], NumberStyles.None, CultureInfo.InvariantCulture, out int weight))
                    throw new FormatException($"Invalid integer weight at {id}: {row[2]}");
                if (weight < (type == FarmingObjectType.Person ? 2 : 1) || weight > (type == FarmingObjectType.Person ? 4 : 2))
                    throw new FormatException($"Weight outside supported range at {id}: {weight}");
                result.Add(new FarmingRecord(id, type, weight));
            }
            if (result.Count == 0) throw new FormatException("FarmingData contains no records.");
            return result;
        }
    }
}
