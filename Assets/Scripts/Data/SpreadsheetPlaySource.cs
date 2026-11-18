using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameJam.Data
{
    public static class SpreadsheetPlaySource
    {
        public const string BuildFile = "GameDataSource.txt";
#if UNITY_EDITOR
        public static Func<string> EditorUrl;
        public static Task<string> ReadUrlAsync(CancellationToken cancellation) => Task.FromResult(EditorUrl());
#else
        public static async Task<string> ReadUrlAsync(CancellationToken cancellation)
        {
            string path = Application.streamingAssetsPath + "/" + BuildFile;
            if (!path.Contains("://")) return File.ReadAllText(path);
            // Android and web players expose packaged StreamingAssets through a URI.
            using (var request = UnityWebRequest.Get(path))
            {
                request.timeout = 30;
                using (cancellation.Register(request.Abort))
                    await request.SendWebRequest();
                cancellation.ThrowIfCancellationRequested();
                return request.downloadHandler.text;
            }
        }
#endif
    }
}
