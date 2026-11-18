using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameJam.Data;
using GameJam.Data.Editor;
using ShelterRun;
using ShelterRun.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameJam.Zombies;

// Coordinates existing importers; no additional runtime loader or data store.
[InitializeOnLoad]
public static class ProjectSpreadsheetSync
{
    static bool importing;
    static ProjectSpreadsheetSync()
    {
        DataManagerWindow.ImportProjectSheets = ImportAsync;
        DataImporter.ValidateProjectContent = ZombieCsvImporter.ValidateCurrent;
    }

    public static SpreadsheetSource[] ZombieSources(IReadOnlyList<SpreadsheetSource> enabled)
    {
        var sources = enabled.Where(s => string.Equals(s.tableName, "Zombies", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.tableName, "ZombieRules", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (sources.Length == 0) return sources;
        if (sources.Length != 2 || sources.Count(s => s.tableName == "Zombies") != 1 || sources.Count(s => s.tableName == "ZombieRules") != 1)
            throw new FormatException("Zombies와 ZombieRules를 정확한 파일명으로 함께 활성화하세요.");
        return sources;
    }

    public static void Validate(IReadOnlyList<DataTable> tables, string farmingCsv)
    {
        var farmingRows = FarmingCsvImporter.Parse(farmingCsv);
        var current = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);
        if (!current) throw new InvalidOperationException("Existing FarmingDatabase is missing.");
        FarmingCsvImporter.ValidateRemovals(current.Records, farmingRows);
        var transient = ScriptableObject.CreateInstance<FarmingDatabase>();
        try
        {
            transient.ApplyValidated(farmingRows, "validation");
            transient.Validate();
            ProjectSpreadsheetData.ValidateCatalogs(tables, transient);
        }
        finally { UnityEngine.Object.DestroyImmediate(transient); }
    }

    public static async Task<string> ImportAsync(CancellationToken cancellation)
    {
        if (importing) throw new InvalidOperationException("시트 가져오기가 이미 실행 중입니다.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료한 후 가져오세요.");
        importing = true;
        ZombieData parsedZombies = null;
        try
        {
            cancellation.ThrowIfCancellationRequested();
            var settings = DataToolSettings.instance;
            string folder = settings.csvFolder;
            string configUrl = settings.spreadConfigUrl;
            byte[] previousCsv = File.ReadAllBytes(FarmingCsvImporter.CsvPath);
            var farming = AssetDatabase.LoadAssetAtPath<FarmingDatabase>(FarmingCsvImporter.DatabasePath);
            if (!farming) throw new InvalidOperationException("Existing FarmingDatabase is missing.");
            string previousDb = EditorJsonUtility.ToJson(farming, true);
            string previousCommonHash = DataImporter.SourcesHash(DataImporter.ReadSources(folder));
            var commonDb = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataImporter.DatabasePath);
            string previousCommonDb = commonDb ? EditorJsonUtility.ToJson(commonDb, true) : null;
            var zombieData = AssetDatabase.LoadAssetAtPath<ZombieData>(ZombieSetup.DataPath);
            string previousZombieDb = zombieData ? EditorJsonUtility.ToJson(zombieData, true) : null;
            byte[] previousZombies = File.ReadAllBytes(ZombieCsvImporter.CatalogPath);
            byte[] previousZombieRules = File.ReadAllBytes(ZombieCsvImporter.RulesPath);
            var connections = await SpreadConfigReader.DownloadAsync(configUrl, cancellation);
            var enabled = connections.Where(s => s.enabled).ToArray();
            var zombieSources = ZombieSources(enabled);
            var farmingSource = enabled.SingleOrDefault(s => string.Equals(s.tableName, "FarmingData", StringComparison.OrdinalIgnoreCase));
            if (farmingSource != null && farmingSource.tableName != "FarmingData") throw new FormatException("파밍 파일명은 FarmingData로 입력하세요.");
            var sources = enabled.Where(s => !SpreadConfigReader.UsesProjectImporter(s.tableName)).ToArray();
            foreach (var table in DataImporter.Preview(folder).Tables)
                if (sources.Any(s => string.Equals(s.tableName, table.Name, StringComparison.OrdinalIgnoreCase) && s.tableName != table.Name))
                    throw new FormatException("기존 파일명의 대소문자를 유지하세요: " + table.Name);
            var preview = sources.Length > 0 ? await SpreadsheetImporter.DownloadAsync(folder, sources, cancellation) : null;
            var plan = preview != null ? preview.Plan : DataImporter.Preview(folder);
            string farmingCsv = farmingSource != null ? await SpreadsheetImporter.DownloadCsvAsync(farmingSource.url, cancellation) : Encoding.UTF8.GetString(previousCsv).TrimStart('\uFEFF');
            Validate(plan.Tables, farmingCsv);
            string zombieCsv = null, zombieRulesCsv = null;
            if (zombieSources.Length > 0)
            {
                zombieCsv = await SpreadsheetImporter.DownloadCsvAsync(zombieSources.Single(s => s.tableName == "Zombies").url, cancellation);
                zombieRulesCsv = await SpreadsheetImporter.DownloadCsvAsync(zombieSources.Single(s => s.tableName == "ZombieRules").url, cancellation);
                parsedZombies = ZombieCsvImporter.Parse(zombieCsv, zombieRulesCsv);
                ZombieCsvImporter.ValidateMigration(parsedZombies, zombieData);
            }
            if (preview != null && plan.HasRemovals) throw new InvalidOperationException("삭제 또는 스키마 변경이 있습니다. 고급 미리보기에서 검토 후 적용하세요.");
            cancellation.ThrowIfCancellationRequested();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("다운로드 중 Play가 시작되어 적용하지 않았습니다.");
            if (settings.spreadConfigUrl != configUrl || settings.csvFolder != folder)
                throw new InvalidOperationException("다운로드 중 연결 설정이 변경되었습니다. 다시 가져오세요.");
            commonDb = AssetDatabase.LoadAssetAtPath<GameDatabase>(DataImporter.DatabasePath);
            if (previousCommonHash != DataImporter.SourcesHash(DataImporter.ReadSources(folder)) || previousCommonDb != (commonDb ? EditorJsonUtility.ToJson(commonDb, true) : null))
                throw new InvalidOperationException("다운로드 중 공용 데이터가 변경되었습니다. 다시 가져오세요.");
            if (!previousCsv.SequenceEqual(File.ReadAllBytes(FarmingCsvImporter.CsvPath)) || previousDb != EditorJsonUtility.ToJson(farming, true))
                throw new InvalidOperationException("다운로드 중 파밍 데이터가 변경되었습니다. 다시 가져오세요.");
            if (zombieSources.Length > 0 && (!previousZombies.SequenceEqual(File.ReadAllBytes(ZombieCsvImporter.CatalogPath)) ||
                !previousZombieRules.SequenceEqual(File.ReadAllBytes(ZombieCsvImporter.RulesPath)) ||
                previousZombieDb != (zombieData ? EditorJsonUtility.ToJson(zombieData, true) : null)))
                throw new InvalidOperationException("다운로드 중 좀비 데이터가 변경되었습니다. 다시 가져오세요.");

            // Keep the synchronous commit together even when generated code triggers compilation.
            EditorApplication.LockReloadAssemblies();
            try
            {
                try
                {
                    if (parsedZombies)
                    {
                        ZombieCsvImporter.WriteReport("planned", zombieData, "Validated sheets; replacing existing generated SO. Next: " + JsonUtility.ToJson(parsedZombies));
                        File.WriteAllText(ZombieCsvImporter.CatalogPath, zombieCsv, new UTF8Encoding(true));
                        File.WriteAllText(ZombieCsvImporter.RulesPath, zombieRulesCsv, new UTF8Encoding(true));
                        ZombieCsvImporter.Apply(parsedZombies, zombieData);
                        ZombieCsvImporter.WriteReport("applied", zombieData, "Zombies + ZombieRules sheets projected into the existing SO. Content build required.");
                    }
                    if (farmingSource != null)
                    {
                        File.WriteAllText(FarmingCsvImporter.CsvPath, farmingCsv, new UTF8Encoding(true));
                        FarmingCsvImporter.Import();
                    }
                    if (preview != null) SpreadsheetImporter.Apply(preview, false);
                }
                catch
                {
                    if (parsedZombies)
                    {
                        File.WriteAllBytes(ZombieCsvImporter.CatalogPath, previousZombies);
                        File.WriteAllBytes(ZombieCsvImporter.RulesPath, previousZombieRules);
                        EditorJsonUtility.FromJsonOverwrite(previousZombieDb, zombieData);
                        EditorUtility.SetDirty(zombieData);
                        AssetDatabase.SaveAssets();
                    }
                    if (farmingSource != null)
                    {
                        File.WriteAllBytes(FarmingCsvImporter.CsvPath, previousCsv);
                        EditorJsonUtility.FromJsonOverwrite(previousDb, farming);
                        EditorUtility.SetDirty(farming);
                        AssetDatabase.SaveAssets();
                    }
                    File.WriteAllText("Docs/DataHistory/" + DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fffffff") + "-sheet-sync-rollback.md",
                        "# 시트 일괄 가져오기 복구\n\n적용 실패로 이번에 변경한 파밍 및 좀비 CSV/SO를 이전 값으로 복원했다. 공용 CSV/DB/생성 코드 복구는 SpreadsheetImporter가 담당한다.\n", new UTF8Encoding(false));
                    throw;
                }
                finally { AssetDatabase.Refresh(); }
            }
            finally { EditorApplication.UnlockReloadAssemblies(); }
            return $"완료: SpreadConfig의 활성 {enabled.Length}개 시트 → CSV 저장 / 클래스 파싱 / DB 갱신. 배포용 데이터는 Addressables 콘텐츠 빌드를 누르세요.";
        }
        finally { if (parsedZombies) UnityEngine.Object.DestroyImmediate(parsedZombies); importing = false; }
    }
}
