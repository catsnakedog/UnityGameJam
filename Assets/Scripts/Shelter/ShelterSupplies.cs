using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameJam.Data;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string UseKind { get; }
        public int MentalRestore { get; }
        public string SourceEvent { get; }
        public ShelterItemDefinition(DataRow row)
        {
            Id=row.Id; DisplayName=row.Get<string>("displayName"); UseKind=row.Get<string>("useKind");
            MentalRestore=row.Get<int>("mentalRestore"); SourceEvent=row.Get<string>("sourceEvent");
            if(UseKind!="mental" || MentalRestore<0) throw new FormatException("Invalid item consumption definition: "+Id);
        }
    }
    public sealed class ShelterItemCatalog
    {
        public const string TableName="ShelterItems";
        readonly Dictionary<string,string> names=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,ShelterItemDefinition> definitions=new Dictionary<string,ShelterItemDefinition>(StringComparer.Ordinal);
        public ShelterItemCatalog(DataTable table)
        {
            foreach(var row in table.Rows)
            {
                string id=row.Get<string>("id"), name=row.Get<string>("displayName");
                if(!Regex.IsMatch(id,@"^[a-z][a-z0-9_]*$") || string.IsNullOrWhiteSpace(name)) throw new FormatException("Invalid ShelterItems row: "+id);
                names.Add(id,name);
                definitions.Add(id,new ShelterItemDefinition(row));
            }
            if(names.Count==0) throw new FormatException("ShelterItems is empty.");
        }
        public bool Contains(string id) => id!=null && names.ContainsKey(id);
        public string Name(string id) => names.TryGetValue(id,out var name)?name:throw new KeyNotFoundException("Unknown shelter item: "+id);
        public ShelterItemDefinition Definition(string id) => definitions.TryGetValue(id,out var row)?row:throw new KeyNotFoundException("Unknown shelter item: "+id);
        public void ValidateSources(DataTable events)
        {
            foreach(var row in definitions.Values)
                if(!string.IsNullOrEmpty(row.SourceEvent) && !events.TryGet(row.SourceEvent,out _)) throw new FormatException("Unknown item sourceEvent: "+row.SourceEvent);
        }
    }
    public sealed class ShelterSupplies : MonoBehaviour
    {
        readonly Dictionary<string,int> counts=new Dictionary<string,int>(StringComparer.Ordinal);
        public ShelterItemCatalog Catalog { get; private set; }
        public event Action Changed;
        public void Initialize(ShelterItemCatalog catalog) { Catalog=catalog??throw new ArgumentNullException(nameof(catalog)); }
        public int GetCount(string id)
        {
            if(Catalog==null) throw new InvalidOperationException("Shelter supplies are not initialized.");
            Catalog.Name(id);
            return counts.TryGetValue(id,out int value)?value:0;
        }
        public void SetCounts(IReadOnlyDictionary<string,int> values)
        {
            foreach(var pair in values) { Catalog.Name(pair.Key); if(pair.Value<0) throw new InvalidOperationException("Insufficient "+pair.Key); }
            foreach(var pair in values) counts[pair.Key]=pair.Value;
            Changed?.Invoke();
        }
        public void Add(string id,int delta) => SetCounts(new Dictionary<string,int>{{id,checked(GetCount(id)+delta)}});
        public void ResetRun() { counts.Clear(); Changed?.Invoke(); }
    }
}
