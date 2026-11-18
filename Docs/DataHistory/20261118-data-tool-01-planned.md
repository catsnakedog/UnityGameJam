# Data Manager · planned

- Record: 20261118-data-tool-01-planned / 2026-11-18 Asia/Seoul.
- Request: CSV folder as default source, optional spreadsheet import, reference Idler.
- Baseline: no shared CSV tool. Existing FarmingData.csv/FarmingDatabase/FarmingRules and their loader belong to ongoing farming work and remain separate. No game balance changes.
- Reference: Idler/Assets/JJData uses description/type/name rows, generated C# and cached dictionaries. Reuse the authoring convention, without copying its delete-before-validation pipeline or mutable global arrays.
- Layout: Assets/Scripts/Data (runtime), Assets/Scripts/Data/Editor (tools), Assets/Scripts/Data/Generated (typed accessors), Assets/Settings/Data/Csv (source), Assets/Settings/Data/GameDatabase.asset (compiled SO), Docs/DataHistory (append-only records).
- Planned schema v1: typed header name:type, or Idler description/type/name rows. Unique table filename and key column (id, or explicit key annotation). Invariant scalar/array types. Empty numeric values and nonfinite numbers are errors.
- Planned asset: GameJam.Data.GameDatabase, stable address data/catalog, group Game Data Local; GUID unknown until Editor creates it. CSV is authoritative, asset is an import result.
- Runtime: GameData.InitializeAsync shares one asynchronous Addressables load. Immutable row/table views and dictionary indexes; GameData.GetTable/Get/TryGet available after initialization. Application lifetime, explicit Release and domain-reload reset.
- Editor: validate all inputs, preview per-cell changes, reject implicit deletion, update one database with rollback. Optional Google Sheets CSV download into local source; no remote write. Generate typed schema accessors, no embedded content values.
- Verification planned: malformed/quoted CSV, duplicate IDs, type/range failures, Idler compatibility, stale preview, deletion protection, import rollback, cache lifecycle and real Addressables content loading. No Player build requested.
