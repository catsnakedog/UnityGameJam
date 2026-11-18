using System;
using System.Collections.Generic;
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
        [SerializeField] FarmingPickup[] pickups;
        readonly List<string> depositedIds = new List<string>();
        readonly HashSet<FarmingPickup> depositedObjects = new HashSet<FarmingPickup>();
        FarmingDataLoader loader;
        int startFrame;
        bool restarting;
        public FarmingPhase Phase { get; private set; } = FarmingPhase.Loading;
        public FarmingRules Rules => loader?.Rules;
        public FarmingPlayer Player => player;
        public FarmingBunker Bunker => bunker;
        public IReadOnlyList<FarmingPickup> Pickups => Array.AsReadOnly(pickups);
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
                foreach (var item in pickups)
                {
                    item.Initialize(loader.Database.Get(item.RecordId));
                    if (item.Record.Type == FarmingObjectType.Person) PeopleTotal++;
                }
                player.Initialize(this);
                Remaining = Rules.Duration;
                Phase = FarmingPhase.Ready;
                hud.PrepareSlots(Rules.Capacity);
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
