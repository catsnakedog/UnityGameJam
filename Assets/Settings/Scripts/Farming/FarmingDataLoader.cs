using System;
using System.Threading.Tasks;
using GameJam.Data;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ShelterRun
{
    // The game session owns the selected database. The scene owns its rules handle.
    public sealed class FarmingDataLoader : IDisposable
    {
        AsyncOperationHandle<FarmingRules> rulesHandle;
        Task loadTask;
        bool disposed;
        public FarmingDatabase Database { get; private set; }
        public FarmingRules Rules { get; private set; }

        public Task LoadAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(FarmingDataLoader));
            return loadTask ?? (loadTask = LoadInternal());
        }

        async Task LoadInternal()
        {
            var database = GameData.LoadSettingsAsync<FarmingDatabase>(FarmingDataKeys.Database);
            rulesHandle = Addressables.LoadAssetAsync<FarmingRules>(FarmingDataKeys.Rules);
            await Task.WhenAll(database, rulesHandle.Task);
            if (disposed) throw new ObjectDisposedException(nameof(FarmingDataLoader));
            if (rulesHandle.Status != AsyncOperationStatus.Succeeded)
                throw new InvalidOperationException("Addressables: farming database/rules load failed.");
            Database = await database;
            Rules = rulesHandle.Result;
            Database.Validate();
            Rules.Validate();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (rulesHandle.IsValid()) Addressables.Release(rulesHandle);
            Database = null;
            Rules = null;
        }
    }
}
