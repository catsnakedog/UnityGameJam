using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GameJam.Data;
using ShelterRun;
using UnityEngine;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string UseKind { get; }
        public bool IsEquipment => UseKind == "attack" || UseKind == "buff";
        public int MentalRestore { get; }
        public int PlusAttack { get; }
        public int PlusSkill { get; }
        public string SourceEvent { get; }
        public ShelterItemDefinition(DataRow row)
        {
            Id=row.Id; DisplayName=row.Get<string>("displayName"); UseKind=row.Get<string>("useKind");
            MentalRestore=row.Get<int>("mentalRestore"); SourceEvent=row.Get<string>("sourceEvent");
            PlusAttack = row.Get<int>("plusAttack");
            PlusSkill = row.Get<int>("plusSkill");
            if((UseKind!="mental" && UseKind!="hunger" && !IsEquipment) || MentalRestore<0 ||
                PlusAttack < 0 || PlusSkill < 0 || (!IsEquipment && (PlusAttack != 0 || PlusSkill != 0)) ||
                (UseKind=="hunger") != (Id==FarmingDataKeys.SpaghettiCan))
                throw new FormatException("Invalid item consumption definition: "+Id);
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
        public void ValidateSources(DataTable events, IEnumerable<FarmingRecord> farming)
        {
            var pickupIds = new HashSet<string>(farming.Where(row => row.Type == FarmingObjectType.Item).Select(row => row.Id), StringComparer.Ordinal);
            foreach(var row in definitions.Values)
                if (!string.IsNullOrEmpty(row.SourceEvent) && !events.TryGet(row.SourceEvent, out _) && !pickupIds.Contains(row.Id))
                    throw new FormatException($"Unknown source event {row.SourceEvent} for item {row.Id} without a farming pickup.");
        }
    }
    public sealed class ShelterPickup
    {
        public FarmingRecord Record { get; }
        public string Id { get; }
        public int RemainingServings { get; internal set; }
        internal ShelterPickup(string id, FarmingRecord record, int servings)
        {
            Id = id;
            Record = record;
            RemainingServings = servings;
        }
    }

    public readonly struct ShelterStock
    {
        public ShelterPickup Instance { get; }
        public FarmingRecord Pickup => Instance?.Record;
        public ShelterItemDefinition Supply { get; }
        public int Count { get; }
        public string Id => Instance?.Id ?? Supply?.Id;

        internal ShelterStock(ShelterPickup item, ShelterItemDefinition supply, int count)
        {
            Instance = item;
            Supply = supply;
            Count = count;
        }
    }

    public sealed class ShelterSupplies : MonoBehaviour
    {
        readonly Dictionary<string,int> counts=new Dictionary<string,int>(StringComparer.Ordinal);
        readonly List<ShelterPickup> pickups = new List<ShelterPickup>();
        public BunkerRules Rules { get; private set; }
        public int RunVersion { get; private set; }
        public ShelterItemCatalog Catalog { get; private set; }
        public event Action Changed;
        public void Initialize(ShelterItemCatalog catalog, BunkerRules rules)
        {
            bool firstLoad = Catalog == null;
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Rules = rules ? rules : throw new ArgumentNullException(nameof(rules));
            if (firstLoad) RestorePickups();
            Changed?.Invoke();
        }
        void RestorePickups()
        {
            pickups.Clear();
            foreach (var record in FarmingRunTransfer.Items)
                pickups.Add(new ShelterPickup(record.Id, record, record.Id == FarmingDataKeys.SpaghettiCan ? Rules.ServingsPerCan : 0));
        }

        public IEnumerable<ShelterStock> OwnedItems()
        {
            foreach (var item in pickups)
                yield return new ShelterStock(item, Catalog.Contains(item.Id) ? Catalog.Definition(item.Id) : null, 1);

            foreach (var pair in counts)
                if (pair.Value > 0)
                    yield return new ShelterStock(null, Catalog.Definition(pair.Key), pair.Value);
        }
        public bool TryUse(ShelterStock stock, CharacterIdentity actor, EquippedItemView view)
        {
            if (!Rules || !actor || !actor.isActiveAndEnabled || actor.Definition == null || actor.gameObject.scene != gameObject.scene) return false;
            var item = stock.Instance;
            var supply = stock.Supply;
            if (supply == null) return false;
            if (item != null && !pickups.Contains(item)) return false;
            if (item == null && (!counts.TryGetValue(supply.Id, out int count) || count <= 0)) return false;
            if (supply.UseKind == "mental")
            {
                if (!Rules.IsMentalConsumable(supply.Id)) return false;
                int mental = actor.ClampMental((long)actor.CurrentMental + supply.MentalRestore);
                if (mental == actor.CurrentMental) return false;
                actor.SetMental(mental);
                if (item != null) pickups.Remove(item);
                else counts[supply.Id]--;
            }
            else if (supply.UseKind == "hunger")
            {
                if (!actor.Eat()) return false;
                if (item == null)
                {
                    counts[supply.Id]--;
                    item = new ShelterPickup(supply.Id, null, Rules.ServingsPerCan);
                    pickups.Add(item);
                }
                if (--item.RemainingServings == 0) pickups.Remove(item);
            }
            else
            {
                if (!supply.IsEquipment) return false;
                if (item == null)
                {
                    counts[supply.Id]--;
                    item = new ShelterPickup(supply.Id, null, 0);
                }
                else pickups.Remove(item);
                if (actor.EquippedPickup != null) pickups.Add(actor.EquippedPickup);
                actor.SetEquipment(item, view, supply);
            }
            Changed?.Invoke();
            return true;
        }
        public bool TryUnequip(CharacterIdentity actor)
        {
            if (!actor || !actor.isActiveAndEnabled || actor.gameObject.scene != gameObject.scene || actor.EquippedPickup == null) return false;
            pickups.Add(actor.EquippedPickup);
            actor.SetEquipment(null, default);
            Changed?.Invoke();
            return true;
        }
        public int GetCount(string id)
        {
            if(Catalog==null) throw new InvalidOperationException("Shelter supplies are not initialized.");
            Catalog.Name(id);
            return checked((counts.TryGetValue(id,out int value)?value:0) + pickups.Count(item => item.Id == id));
        }
        public void SetCounts(IReadOnlyDictionary<string,int> values)
        {
            foreach(var pair in values) { Catalog.Name(pair.Key); if(pair.Value<0) throw new InvalidOperationException("Insufficient "+pair.Key); }
            foreach(var pair in values)
            {
                counts.TryGetValue(pair.Key, out int stack);
                int delta = pair.Value - GetCount(pair.Key);
                int fromInstances = Math.Max(0, -delta - stack);
                counts[pair.Key] = Math.Max(0, stack + delta);
                for (int i = pickups.Count - 1; i >= 0 && fromInstances > 0; i--)
                    if (pickups[i].Id == pair.Key) { pickups.RemoveAt(i); fromInstances--; }
            }
            Changed?.Invoke();
        }
        public void Add(string id,int delta) => SetCounts(new Dictionary<string,int>{{id,checked(GetCount(id)+delta)}});
        public void ResetRun()
        {
            RunVersion++;
            counts.Clear();
            if (Rules) RestorePickups();
            Changed?.Invoke();
        }
    }
}
