using System;
using System.Collections.Generic;
using GameJam.Data;
using ShelterRun;

namespace UnityGameJam.Characters
{
    public sealed class CharacterDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Programming { get; }
        public int Art { get; }
        public int Planning { get; }
        public int Mental { get; }
        public string DesignId { get; }
        public string Role { get; }
        public string Description { get; }
        public int Combat { get; }
        public CharacterDefinition(DataRow row)
        {
            Id = row.Id; DisplayName = row.Get<string>("displayName");
            Programming = row.Get<int>("programming"); Art = row.Get<int>("art");
            Planning = row.Get<int>("planning"); Mental = row.Get<int>("mental");
            DesignId = row.Get<string>("designId"); Role = row.Get<string>("role");
            Description = row.Get<string>("description"); Combat = row.Get<int>("combat");
            if (string.IsNullOrWhiteSpace(DesignId) || string.IsNullOrWhiteSpace(Description) ||
                (Role != "기획" && Role != "아트" && Role != "개발") ||
                Programming < 1 || Programming > 6 || Art < 1 || Art > 6 || Planning < 1 || Planning > 6 || Combat < 1 || Combat > 6 ||
                Programming + Art + Planning + Combat != 12)
                throw new FormatException("Invalid character design ID, role or stat allocation: " + Id);
            if (string.IsNullOrWhiteSpace(DisplayName) || Programming < 0 || Art < 0 || Planning < 0 || Mental < 0)
                throw new FormatException("Invalid character name or negative ability/mental: " + Id);
        }
    }

    public sealed class CharacterCatalog
    {
        readonly Dictionary<string, CharacterDefinition> rows = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, CharacterDefinition> designRows = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
        public CharacterCatalog(DataTable table)
        {
            foreach (var row in table.Rows)
            {
                var definition = new CharacterDefinition(row);
                rows.Add(row.Id, definition);
                if (designRows.ContainsKey(definition.DesignId)) throw new FormatException("Duplicate character designId: " + definition.DesignId);
                designRows.Add(definition.DesignId, definition);
            }
            if (rows.Count == 0) throw new FormatException("Characters is empty.");
        }
        public CharacterDefinition Get(string id) => rows.TryGetValue(id, out var row) ? row : throw new KeyNotFoundException("Characters ID missing: " + id);
        public CharacterDefinition GetByDesignId(string id) => designRows.TryGetValue(id, out var row) ? row : throw new KeyNotFoundException("Character designId missing: " + id);
        public void ValidateFarming(FarmingDatabase farming)
        {
            foreach (var person in farming.Records)
                if (person.Type == FarmingObjectType.Person) Get(person.Id);
            foreach (var id in rows.Keys)
                if (farming.Get(id).Type != FarmingObjectType.Person) throw new FormatException("Character ID is not a farming Person: " + id);
        }
    }
}
