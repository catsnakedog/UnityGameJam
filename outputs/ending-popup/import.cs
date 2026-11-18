using GameJam.Data.Editor;
using UnityGameJam.GameDev;
var settings = DataToolSettings.instance;
var source = settings.sheets.Single(s => s.tableName == GameDevKeys.EndingsTable);
var preview = await SpreadsheetImporter.DownloadAsync(settings.csvFolder, new[] { source }, System.Threading.CancellationToken.None);
var catalog = GameDevEndingCatalog.FromTable(preview.Plan.Tables.Single(t => t.Name == GameDevKeys.EndingsTable));
if (!catalog.Active.Select(e => e.MinimumScore).SequenceEqual(new double[] { 0, 0.3, 0.5 })) throw new Exception("Unexpected sheet thresholds; inspect before applying.");
SpreadsheetImporter.Apply(preview, false);
return string.Join("\n", preview.Plan.Changes);
