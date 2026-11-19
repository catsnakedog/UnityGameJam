using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using ShelterRun;
using UnityEngine;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterRoster : MonoBehaviour
    {
        [SerializeField] ShelterWalker survivorPrefab;
        [SerializeField] Transform[] spawnPoints;
        readonly List<CharacterIdentity> characters = new List<CharacterIdentity>();
        public IReadOnlyList<CharacterIdentity> Characters => characters.AsReadOnly();
        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        Task initialization;
        async void Start() { try { await InitializeAsync(); } catch (Exception e) { Debug.LogException(e, this); } }
        public Task InitializeAsync() => initialization ?? (initialization = LoadAsync());
        async Task LoadAsync()
        {
            try
            {
                await GameData.InitializeAsync();
                if (this == null) return;
                var table = GameData.GetTable(CharacterKeys.Table);
                var catalog = new CharacterCatalog(table);
                IEnumerable<string> ids = FarmingRunTransfer.People;
                if (Application.isEditor && !FarmingRunTransfer.HasResult)
                    ids = table.Rows.Select(row => row.Id);
                var definitions = new List<CharacterDefinition>();
                foreach (var id in ids) definitions.Add(catalog.Get(id));
                if (!survivorPrefab || spawnPoints == null || definitions.Count > spawnPoints.Length)
                    throw new InvalidOperationException("Shelter roster prefab/spawn points are missing or insufficient.");
                for (int i = 0; i < definitions.Count; i++)
                    if (!spawnPoints[i]) throw new InvalidOperationException("Missing shelter spawn point: " + i);
                for (int i = 0; i < definitions.Count; i++)
                {
                    var walker = Instantiate(survivorPrefab, spawnPoints[i].position, Quaternion.identity, transform);
                    walker.name = definitions[i].Id;
                    var identity = walker.GetComponent<CharacterIdentity>();
                    if (!identity) identity = walker.gameObject.AddComponent<CharacterIdentity>();
                    identity.Initialize(definitions[i]);
                    walker.GetComponent<SurvivorAnimation>()?.Bind(identity, walker);
                    characters.Add(identity);
                }
                IsReady = true;
                if (characters.Count == 0) GetComponent<ShelterCommander>()?.Report("NO RESCUED SURVIVORS - START FROM FARMING");
            }
            catch (Exception e)
            {
                if (this != null)
                {
                    foreach (var actor in characters) if (actor) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
                    characters.Clear(); LastError = e.Message;
                    GetComponent<ShelterCommander>()?.Report("CHARACTER DATA ERROR");
                }
                throw;
            }
        }
    }
}
