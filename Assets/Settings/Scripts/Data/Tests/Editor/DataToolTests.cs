using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GameJam.Data.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GameJam.Data.Tests
{
    public sealed class DataToolTests
    {
        [Test]
        public void SpreadsheetTemplateWithoutMarkerPreservesTypesAndEffects()
        {
            const string csv = "사건 ID,본문,선택 효과,필요 수량\nstring,string,string[],int\nid,content,effects,count\nev_radio,\"한글, \"\"방송\"\"\",\"MENTAL(all, -5)|PROGRESS(open, +1)\",0\n";
            var table = CsvTableReader.Read("Events.csv", csv);
            Assert.That(table.Columns.Count, Is.EqualTo(4));
            Assert.That(table.Get("ev_radio").Get<string>("content"), Is.EqualTo("한글, \"방송\""));
            Assert.That(table.Get("ev_radio").Get<IReadOnlyList<string>>("effects"), Has.Count.EqualTo(2));
            Assert.That(table.Get("ev_radio").Get<int>("count"), Is.Zero);
        }

        [Test]
        public void SpreadsheetBlankDescriptionRowAndDuplicateKeys()
        {
            var table = CsvTableReader.Read("Items.csv", ",\nstring,int\nid,count\na,2\n");
            Assert.That(table.Get("a").Get<int>("count"), Is.EqualTo(2));
            Assert.Throws<FormatException>(() => CsvTableReader.Read("Items.csv", ",\nstring,int\nid,count\na,2\na,3\n"));
        }

        [Test]
        public void OrdinaryUntypedHeaderIsNeverMisreadAsSpreadsheetMetadata()
        {
            var table = CsvTableReader.Read("Items.csv", "id,label\nstring,string\nid,label\n");
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.Get("string").Get<string>("label"), Is.EqualTo("string"));
        }

        [Test]
        public void QuotedCsvPreservesUnicodeCommaEscapedQuoteAndNewline()
        {
            var table = CsvTableReader.Read("Items.csv", "\uFEFFid:string,text:string\r\na,\"한글, \"\"quote\"\"\r\nsecond line\"\r\n");
            Assert.That(table.Get("a").Get<string>("text"), Is.EqualTo("한글, \"quote\"\r\nsecond line"));
        }

        [TestCase("id,text\na,\"unclosed")]
        [TestCase("id,text\na,ba\"d")]
        [TestCase("id,text\na,\"valid\"bad")]
        [TestCase("id:int,price:float\n1,NaN")]
        [TestCase("id:int,price:float\n1,Infinity")]
        [TestCase("id:int,price:int\n1,")]
        [TestCase("id:int,price:int\n1,2147483648")]
        [TestCase("id:int\n01\n1")]
        [TestCase("id:string\na\na")]
        [TestCase("id:string\n\" \"")]
        [TestCase("id:string\n padded")]
        [TestCase("id:string,id:string\na,b")]
        [TestCase("id:decimal\n1")]
        [TestCase("id:bool\ntrue")]
        [TestCase("name:string\na")]
        [TestCase("id:string,n:int\na")]
        [TestCase("id:string\na,extra")]
        [TestCase("bad-name:string\na")]
        public void InvalidCsvIsRejected(string csv) => Assert.Throws<FormatException>(() => CsvTableReader.Read("Invalid.csv", csv));

        [Test]
        public void IdlerThreeRowFormatSkipsCommentColumnsAndEmptyTrailingCells()
        {
            const string csv = "#변수 설명,번호,이름,#메모,,\n#데이터 타입,int,string,string,,\n#변수명,id,name,note,,\n,1,나무,ignored,,\n#ignored,2,돌,,,\n";
            var table = CsvTableReader.Read("ItemData_Default.csv", csv);
            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Columns.Count, Is.EqualTo(2));
            Assert.That(table.Get(1).Get<string>("name"), Is.EqualTo("나무"));
        }

        [Test]
        public void ExplicitKeySupportsRepeatedContentId()
        {
            var table = CsvTableReader.Read("Recipes.csv", "listId!:int,id:int\n1,5\n2,5");
            Assert.That(table.Get(2).Get<int>("id"), Is.EqualTo(5));
        }

        [Test]
        public void ValuesUseInvariantCultureAndReadonlyCollections()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var table = CsvTableReader.Read("Items.csv", "id:int,weight:float,tags:string[],nums:int[],active:bool\n1,1.25,a|b,1|2,1");
                var row = table.Get(1);
                Assert.That(row.Get<float>("weight"), Is.EqualTo(1.25f));
                Assert.That(row.Get<bool>("active"), Is.True);
                Assert.That(row.Get<IReadOnlyList<int>>("nums"), Is.EqualTo(new[] { 1, 2 }));
                Assert.Throws<NotSupportedException>(() => ((IList<string>)row.Get<IReadOnlyList<string>>("tags"))[0] = "changed");
                Assert.Throws<NotSupportedException>(() => ((IList<DataRow>)table.Rows).Clear());
                Assert.Throws<InvalidCastException>(() => row.Get<int>("weight"));
                Assert.Throws<KeyNotFoundException>(() => table.Get("missing"));
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }

        [Test]
        public void EmptyQuotedCellAndTrailingDelimiterArePreserved()
        {
            var table = CsvTableReader.Read("Items.csv", "id:string,text:string,tags:string[]\na,\"\",");
            Assert.That(table.Get("a").Get<string>("text"), Is.Empty);
            Assert.That(table.Get("a").Get<IReadOnlyList<string>>("tags"), Is.Empty);
        }

        [Test]
        public void DatabaseMustBeInitializedAndUsesDistinctIndexes()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => db.GetTable("One"));
                var one = CsvTableReader.Read("One.csv", "id,value\na,first");
                var two = CsvTableReader.Read("Two.csv", "id,value\na,second");
                db.ApplyValidated(new List<DataTable> { one, two }, "hash");
                Assert.That(db.GetTable("One").Get("a").Get<string>("value"), Is.EqualTo("first"));
                Assert.That(db.GetTable("Two").Get("a").Get<string>("value"), Is.EqualTo("second"));
                Assert.That(db.GetTable("One").Get("a"), Is.SameAs(db.GetTable("One").Get("a")));
            }
            finally { UnityEngine.Object.DestroyImmediate(db); }
        }

        [Test]
        public void GeneratedCodeContainsSchemaWithoutEmbeddingContent()
        {
            var table = CsvTableReader.Read("System.csv", "id:int,class:string,tags:int[]\n1,UNIQUE_CONTENT_VALUE,1|2");
            string code = DataCodeGenerator.Generate(new[] { table });
            Assert.That(code, Does.Contain("@class"));
            Assert.That(code, Does.Contain("global::System.Collections.Generic.IReadOnlyList<int>"));
            Assert.That(code, Does.Not.Contain("UNIQUE_CONTENT_VALUE"));
        }

        [Test]
        public void PreviewDetectsChangedSourceBeforeMutation()
        {
            string folder = "Assets/Settings/Data/Test_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder);
            try
            {
                string file = Path.Combine(folder, "Test.csv");
                File.WriteAllText(file, "id:int,value:string\n1,old");
                var preview = DataImporter.Preview(folder);
                File.WriteAllText(file, "id:int,value:string\n1,new");
                var error = Assert.Throws<InvalidOperationException>(() => DataImporter.Apply(preview));
                Assert.That(error.Message, Does.Contain("changed since preview"));
            }
            finally
            {
                // This test owns this exact, newly created folder only.
                UnityEditor.AssetDatabase.DeleteAsset(folder);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                if (File.Exists(folder + ".meta")) File.Delete(folder + ".meta");
            }
        }

        [Test]
        public void ReadSourcesSupportsCsvOpenForWritingInExcel()
        {
            string folder = "Assets/Settings/Data/Test_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "Shared.csv");
            const string csv = "id:string,name:string\na,한글";
            try
            {
                File.WriteAllText(file, csv, new System.Text.UTF8Encoding(true));
                using (var heldByEditor = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    Assert.That(DataImporter.ReadSources(folder)["Shared.csv"], Is.EqualTo(csv));
            }
            finally
            {
                UnityEditor.AssetDatabase.DeleteAsset(folder);
                if (File.Exists(file)) File.Delete(file);
                if (File.Exists(file + ".meta")) File.Delete(file + ".meta");
                if (Directory.Exists(folder)) Directory.Delete(folder);
                if (File.Exists(folder + ".meta")) File.Delete(folder + ".meta");
            }
        }

        [TestCase("https://docs.google.com/spreadsheets/d/abc_123/edit#gid=42", "https://docs.google.com/spreadsheets/d/abc_123/export?format=csv&gid=42")]
        [TestCase("https://docs.google.com/spreadsheets/d/abc_123/edit?gid=7#gid=7", "https://docs.google.com/spreadsheets/d/abc_123/export?format=csv&gid=7")]
        [TestCase("https://docs.google.com/spreadsheets/d/e/abc/pub?output=csv&gid=9", "https://docs.google.com/spreadsheets/d/e/abc/pub?output=csv&single=true&gid=9")]
        public void GoogleSheetLinksBecomeCsvUrls(string source, string expected) => Assert.That(SpreadsheetImporter.CsvUrl(source), Is.EqualTo(expected));

        [TestCase("http://docs.google.com/spreadsheets/d/abc")]
        [TestCase("https://docs.google.com.evil.test/spreadsheets/d/abc")]
        [TestCase("https://example.com/data.csv")]
        public void InvalidSheetHostsAreRejected(string source) => Assert.Throws<FormatException>(() => SpreadsheetImporter.CsvUrl(source));
    }
}
