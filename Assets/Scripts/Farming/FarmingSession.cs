using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShelterRun
{
    [DefaultExecutionOrder(-100)]
    public sealed class FarmingSession : MonoBehaviour
    {
        [SerializeField] FarmingPlayer player;
        [SerializeField] FarmingBunker bunker;
        [SerializeField] FarmingHud hud;
        [SerializeField] FarmingPickup[] pickupPrefabs;
        readonly List<FarmingPickup> pickups = new List<FarmingPickup>();
        readonly List<string> depositedIds = new List<string>();
        readonly HashSet<FarmingPickup> depositedObjects = new HashSet<FarmingPickup>();
        FarmingDataLoader loader;
        int startFrame;
        bool restarting;
        public FarmingPhase Phase { get; private set; } = FarmingPhase.Loading;
        public FarmingRules Rules => loader?.Rules;
        public FarmingPlayer Player => player;
        public FarmingBunker Bunker => bunker;
        public IReadOnlyList<FarmingPickup> Pickups => pickups;
        public IReadOnlyList<string> DepositedIds => depositedIds.AsReadOnly();
        public float Remaining { get; private set; }
        public int PeopleSaved { get; private set; }
        public int ItemsSaved { get; private set; }
        public int PeopleTotal { get; private set; }
        public bool CanAct => Phase == FarmingPhase.Running && Time.frameCount > startFrame;
        public event Action<FarmingSession> Completed;

        async void Start()
        {
            FarmingRunTransfer.Reset();
            hud.Bind(this);
            loader = new FarmingDataLoader();
            try
            {
                await loader.LoadAsync();
                if (this == null) return;
                await GameJam.Data.GameData.InitializeAsync();
                if (this == null) return;
                new UnityGameJam.Characters.CharacterCatalog(GameJam.Data.GameData.GetTable(UnityGameJam.Characters.CharacterKeys.Table)).ValidateFarming(loader.Database);
                player.Initialize(this);
                SpawnPickups();
                Remaining = Rules.Duration;
                Phase = FarmingPhase.Ready;
                hud.Prepare(Rules);
            }
            catch (Exception exception)
            {
                if (this == null) return;
                Phase = FarmingPhase.Failed;
                hud.ShowFailure(exception.Message);
                Debug.LogException(exception, this);
                loader.Dispose();
            }
        }

        void SpawnPickups()
        {
            var prefabs = pickupPrefabs.ToDictionary(prefab => prefab.RecordId, StringComparer.Ordinal);
            var placements = FarmingSpawnLayout.Create(FarmingMap2D.Instance, bunker, Rules, loader.Database);
            var starter = loader.Database.Get(Rules.StartingPersonId);
            foreach (var record in placements.Select(p => p.Record).Append(starter))
                if (!prefabs.ContainsKey(record.Id))
                    throw new InvalidOperationException($"Missing farming pickup prefab for {record.Id}.");

            FarmingPickup Spawn(FarmingRecord record, Vector3 position, Transform parent, Transform slot = null)
            {
                var item = Instantiate(prefabs[record.Id], position, Quaternion.identity, parent);
                item.Initialize(record, slot, record.Type == FarmingObjectType.Person ? player.ArtworkFor(record.Id) : null);
                pickups.Add(item);
                if (record.Type == FarmingObjectType.Person) PeopleTotal++;
                return item;
            }

            if (!player.Take(Spawn(starter, player.transform.position, null)))
                throw new InvalidOperationException("Failed to carry starting person " + starter.Id);

            foreach (var placement in placements)
            {
                var slot = placement.Tabletop;
                var parent = slot ? slot.parent : placement.Room.transform;
                Spawn(placement.Record, placement.Ground, parent, slot);
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (Phase == FarmingPhase.Ready && keyboard != null && keyboard.eKey.wasPressedThisFrame) Begin();
            else if (Phase == FarmingPhase.Running) Tick(Time.deltaTime);
            else if (Phase == FarmingPhase.Finished && keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
        }

        public void Begin()
        {
            if (Phase != FarmingPhase.Ready) return;
            startFrame = Time.frameCount;
            Phase = FarmingPhase.Running;
        }

        public void Tick(float deltaTime)
        {
            if (Phase != FarmingPhase.Running) return;
            Remaining = Mathf.Max(0, Remaining - Mathf.Max(0, deltaTime));
            if (Remaining > 0) return;
            Phase = FarmingPhase.Finished;
            player.StopInteraction();
            Completed?.Invoke(this);
        }

        public void Deposit(FarmingPickup item)
        {
            if (!CanAct || !depositedObjects.Add(item)) return;
            // Commit when thrown before the deadline; the short visual may finish after zero.
            depositedIds.Add(item.Record.Id);
            if (item.Record.Type == FarmingObjectType.Person) PeopleSaved++;
            else ItemsSaved++;
        }
        public void Notify(string message) { hud.Notify(message); }
        public void EnterShelter()
        {
            if (Phase != FarmingPhase.Finished || restarting) return;
            string scene = UnityGameJam.GameDev.GameDevKeys.ShelterScene;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("Shelter scene is missing from Build Settings: " + scene, this);
                return;
            }
            restarting = true;
            FarmingRunTransfer.Capture(depositedIds, loader.Database);
            SceneManager.LoadSceneAsync(scene);
        }
        public void Restart()
        {
            if (Phase != FarmingPhase.Finished || restarting) return;
            restarting = true;
            SceneManager.LoadSceneAsync(gameObject.scene.path);
        }
        void OnDestroy() { loader?.Dispose(); }
    }
}
