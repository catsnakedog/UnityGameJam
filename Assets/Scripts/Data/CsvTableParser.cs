using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GameJam.Data
{
    public static class CsvTableParser
    {
        // Deliberately restrict schema identifiers so generated C# cannot contain executable input.
        public static bool IsIdentifier(string text) => Regex.IsMatch(text ?? "", @"^[A-Za-z_][A-Za-z0-9_]*$");

        public static DataTable Read(string source, string csv)
        {
            string name = Path.GetFileNameWithoutExtension(source);
            if (!IsIdentifier(name)) throw new FormatException(source + ": file name must be a C# identifier.");
            var records = Parse(csv);
            if (records.Count == 0) throw new FormatException(source + ": empty CSV.");
            var columns = new List<DataColumn>();
            var indexes = new List<int>();
            int firstDataRow;
            int key = -1;
            bool legacy = records[0][0].StartsWith("#", StringComparison.Ordinal) || IsThreeRowSheet(records);
            if (legacy && records.Count < 3) throw new FormatException(source + ": Idler CSV needs description/type/name rows.");
            var headers = records[legacy ? 2 : 0];
            firstDataRow = legacy ? 3 : 1;
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i].Trim();
                string description = legacy ? Cell(records[0], i) : "";
                string type;
                if (legacy)
                {
                    type = Cell(records[1], i).Trim();
                    if (header.Length == 0 || type.Length == 0 || header.StartsWith("#") || type.StartsWith("#") || description.StartsWith("#")) continue;
                }
                else
                {
                    if (header.Length == 0 || header.StartsWith("#")) continue;
                    var parts = header.Split(':');
                    if (parts.Length > 2) throw new FormatException(source + ": invalid header " + header);
                    header = parts[0].Trim();
                    type = parts.Length == 2 ? parts[1].Trim() : "string";
                }
                bool explicitKey = header.EndsWith("!", StringComparison.Ordinal);
                if (explicitKey) header = header.Substring(0, header.Length - 1);
                if (!IsIdentifier(header)) throw new FormatException(source + ": invalid column name " + header);
                DataValue.CSharpType(type);
                if (columns.Any(c => c.Name == header)) throw new FormatException(source + ": duplicate column " + header);
                if (explicitKey)
                {
                    if (key >= 0) throw new FormatException(source + ": only one explicit key column is allowed.");
                    key = columns.Count;
                }
                indexes.Add(i);
                columns.Add(new DataColumn(header, type, description));
            }
            if (key < 0) key = columns.FindIndex(c => string.Equals(c.Name, "id", StringComparison.OrdinalIgnoreCase));
            if (key < 0) throw new FormatException(source + ": add an id column or mark a unique key with ! (listId!:int).");
            var rows = new List<DataRow>();
            for (int r = firstDataRow; r < records.Count; r++)
            {
                var record = records[r];
                if (record.All(value => value.Length == 0) || record[0].StartsWith("#", StringComparison.Ordinal)) continue;
                if (record.Count < indexes.Max() + 1 || record.Skip(headers.Count).Any(v => v.Length != 0))
                    throw new FormatException($"{source}: record {r + 1} has the wrong number of columns.");
                var cells = indexes.Select(i => Cell(record, i)).ToArray();
                for (int c = 0; c < columns.Count; c++)
                {
                    try { DataValue.Parse(columns[c].Type, cells[c]); }
                    catch (Exception error) when (error is FormatException || error is OverflowException)
                    { throw new FormatException($"{source}: record {r + 1}, column '{columns[c].Name}' ({columns[c].Type}): {error.Message}"); }
                }
                rows.Add(new DataRow(cells));
            }
            var table = new DataTable(name, source.Replace('\\', '/'), key, columns, rows);
            // The runtime validator checks canonical IDs (e.g. int 01 and 1 collide).
            table.Initialize();
            return table;
        }

        static string Cell(List<string> record, int index) => index < record.Count ? record[index] : "";

        // Spreadsheet template: description / type / field name, without an Idler marker column.
        // Typed single-row headers take precedence and must never reinterpret ordinary data rows.
        static bool IsThreeRowSheet(List<List<string>> records)
        {
            if (records.Count < 3 || records[0].Any(c => c.Contains(":"))) return false;
            var types = records[1];
            var names = records[2];
            if (types.Count != names.Count || records[0].Count != names.Count) return false;
            bool hasKey = false;
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i].Trim();
                if (name.EndsWith("!", StringComparison.Ordinal)) { hasKey = true; name = name.Substring(0, name.Length - 1); }
                if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase)) hasKey = true;
                if (!IsIdentifier(name)) return false;
                try { DataValue.CSharpType(types[i].Trim()); }
                catch (FormatException) { return false; }
            }
            // A normal untyped header already declaring a key keeps its original interpretation.
            if (records[0].Any(c => string.Equals(c.Trim(), "id", StringComparison.OrdinalIgnoreCase) || c.Trim().EndsWith("!", StringComparison.Ordinal))) return false;
            return hasKey;
        }

        /// <summary>RFC-style CSV, with UTF-8 BOM, CR/LF and escaped/multiline quoted fields.</summary>
        public static List<List<string>> Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var records = new List<List<string>>();
            var record = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false, closed = false, started = false;
            int line = 1;
            for (int i = text.Length > 0 && text[0] == '\uFEFF' ? 1 : 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else { quoted = false; closed = true; }
                    }
                    else { cell.Append(ch); if (ch == '\n') line++; }
                    continue;
                }
                if (ch == ',' || ch == '\n' || ch == '\r')
                {
                    record.Add(cell.ToString()); cell.Clear(); closed = false; started = false;
                    if (ch != ',')
                    {
                        records.Add(record); record = new List<string>(); line++;
                        if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    }
                    continue;
                }
                if (closed) throw new FormatException($"CSV line {line}: unexpected text after closing quote.");
                if (ch == '"')
                {
                    if (started) throw new FormatException($"CSV line {line}: quote inside an unquoted field.");
                    quoted = true;
                }
                else cell.Append(ch);
                started = true;
            }
            if (quoted) throw new FormatException($"CSV line {line}: unterminated quoted field.");
            if (started || closed || record.Count > 0) { record.Add(cell.ToString()); records.Add(record); }
            return records;
        }
    }
}
