using System;
using System.Collections.Generic;
using GameJam.Data;

namespace UnityGameJam.GameDev
{
    public sealed class GameDevEnding
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public double MinimumScore { get; }
        public bool Enabled { get; }

        public GameDevEnding(string id, string displayName, string description, double minimumScore, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new FormatException("Ending ID is empty or padded.");
            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(description)) throw new FormatException(id + ": ending text is required.");
            ValidateScore(minimumScore);
            Id = id; DisplayName = displayName; Description = description; MinimumScore = minimumScore; Enabled = enabled;
        }

        public static void ValidateScore(double score)
        {
            if (double.IsNaN(score) || double.IsInfinity(score) || score < 0)
                throw new ArgumentOutOfRangeException(nameof(score), "Ending score must be finite and nonnegative.");
        }
    }

    /// <summary>A validated read-only projection of the shared DB; no IO or separate asset loading.</summary>
    public sealed class GameDevEndingCatalog
    {
        readonly List<GameDevEnding> active = new List<GameDevEnding>();
        public IReadOnlyList<GameDevEnding> Active => active.AsReadOnly();

        public GameDevEndingCatalog(IEnumerable<GameDevEnding> endings)
        {
            if (endings == null) throw new ArgumentNullException(nameof(endings));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var thresholds = new HashSet<double>();
            foreach (var ending in endings)
            {
                if (ending == null || !ids.Add(ending.Id)) throw new FormatException("GameDevEndings: null or duplicate ending ID.");
                if (!ending.Enabled) continue;
                if (!thresholds.Add(ending.MinimumScore)) throw new FormatException("GameDevEndings: enabled endings must have distinct minimumScore values.");
                active.Add(ending);
            }
            active.Sort((a, b) => a.MinimumScore.CompareTo(b.MinimumScore));
            if (active.Count == 0 || active[0].MinimumScore != 0)
                throw new FormatException("GameDevEndings: an enabled ending at minimumScore=0 is required.");
        }

        public static GameDevEndingCatalog FromTable(DataTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            var endings = new List<GameDevEnding>();
            foreach (var row in table.Rows)
                endings.Add(new GameDevEnding(row.Id, row.Get<string>("displayName"), row.Get<string>("description"), row.Get<double>("minimumScore"), row.Get<bool>("enabled")));
            return new GameDevEndingCatalog(endings);
        }

        public string FormatScore(double totalScore)
        {
            var ending = Select(totalScore);
            double rounded = Math.Round(totalScore, 3, MidpointRounding.AwayFromZero);
            string text = rounded.ToString("0.000");
            if (Select(rounded) == ending) return text;
            return text + (totalScore < rounded ? " 미만" : " 초과");
        }

        public GameDevEnding Select(double totalScore)
        {
            GameDevEnding.ValidateScore(totalScore);
            for (int i = active.Count - 1; i >= 0; i--)
                if (totalScore >= active[i].MinimumScore) return active[i];
            throw new InvalidOperationException("Validated ending catalog has no baseline ending.");
        }
    }
}
