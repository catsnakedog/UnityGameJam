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
        readonly TaskCompletionSource<FarmingRules> rulesLoaded = new TaskCompletionSource<FarmingRules>();
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
            rulesHandle.Completed += OnRulesLoaded;
            await Task.WhenAll(database, rulesLoaded.Task);
            if (disposed) throw new ObjectDisposedException(nameof(FarmingDataLoader));
            Database = await database;
            Rules = await rulesLoaded.Task;
            Database.Validate();
            Rules.Validate();
        }

        void OnRulesLoaded(AsyncOperationHandle<FarmingRules> result)
        {
            if (result.Status == AsyncOperationStatus.Succeeded)
                rulesLoaded.TrySetResult(result.Result);
            else
                rulesLoaded.TrySetException(result.OperationException ?? new InvalidOperationException("Cannot load " + FarmingDataKeys.Rules));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (rulesHandle.IsValid())
            {
                rulesHandle.Completed -= OnRulesLoaded;
                Addressables.Release(rulesHandle);
            }
            rulesLoaded.TrySetCanceled();
            Database = null;
            Rules = null;
        }
    }
}
