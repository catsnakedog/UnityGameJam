using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameJam.Data
{
    /// <summary>Application-wide, main-thread data service. Initialize once before gameplay.</summary>
    public static class GameData
    {
        public const string CatalogAddress = "data/catalog";
        static GameDatabase database;
        static Task loading;
        static AsyncOperationHandle<GameDatabase> handle;
        static int generation;
        static readonly Dictionary<string, Task<UnityEngine.Object>> settings = new Dictionary<string, Task<UnityEngine.Object>>();
        static event Action ReleaseSettings;

        public static async Task<T> LoadSettingsAsync<T>(string address) where T : UnityEngine.Object
        {
            if (!settings.TryGetValue(address, out var task))
            {
                task = LoadSetting<T>(address, generation);
                settings.Add(address, task);
            }
            try { return (T)await task; }
            catch { if (settings.TryGetValue(address, out var current) && current == task) settings.Remove(address); throw; }
        }

        static async Task<UnityEngine.Object> LoadSetting<T>(string address, int version) where T : UnityEngine.Object
        {
            var operation = Addressables.LoadAssetAsync<T>(address);
            var completion = new TaskCompletionSource<T>();
            operation.Completed += value =>
            {
                if (value.Status == AsyncOperationStatus.Succeeded) completion.TrySetResult(value.Result);
                else completion.TrySetException(value.OperationException ?? new InvalidOperationException("Cannot load " + address));
            };
            try
            {
                var result = await completion.Task;
                if (version != generation) throw new OperationCanceledException("Settings released while loading: " + address);
                ReleaseSettings += () => { if (operation.IsValid()) Addressables.Release(operation); };
                return result;
            }
            catch { if (operation.IsValid()) Addressables.Release(operation); throw; }
        }
        public static bool IsReady => database != null;
        public static Exception LastError { get; private set; }

        public static Task InitializeAsync()
        {
            if (IsReady) return Task.CompletedTask;
            if (loading != null && !loading.IsFaulted && !loading.IsCanceled) return loading;
            LastError = null;
            loading = LoadAsync(generation);
            return loading;
        }

        static async Task LoadAsync(int version)
        {
            var operation = Addressables.LoadAssetAsync<GameDatabase>(CatalogAddress);
            handle = operation;
            try
            {
                // Completed works on platforms where AsyncOperationHandle.Task is unavailable.
                var completion = new TaskCompletionSource<GameDatabase>();
                operation.Completed += completed =>
                {
                    if (completed.Status == AsyncOperationStatus.Succeeded) completion.TrySetResult(completed.Result);
                    else completion.TrySetException(completed.OperationException ?? new InvalidOperationException("Cannot load " + CatalogAddress));
                };
                var loaded = await completion.Task;
                if (version != generation) throw new OperationCanceledException("GameData was released while loading.");
                loaded.Initialize();
                database = loaded;
            }
            catch (Exception error)
            {
                if (version == generation)
                {
                    LastError = error;
                    if (operation.IsValid()) Addressables.Release(operation);
                    handle = default;
                }
                throw;
            }
            finally
            {
                // Release during a pending operation defers releasing its handle until completion.
                if (version != generation && operation.IsValid()) Addressables.Release(operation);
            }
        }

        public static DataTable GetTable(string name)
        {
            if (!IsReady) throw new InvalidOperationException("Await GameData.InitializeAsync() before reading data.", LastError);
            return database.GetTable(name);
        }
        public static DataRow Get(string table, string id) => GetTable(table).Get(id);
        public static bool TryGet(string table, string id, out DataRow row) => GetTable(table).TryGet(id, out row);

        public static void Release()
        {
            bool pending = loading != null && !loading.IsCompleted;
            generation++;
            ReleaseSettings?.Invoke();
            ReleaseSettings = null;
            settings.Clear();
            database = null;
            loading = null;
            LastError = null;
            if (!pending && handle.IsValid()) Addressables.Release(handle);
            handle = default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }
    }
}
