using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace GameJam.Data.Editor
{
    public sealed class SpreadsheetPreview
    {
        public DataImportPlan Plan { get; internal set; }
        internal string OriginalHash;
        internal Dictionary<string, string> Downloads;
    }

    public static class SpreadsheetImporter
    {
        public static async Task<string> DownloadCsvAsync(string url, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            using (var request = UnityWebRequest.Get(CsvUrl(url)))
            {
                request.timeout = 30;
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellation.IsCancellationRequested) { request.Abort(); cancellation.ThrowIfCancellationRequested(); }
                    await Task.Delay(25);
                }
                cancellation.ThrowIfCancellationRequested();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException($"Sheet download failed ({request.responseCode}). Check link viewing access. {request.error}");
                string csv = request.downloadHandler.text.TrimStart('\uFEFF');
                string contentType = request.GetResponseHeader("Content-Type") ?? "";
                if (contentType.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0 || csv.TrimStart().StartsWith("<", StringComparison.Ordinal))
                    throw new FormatException("Received a login/HTML page instead of CSV. Check sheet sharing.");
                return csv;
            }
        }

        public static string CsvUrl(string input)
        {
            if (!Uri.TryCreate(input?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "docs.google.com")
                throw new FormatException("Enter an HTTPS Google Sheets link (docs.google.com).");
            var published = Regex.Match(uri.AbsolutePath, @"^/spreadsheets/d/e/([A-Za-z0-9_-]+)/pub$");
            string gid = Regex.Match(uri.Query + "&" + uri.Fragment.TrimStart('#'), @"(?:[?&#]|^)gid=(\d+)").Groups[1].Value;
            if (gid.Length == 0) gid = "0";
            if (published.Success) return $"https://docs.google.com/spreadsheets/d/e/{published.Groups[1].Value}/pub?output=csv&single=true&gid={gid}";
            var match = Regex.Match(uri.AbsolutePath, @"^/spreadsheets/d/([A-Za-z0-9_-]+)(?:/|$)");
            if (!match.Success) throw new FormatException("Invalid Google Sheets document link.");
            return $"https://docs.google.com/spreadsheets/d/{match.Groups[1].Value}/export?format=csv&gid={gid}";
        }

        public static async Task<SpreadsheetPreview> DownloadAsync(string folder, IEnumerable<SpreadsheetSource> sources, CancellationToken cancellation)
        {
            var local = DataImporter.ReadSources(folder);
            var originalHash = DataImporter.SourcesHash(local);
            var downloads = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Capture editable settings before the first await.
            var selected = sources.Where(s => s.enabled).Select(s => new SpreadsheetSource { tableName = s.tableName, url = s.url }).ToArray();
            if (selected.Length == 0) throw new InvalidOperationException("Add and enable at least one spreadsheet source.");
            foreach (var source in selected)
            {
                if (!CsvTableReader.IsIdentifier(source.tableName)) throw new FormatException("Choose a valid table filename for each sheet.");
                string filename = source.tableName + ".csv";
                if (downloads.ContainsKey(filename)) throw new FormatException("Duplicate spreadsheet table: " + source.tableName);
                string existing = local.Keys.FirstOrDefault(k => string.Equals(Path.GetFileNameWithoutExtension(k), source.tableName, StringComparison.OrdinalIgnoreCase));
                if (existing != null) filename = existing;
                string csv;
                try { csv = await DownloadCsvAsync(source.url, cancellation); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { throw new IOException(source.tableName + ": " + ex.Message, ex); }
                CsvTableReader.Read(filename, csv);
                downloads.Add(filename, csv);
                local[filename] = csv;
            }
            return new SpreadsheetPreview { OriginalHash = originalHash, Downloads = downloads, Plan = DataImporter.PreviewSources(folder, local) };
        }

        public static void Apply(SpreadsheetPreview preview, bool allowRemovals)
        {
            var current = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>(DataImporter.DatabasePath);
            string previousJson = current ? UnityEditor.EditorJsonUtility.ToJson(current, true) : null;
            if (DataImporter.SourcesHash(DataImporter.ReadSources(preview.Plan.Folder)) != preview.OriginalHash || previousJson != preview.Plan.PreviousJson)
                throw new InvalidOperationException("Local CSV or database changed during download. Fetch a fresh preview.");
            if (preview.Plan.HasRemovals && !allowRemovals) throw new InvalidOperationException("Review and enable Allow removals first.");
            string folder = DataImporter.ValidateFolder(preview.Plan.Folder);
            var backups = new Dictionary<string, byte[]>();
            try
            {
                foreach (var pair in preview.Downloads)
                {
                    string path = Path.Combine(folder, pair.Key);
                    backups[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
                    // Excel recognizes UTF-8 on double-click when the CSV includes its BOM.
                    File.WriteAllText(path, pair.Value, new UTF8Encoding(true));
                }
                DataImporter.Apply(preview.Plan, allowRemovals);
            }
            catch
            {
                foreach (var pair in backups)
                    if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value);
                UnityEditor.AssetDatabase.Refresh();
                throw;
            }
        }
    }
}
