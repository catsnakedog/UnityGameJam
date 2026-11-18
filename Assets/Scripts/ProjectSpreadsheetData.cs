using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameJam.Data;
using ShelterRun;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;
using UnityGameJam.Zombies;

public static class ProjectSpreadsheetData
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() => GameData.ConfigureSessionSource(ReadAsync, FarmingDataKeys.Database, ZombieData.Address);

    static async Task ReadAsync(GameDatabase packaged, Dictionary<string, ScriptableObject> staged, CancellationToken cancellation)
    {
        var url = await SpreadsheetPlaySource.ReadUrlAsync(cancellation);
        if (url.Length == 0) return;
        var connections = await GoogleSheets.DownloadConfigAsync(url, cancellation);
        var enabled = connections.Where(source => source.enabled).ToArray();
        var downloads = await Task.WhenAll(enabled.Select(source => DownloadAsync(source, cancellation)));
        cancellation.ThrowIfCancellationRequested();
        var sheets = downloads.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var farming = await GameData.LoadPackagedAsync<FarmingDatabase>(FarmingDataKeys.Database);
        var zombies = sheets.ContainsKey("Zombies") || sheets.ContainsKey("ZombieData")
            ? await GameData.LoadPackagedAsync<ZombieData>(ZombieData.Address) : null;
        cancellation.ThrowIfCancellationRequested();
        BuildSession(packaged, farming, zombies, sheets, staged);
        Debug.Log($"Data Manager: 스프레드시트 {sheets.Count}개를 읽었습니다. 이번 실행에 최신 값을 사용합니다.");
    }

    static async Task<KeyValuePair<string, string>> DownloadAsync(SpreadsheetSource source, CancellationToken cancellation)
    {
        try { return new KeyValuePair<string, string>(source.tableName, await GoogleSheets.DownloadCsvAsync(source.url, cancellation)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { throw new IOException(source.tableName + ": " + ex.Message, ex); }
    }

    // GameData owns staged objects and publishes them together only after this method succeeds.
    internal static void BuildSession(GameDatabase packaged, FarmingDatabase farming, ZombieData zombies,
        IReadOnlyDictionary<string, string> sheets, Dictionary<string, ScriptableObject> staged)
    {
        if (sheets.TryGetValue("FarmingData", out var farmingCsv))
        {
            var rows = FarmingCsv.Parse(farmingCsv);
            FarmingCsv.ValidateRemovals(farming.Records, rows);
            farming = FarmingDatabase.CreateSession(rows);
            staged.Add(FarmingDataKeys.Database, farming);
        }

        var tables = packaged.Tables.ToDictionary(table => table.Name, StringComparer.Ordinal);
        sheets.TryGetValue("Zombies", out var catalogCsv);
        sheets.TryGetValue("ZombieRules", out var rulesCsv);
        sheets.TryGetValue("ZombieData", out var aliasCsv);
        if ((catalogCsv != null && (aliasCsv != null || rulesCsv == null)) ||
            (rulesCsv != null && catalogCsv == null && aliasCsv == null))
            throw new FormatException("좀비 종류 탭 하나를 연결하세요. Zombies는 ZombieRules와 함께 활성화해야 합니다.");
        if (catalogCsv != null || aliasCsv != null)
        {
            // Older configs name the same catalog ZombieData. Its existing generated accessor
            // and the combat SO must see the same rows.
            var name = tables.ContainsKey("ZombieData") ? "ZombieData" : "Zombies";
            var catalog = CsvTableParser.Read(name + ".csv", catalogCsv ?? aliasCsv);
            var rules = rulesCsv == null ? null : CsvTableParser.Read("ZombieRules.csv", rulesCsv);
            var next = ZombieData.FromTables(catalog, rules, zombies);
            staged.Add(ZombieData.Address, next);
            ZombieData.ValidateMigration(next, zombies);
            if (tables.ContainsKey(name)) tables[name] = catalog;
        }

        var special = new[] { "FarmingData", "Zombies", "ZombieData", "ZombieRules" };
        foreach (var sheet in sheets)
        {
            if (special.Contains(sheet.Key)) continue;
            if (special.Contains(sheet.Key, StringComparer.OrdinalIgnoreCase) ||
                (!tables.ContainsKey(sheet.Key) && tables.Keys.Contains(sheet.Key, StringComparer.OrdinalIgnoreCase)))
                throw new FormatException("기존 시트 이름의 대소문자를 유지하세요: " + sheet.Key);
            var table = CsvTableParser.Read(sheet.Key + ".csv", sheet.Value);
            if (tables.TryGetValue(table.Name, out var previous)) ValidateCompatibility(previous, table);
            tables[table.Name] = table;
        }
        var live = GameDatabase.CreateSession(tables.Values.ToList());
        staged.Add(GameData.CatalogAddress, live);
        ValidateCatalogs(live.Tables, farming);
    }

    static void ValidateCompatibility(DataTable previous, DataTable next)
    {
        if (previous.Columns[previous.KeyIndex].Name != next.Columns[next.KeyIndex].Name ||
            previous.Columns.Any(column => !next.Columns.Any(c => c.Name == column.Name && c.Type == column.Type)))
            throw new FormatException(next.Name + ": 기존 접근자와 호환되지 않는 열 또는 키입니다. Data Manager에서 스키마 변경을 먼저 적용하세요.");
        foreach (var row in previous.Rows)
            if (!next.TryGet(row.Id, out _)) throw new FormatException(next.Name + ": 삭제된 ID를 Data Manager에서 먼저 검토하세요: " + row.Id);
    }

    public static void ValidateCatalogs(IReadOnlyList<DataTable> tables, FarmingDatabase farming)
    {
        new CharacterCatalog(tables.Single(t => t.Name == CharacterKeys.Table)).ValidateFarming(farming);
        var items = new ShelterItemCatalog(tables.Single(t => t.Name == ShelterItemCatalog.TableName));
        var events = tables.Single(t => t.Name == EventCatalog.TableName);
        items.ValidateSources(events, farming.Records);
        EventCatalog.FromTable(events, items);
        GameDevEndingCatalog.FromTable(tables.Single(t => t.Name == GameDevKeys.EndingsTable));
    }
}
