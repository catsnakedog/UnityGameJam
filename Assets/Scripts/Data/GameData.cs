using System;
using System.Threading;
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
        static CancellationTokenSource pendingSheets;
        static Dictionary<string, ScriptableObject> sessionAssets;
        static Func<GameDatabase, Dictionary<string, ScriptableObject>, CancellationToken, Task> readSession;
        static readonly HashSet<string> sessionAddresses = new HashSet<string>(StringComparer.Ordinal);
        static int generation;
        static readonly Dictionary<string, Task<UnityEngine.Object>> settings = new Dictionary<string, Task<UnityEngine.Object>>();
        static event Action ReleaseSettings;

        // Project-specific CSV projections live in the gameplay assembly.
        public static void ConfigureSessionSource(
            Func<GameDatabase, Dictionary<string, ScriptableObject>, CancellationToken, Task> read,
            params string[] addresses)
        {
            readSession = read;
            sessionAddresses.Clear();
            sessionAddresses.UnionWith(addresses);
        }

        public static async Task<T> LoadSettingsAsync<T>(string address) where T : UnityEngine.Object
        {
            if (sessionAddresses.Contains(address))
            {
                int version = generation;
                await InitializeAsync();
                if (version != generation) throw new OperationCanceledException("GameData was released while loading " + address);
                if (sessionAssets.TryGetValue(address, out var value)) return (T)(UnityEngine.Object)value;
            }
            return await LoadPackagedAsync<T>(address);
        }

        public static async Task<T> LoadPackagedAsync<T>(string address) where T : UnityEngine.Object
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
            var cancellation = new CancellationTokenSource();
            pendingSheets = cancellation;
            var staged = new Dictionary<string, ScriptableObject>(StringComparer.Ordinal);
            try
            {
                var packaged = await LoadPackagedAsync<GameDatabase>(CatalogAddress);
                cancellation.Token.ThrowIfCancellationRequested();
                packaged.Initialize();
                if (readSession != null) await readSession(packaged, staged, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                database = staged.TryGetValue(CatalogAddress, out var live) ? (GameDatabase)live : packaged;
                sessionAssets = staged;
            }
            catch (Exception error)
            {
                DestroySessionAssets(staged);
                if (version == generation) LastError = error;
                throw;
            }
            finally
            {
                if (pendingSheets == cancellation) pendingSheets = null;
                cancellation.Dispose();
            }
        }

        static void DestroySessionAssets(Dictionary<string, ScriptableObject> assets)
        {
            if (assets == null) return;
            foreach (var asset in assets.Values)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
                else UnityEngine.Object.DestroyImmediate(asset);
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
            generation++;
            pendingSheets?.Cancel();
            pendingSheets = null;
            DestroySessionAssets(sessionAssets);
            sessionAssets = null;
            ReleaseSettings?.Invoke();
            ReleaseSettings = null;
            settings.Clear();
            database = null;
            loading = null;
            LastError = null;
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
