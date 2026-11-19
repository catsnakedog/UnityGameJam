using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ShelterRun
{
    [Serializable]
    public sealed class FarmingRecord
    {
        [SerializeField] string id;
        [SerializeField] FarmingObjectType type;
        [SerializeField] int weight;
        public string Id => id;
        public FarmingObjectType Type => type;
        public int Weight => weight;
        public FarmingRecord(string id, FarmingObjectType type, int weight)
        { this.id = id; this.type = type; this.weight = weight; }
    }

    [CreateAssetMenu(menuName = "Shelter Run/Farming Database")]
    public sealed class FarmingDatabase : ScriptableObject
    {
        [SerializeField] List<FarmingRecord> records = new List<FarmingRecord>();
        [SerializeField, HideInInspector] string sourceHash;
        public ReadOnlyCollection<FarmingRecord> Records => records.AsReadOnly();
        public string SourceHash => sourceHash;

        public FarmingRecord Get(string id)
        {
            foreach (var row in records) if (row.Id == id) return row;
            throw new KeyNotFoundException($"FarmingData ID not found: {id}");
        }

        public void Validate()
        {
            if (records.Count == 0) throw new InvalidOperationException("FarmingData is empty.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in records)
            {
                if (string.IsNullOrWhiteSpace(row.Id) || !ids.Add(row.Id))
                    throw new InvalidOperationException($"Invalid/duplicate FarmingData ID: {row.Id}");
                int min = row.Type == FarmingObjectType.Person ? 2 : 1;
                int max = row.Type == FarmingObjectType.Person ? 4 : 2;
                if (!Enum.IsDefined(typeof(FarmingObjectType), row.Type) || row.Weight < min || row.Weight > max)
                    throw new InvalidOperationException($"Invalid type/weight: {row.Id}");
            }
        }
        public static FarmingDatabase CreateSession(List<FarmingRecord> rows)
        {
            var result = CreateInstance<FarmingDatabase>();
            result.records = rows;
            try { result.Validate(); return result; }
            catch { Destroy(result); throw; }
        }
#if UNITY_EDITOR
        public void ApplyValidated(List<FarmingRecord> rows, string hash)
        {
            records = new List<FarmingRecord>(rows);
            sourceHash = hash;
        }
#endif
    }
}
