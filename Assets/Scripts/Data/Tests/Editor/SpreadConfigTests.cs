using System;
using System.Linq;
using GameJam.Data.Editor;
using NUnit.Framework;

namespace GameJam.Data.Tests
{
    public sealed class SpreadConfigTests
    {
        const string Header = "dataName,sheetName,id,gid,enabled,spreadsheetName,tabName,updatedAt\n";

        [Test]
        public void UserConfigBuildsUrlsAndUsesSheetNameAsFilename()
        {
            var rows = SpreadConfigReader.Parse("\uFEFF" + Header + "CsvData,Events.csv,doc_1-Ab,1622955486,TRUE,사건 목록,사건 탭,2026. 11. 18\nCsvData,FarmingData,doc2,0,1,파밍,파밍,오늘\n");
            Assert.That(rows.Select(x => x.tableName), Is.EqualTo(new[] { "Events", "FarmingData" }));
            Assert.That(SpreadsheetImporter.CsvUrl(rows[0].url), Is.EqualTo("https://docs.google.com/spreadsheets/d/doc_1-Ab/export?format=csv&gid=1622955486"));
            Assert.That(rows.All(x => x.enabled), Is.True);
        }

        [Test]
        public void DisabledRowsNeedNoUrlAndExtraMetadataHasNoEffect()
        {
            var rows = SpreadConfigReader.Parse(Header + "\"Group, with comma\",Events,doc1,0,true,\"multi\nline\",unused,date\nCsvData,Characters,,,FALSE,,,\n\n");
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[1].enabled, Is.False);
            Assert.That(rows[1].url, Is.Empty);
        }

        [Test]
        public void ColumnsCanBeReorderedAndTrailingEmptyColumnsIgnored()
        {
            var rows = SpreadConfigReader.Parse("enabled,gid,id,sheetName,\nTRUE,4,abc,Events,\n");
            Assert.That(rows.Single().url, Does.EndWith("gid=4"));
        }

        [TestCase("")]
        [TestCase("sheetName,id,gid\nEvents,doc1,0\n")]
        [TestCase("sheetName,id,gid,enabled,ID\nEvents,doc1,0,true,doc2\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0,yes\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0,\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0,false\n")]
        [TestCase("sheetName,id,gid,enabled\n../Events,doc1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nFolder/Events,doc1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nFolder\\Events,doc1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\n1Events,doc1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents.xlsx,doc1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc/1,0,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,-1,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0&evil=1,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0,true\nevents.csv,doc2,1,true\n")]
        [TestCase("sheetName,id,gid,enabled\nEvents,doc1,0,true,extra\n")]
        public void InvalidConfigIsRejectedBeforeAnyDownloads(string csv)
        {
            Assert.Throws<FormatException>(() => SpreadConfigReader.Parse(csv));
        }
    }
}
