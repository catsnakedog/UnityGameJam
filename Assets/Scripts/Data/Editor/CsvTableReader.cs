using System.Collections.Generic;

namespace GameJam.Data.Editor
{
    public static class CsvTableReader
    {
        public static bool IsIdentifier(string text) => CsvTableParser.IsIdentifier(text);
        public static DataTable Read(string source, string csv) => CsvTableParser.Read(source, csv);
        public static List<List<string>> Parse(string text) => CsvTableParser.Parse(text);
    }
}
