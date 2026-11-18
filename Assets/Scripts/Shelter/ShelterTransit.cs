using System;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine;
using UnityGameJam.GameDev;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterTransit : MonoBehaviour
    {
        public ShelterNavigation navigation;
        public BunkerSimulation simulation;
        public GameDevManager development;
        public ShelterCommander commander;
        public ElevatorRules Rules { get; private set; }
        public bool IsReady => Rules;
        public string LastError { get; private set; }
        Task loading;
        void OnEnable()
        {
            if (!simulation || !development) return;
            simulation.AcquirePause(this); commander?.AcquireInputBlock(this);
            development.DevelopmentRestarted += ResetService;
            development.Changed += OnDevelopmentChanged;
            _ = StartSafely();
        }
        async Task StartSafely()
        {
            try { await InitializeAsync(); }
            catch (Exception e) { if (this) { LastError = e.Message; commander?.Report("엘리베이터 데이터 오류: " + e.Message); Debug.LogException(e, this); } }
            finally { if (this) { simulation?.ReleasePause(this); commander?.ReleaseInputBlock(this); } }
        }
        public Task InitializeAsync() => loading ?? (loading = LoadAsync());
        async Task LoadAsync()
        {
            var rules = await GameData.LoadSettingsAsync<ElevatorRules>(ElevatorRules.Address); rules.Validate();
            await simulation.InitializeAsync();
            if (!this) return;
            Rules = rules; navigation.Refresh();
            foreach (var lift in FindObjectsByType<ShelterElevator>(FindObjectsSortMode.None)) Register(lift);
            navigation.Refresh();
        }
        public void Register(ShelterElevator lift)
        { if (Rules && lift.gameObject.scene == gameObject.scene) lift.Configure(Rules, simulation, navigation); }
        public void ResetService()
        { foreach (var lift in FindObjectsByType<ShelterElevator>(FindObjectsSortMode.None)) if (lift.gameObject.scene == gameObject.scene) lift.ResetService(); }
        void OnDevelopmentChanged() { if (development.Phase != GameDevPhase.Developing) ResetService(); }
        void OnDisable()
        {
            ResetService();
            if (development) development.DevelopmentRestarted -= ResetService;
            if (development) development.Changed -= OnDevelopmentChanged;
            if (simulation) simulation.ReleasePause(this);
            if (commander) commander.ReleaseInputBlock(this);
        }
    }
}
