using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GameJam.Data.Editor
{
    // Editor connection metadata only; never imported into the game database.
    public static class SpreadConfigReader
    {
        public static async Task<IReadOnlyList<SpreadsheetSource>> DownloadAsync(string configUrl, CancellationToken cancellation)
        {
            try { return Parse(await SpreadsheetImporter.DownloadCsvAsync(configUrl, cancellation)); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { throw new IOException("SpreadConfig: " + ex.Message, ex); }
        }

        public static IReadOnlyList<SpreadsheetSource> Parse(string csv)
        {
            var rows = CsvTableReader.Parse(csv);
            if (rows.Count == 0) throw new FormatException("SpreadConfig가 비어 있습니다. SpreadConfig 탭의 gid를 확인하세요.");
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows[0].Count; i++)
            {
                string field = rows[0][i].Trim();
                if (field.Length == 0) continue;
                if (columns.ContainsKey(field)) throw new FormatException("SpreadConfig 중복 헤더: " + field);
                columns.Add(field, i);
            }
            foreach (string required in new[] { "sheetName", "id", "gid", "enabled" })
                if (!columns.ContainsKey(required)) throw new FormatException("SpreadConfig 필수 헤더 누락: " + required);
            var result = new List<SpreadsheetSource>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.All(string.IsNullOrWhiteSpace) || row[0].TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                string Cell(string name) => columns[name] < row.Count ? row[columns[name]].Trim() : "";
                try
                {
                    if (row.Skip(rows[0].Count).Any(v => !string.IsNullOrWhiteSpace(v))) throw new FormatException("헤더보다 많은 값이 있습니다.");
                    string filename = Cell("sheetName");
                    string name = filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? filename.Substring(0, filename.Length - 4) : filename;
                    if (!CsvTableReader.IsIdentifier(name)) throw new FormatException("sheetName은 경로가 아닌 영문/숫자/밑줄 파일명이어야 합니다: " + filename);
                    if (!names.Add(name)) throw new FormatException("중복 파일명: " + name);
                    bool enabled = (bool)DataValue.Parse("bool", Cell("enabled"));
                    string url = "";
                    if (enabled)
                    {
                        string id = Cell("id"), gid = Cell("gid");
                        if (!Regex.IsMatch(id, @"\A[A-Za-z0-9_-]+\z")) throw new FormatException("id에 Google 스프레드시트 문서 ID를 입력하세요.");
                        if (!Regex.IsMatch(gid, @"\A[0-9]+\z")) throw new FormatException("gid에 대상 탭의 숫자 ID를 입력하세요.");
                        url = $"https://docs.google.com/spreadsheets/d/{id}/edit?gid={gid}";
                        SpreadsheetImporter.CsvUrl(url);
                    }
                    result.Add(new SpreadsheetSource { enabled = enabled, tableName = name, url = url });
                }
                catch (FormatException ex) { throw new FormatException($"SpreadConfig {i + 1}행: {ex.Message}", ex); }
            }
            if (!result.Any(s => s.enabled)) throw new FormatException("SpreadConfig에 활성(enabled=TRUE) 연결이 하나 이상 필요합니다.");
            return result.AsReadOnly();
        }
    }
}
