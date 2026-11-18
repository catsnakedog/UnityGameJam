using System;
using System.Collections.Generic;
using GameJam.Data;
using System.Linq;
using System.Text.RegularExpressions;
using UnityGameJam.Shelter;

namespace UnityGameJam.Events
{
    public sealed class EventDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Content { get; }
        public IReadOnlyList<EventEffect> Yes { get; }
        public IReadOnlyList<EventEffect> No { get; }
        public string RequiredItem { get; }
        public int RequiredCount { get; }
        public EventDefinition(string id, string title, string content, IReadOnlyList<string> yes, IReadOnlyList<string> no, string requiredItem="", int requiredCount=0)
        {
            if (id==null || !Regex.IsMatch(id,@"^ev_[a-z0-9_]+$") || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content) || title.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new FormatException("Events[" + id + "]: ev_* id, single-line title and nonempty content required.");
            Id = id; Title = title; Content = content;
            Yes=EventEffect.ParseMany(yes); No=EventEffect.ParseMany(no);
            if(requiredItem==null || (requiredItem.Length==0 ? requiredCount!=0 : requiredCount<=0)) throw new FormatException("Invalid event inventory condition: "+id);
            RequiredItem=requiredItem; RequiredCount=requiredCount;
        }
        public bool Eligible(ShelterSupplies supplies) => RequiredItem.Length==0 || supplies.GetCount(RequiredItem)>=RequiredCount;
    }

    public sealed class EventCatalog
    {
        public const string TableName = "Events";
        readonly List<EventDefinition> entries;
        public IReadOnlyList<EventDefinition> Entries => entries.AsReadOnly();
        EventCatalog(List<EventDefinition> entries) { this.entries = entries; }
        public static EventCatalog FromTable(DataTable table, ShelterItemCatalog items)
        {
            if (table.Count == 0) throw new FormatException("Events must contain at least one event.");
            if (table.Columns.Count != 7 || table.Columns[table.KeyIndex].Name != "id") throw new FormatException("Events requires v2 choice schema.");
            var values = new List<EventDefinition>();
            foreach (var row in table.Rows)
            {
                var entry=new EventDefinition(row.Get<string>("id"),row.Get<string>("title"),row.Get<string>("content"),row.Get<IReadOnlyList<string>>("yesEffects"),row.Get<IReadOnlyList<string>>("noEffects"),row.Get<string>("requiredItem"),row.Get<int>("requiredCount"));
                foreach(var effect in entry.Yes.Concat(entry.No)) if(effect.Kind==EventEffectKind.Item) items.Name(effect.Target);
                if(entry.RequiredItem.Length>0) items.Name(entry.RequiredItem);
                values.Add(entry);
            }
            return new EventCatalog(values);
        }
        public EventDefinition Find(string id) => entries.Find(e => e.Id == id);
        public EventDefinition Pick(string previousId, ShelterSupplies supplies)
        {
            var candidates = entries.FindAll(e => e.Id != previousId && e.Eligible(supplies));
            return candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }
}
