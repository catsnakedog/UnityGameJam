using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ShelterRun
{
    // Scene-owned; all requests share one load and assets are released on scene teardown.
    public sealed class FarmingDataLoader : IDisposable
    {
        AsyncOperationHandle<FarmingDatabase> databaseHandle;
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
            databaseHandle = Addressables.LoadAssetAsync<FarmingDatabase>(FarmingDataKeys.Database);
            rulesHandle = Addressables.LoadAssetAsync<FarmingRules>(FarmingDataKeys.Rules);
            await Task.WhenAll(databaseHandle.Task, rulesHandle.Task);
            if (disposed) throw new ObjectDisposedException(nameof(FarmingDataLoader));
            if (databaseHandle.Status != AsyncOperationStatus.Succeeded || rulesHandle.Status != AsyncOperationStatus.Succeeded)
                throw new InvalidOperationException("Addressables: farming database/rules load failed.");
            Database = databaseHandle.Result;
            Rules = rulesHandle.Result;
            Database.Validate();
            Rules.Validate();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (databaseHandle.IsValid()) Addressables.Release(databaseHandle);
            if (rulesHandle.IsValid()) Addressables.Release(rulesHandle);
            Database = null;
            Rules = null;
        }
    }
}
