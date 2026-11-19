using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameJam.Data
{
    [Serializable]
    public sealed class DataColumn
    {
        [SerializeField] string name;
        [SerializeField] string type;
        [SerializeField] string description;
        public string Name => name;
        public string Type => type;
        public string Description => description;
        public DataColumn(string name, string type, string description = "")
        { this.name = name; this.type = type; this.description = description; }
    }

    [Serializable]
    public sealed class DataRow
    {
        [SerializeField] string[] cells;
        [NonSerialized] object[] values;
        [NonSerialized] DataTable table;
        public string Id => DataValue.Key(values[table.KeyIndex]);
        public IReadOnlyList<string> Cells => Array.AsReadOnly(cells);
        public DataRow(string[] cells) { this.cells = (string[])cells.Clone(); }
        internal void Bind(DataTable owner)
        {
            table = owner;
            if (cells.Length != owner.Columns.Count) throw new FormatException(owner.Name + ": invalid row width.");
            values = new object[cells.Length];
            for (int i = 0; i < cells.Length; i++) values[i] = DataValue.Parse(owner.Columns[i].Type, cells[i]);
        }
        public T Get<T>(string column) => Get<T>(table.ColumnIndex(column));
        public T Get<T>(int column)
        {
            if (values == null) throw new InvalidOperationException("The data row is not initialized.");
            if (values[column] is T value) return value;
            throw new InvalidCastException($"{table.Name}.{table.Columns[column].Name} is {table.Columns[column].Type}, requested {typeof(T).Name}.");
        }
    }

    [Serializable]
    public sealed class DataTable
    {
        [SerializeField] string name;
        [SerializeField] string source;
        [SerializeField] int keyIndex;
        [SerializeField] List<DataColumn> columns;
        [SerializeField] List<DataRow> rows;
        [NonSerialized] Dictionary<string, DataRow> byId;
        [NonSerialized] Dictionary<string, int> byColumn;
        [NonSerialized] ReadOnlyCollection<DataRow> rowView;
        [NonSerialized] ReadOnlyCollection<DataColumn> columnView;
        public string Name => name;
        public string Source => source;
        public int KeyIndex => keyIndex;
        public IReadOnlyList<DataColumn> Columns => columnView ?? (columnView = columns.AsReadOnly());
        public IReadOnlyList<DataRow> Rows => rowView ?? (rowView = rows.AsReadOnly());
        public int Count => rows.Count;
        public DataTable(string name, string source, int keyIndex, List<DataColumn> columns, List<DataRow> rows)
        { this.name = name; this.source = source; this.keyIndex = keyIndex; this.columns = new List<DataColumn>(columns); this.rows = new List<DataRow>(rows); }

        internal void Initialize()
        {
            if (columns == null || rows == null || keyIndex < 0 || keyIndex >= columns.Count)
                throw new FormatException(name + ": invalid schema.");
            string keyType = columns[keyIndex].Type;
            if (keyType != "string" && keyType != "int" && keyType != "long") throw new FormatException(name + ": keys must be string, int or long.");
            byColumn = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < columns.Count; i++) byColumn.Add(columns[i].Name, i);
            byId = new Dictionary<string, DataRow>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                row.Bind(this);
                if (string.IsNullOrWhiteSpace(row.Id) || row.Id != row.Id.Trim()) throw new FormatException(name + ": empty or padded key.");
                if (byId.ContainsKey(row.Id)) throw new FormatException(name + ": duplicate key " + row.Id);
                byId.Add(row.Id, row);
            }
        }
        public int ColumnIndex(string column) => byColumn.TryGetValue(column, out var index) ? index : throw new KeyNotFoundException(name + ": unknown column " + column);
        public DataRow Get(string id) => TryGet(id, out var row) ? row : throw new KeyNotFoundException(name + ": unknown ID " + id);
        public DataRow Get(int id) => Get(DataValue.Key(id));
        public DataRow Get(long id) => Get(DataValue.Key(id));
        public bool TryGet(string id, out DataRow row) { row = null; return id != null && byId.TryGetValue(id, out row); }
    }

    /// <summary>Compiled import result. CSV files are the authoring source.</summary>
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] int schemaVersion = 1;
        [SerializeField] string sourceHash;
        [SerializeField] List<DataTable> tables = new List<DataTable>();
        [NonSerialized] Dictionary<string, DataTable> byName;
        public int SchemaVersion => schemaVersion;
        public string SourceHash => sourceHash;
        public IReadOnlyList<DataTable> Tables => tables.AsReadOnly();
        public void Initialize()
        {
            if (schemaVersion != 1) throw new FormatException("Unsupported database schema version: " + schemaVersion);
            var index = new Dictionary<string, DataTable>(StringComparer.Ordinal);
            foreach (var table in tables) { table.Initialize(); index.Add(table.Name, table); }
            byName = index;
        }
        public DataTable GetTable(string name)
        {
            if (byName == null) throw new InvalidOperationException("Call GameData.InitializeAsync before querying data.");
            return byName.TryGetValue(name, out var table) ? table : throw new KeyNotFoundException("Unknown data table: " + name);
        }
        public static GameDatabase CreateSession(List<DataTable> value)
        {
            var result = CreateInstance<GameDatabase>();
            result.tables = value;
            try { result.Initialize(); return result; }
            catch { Destroy(result); throw; }
        }
#if UNITY_EDITOR
        public void ApplyValidated(List<DataTable> value, string hash)
        {
            foreach (var table in value) table.Initialize();
            tables = new List<DataTable>(value); sourceHash = hash; schemaVersion = 1; Initialize();
        }
#endif
    }
}
