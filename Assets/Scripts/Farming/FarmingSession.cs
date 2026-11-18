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
        readonly List<string> securedIds = new List<string>();
        readonly HashSet<FarmingPickup> depositedObjects = new HashSet<FarmingPickup>();
        FarmingDataLoader loader;
        int startFrame;
        bool restarting;
        public FarmingPhase Phase { get; private set; } = FarmingPhase.Loading;
        public FarmingRules Rules => loader?.Rules;
        public FarmingPlayer Player => player;
        public FarmingBunker Bunker => bunker;
        public IReadOnlyList<FarmingPickup> Pickups => pickups;
        public IReadOnlyList<string> SecuredIds => securedIds.AsReadOnly();
        public float Remaining { get; private set; }
        public float PreparationRemaining { get; private set; }
        public int PeopleSaved { get; private set; }
        public int ItemsSaved { get; private set; }
        public int PeopleTotal { get; private set; }
        public bool CanAct => Phase == FarmingPhase.Running && Time.frameCount > startFrame;
        public bool CanExplore => (Phase == FarmingPhase.Preparing || Phase == FarmingPhase.Running) && Time.frameCount > startFrame;
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
            foreach (var placement in placements)
                if (!prefabs.ContainsKey(placement.Record.Id))
                    throw new InvalidOperationException($"Missing farming pickup prefab for {placement.Record.Id}.");

            PeopleTotal = loader.Database.Records.Count(record => record.Type == FarmingObjectType.Person);
            RecordSecured(loader.Database.Get(Rules.StartingPersonId));
            foreach (var placement in placements)
            {
                var record = placement.Record;
                var slot = placement.Tabletop;
                var parent = slot ? slot.parent : placement.Room.transform;
                var item = Instantiate(prefabs[record.Id], placement.Ground, Quaternion.identity, parent);
                item.Initialize(record, slot, record.Type == FarmingObjectType.Person ? player.ArtworkFor(record.Id) : null);
                pickups.Add(item);
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (Phase == FarmingPhase.Ready && keyboard != null && keyboard.eKey.wasPressedThisFrame) Begin();
            else if (Phase == FarmingPhase.Preparing || Phase == FarmingPhase.Running) Tick(Time.deltaTime);
            else if (Phase == FarmingPhase.Finished && keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
        }

        public void Begin()
        {
            if (Phase != FarmingPhase.Ready) return;
            startFrame = Time.frameCount;
            PreparationRemaining = Rules.PreparationDuration;
            Phase = FarmingPhase.Preparing;
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0, deltaTime);
            if (Phase == FarmingPhase.Preparing)
            {
                float elapsed = Mathf.Min(PreparationRemaining, deltaTime);
                PreparationRemaining -= elapsed;
                if (PreparationRemaining > 0) return;
                deltaTime -= elapsed;
                Phase = FarmingPhase.Running;
                hud.Notify("파밍 시작 · 사람과 물자를 벙커로 옮기세요");
            }
            if (Phase != FarmingPhase.Running) return;
            Remaining = Mathf.Max(0, Remaining - deltaTime);
            if (Remaining > 0) return;
            Phase = FarmingPhase.Finished;
            player.StopInteraction();
            Completed?.Invoke(this);
        }

        public void Deposit(FarmingPickup item)
        {
            if (!CanAct || !depositedObjects.Add(item)) return;
            // Commit when thrown before the deadline; the short visual may finish after zero.
            RecordSecured(item.Record);
        }
        void RecordSecured(FarmingRecord record)
        {
            securedIds.Add(record.Id);
            if (record.Type == FarmingObjectType.Person) PeopleSaved++;
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
            FarmingRunTransfer.Capture(securedIds, loader.Database);
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
