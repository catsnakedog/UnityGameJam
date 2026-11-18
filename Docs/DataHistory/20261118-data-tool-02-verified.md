# Data Manager · verified

- Record: 20261118-data-tool-02-verified.
- Project: UnityGameJam, Unity 6000.3.28f1, Addressables 4.2.0.
- Date/time zone: 2026-11-18 Asia/Seoul.
- Reason: CSV-first data management tool requested by user, followed by an explicit recheck request.
- Related baseline: [20261118-data-tool-01-planned](20261118-data-tool-01-planned.md).
- Initial application and complete snapshot: [20261118-141619-3123482-data-applied](20261118-141619-3123482-data-applied.md).
- Successful content build and complete snapshot: [20261118-142148-7598907-data-built](20261118-142148-7598907-data-built.md).

## Managed connection snapshot

The only runtime SO managed by this new tool is `GameJam.Data.GameDatabase`, at `Assets/Settings/Data/GameDatabase.asset`, GUID `62a272592b807b3469b072879c349759`, address `data/catalog`, group `Game Data Local`, no labels. Addressables profile: `Default`; content target: `StandaloneWindows64`. The full serialized values are in the linked successful build record. Source SHA256: `862f6d30e10e368ecf9a6102a50cfa195b4c720e6139568ce2f86e999ef94ced`.

Source: `Assets/Settings/Data/Csv/ExampleItems.csv`, CSV schema v1. Two demonstration records: `example_wood` (price 10, weight 1.5, enabled true, tags material/wood) and `example_stone` (price 20, weight 2.5, enabled true, tags material/stone). Display names: 예제 목재, 예제 돌. These are fixture values, not migrated game balance. Existing Farming DB/rules/source/consumer code remains outside this tool's managed set.

Consumers: `GameData.InitializeAsync`, `GetTable`, `Get`, `TryGet`, and generated `GameJam.Data.Generated.ExampleItems.Table` / `Row`. The service owns Addressables handles and application-lifetime indexes. Startup consumers must await initialization; scenes retain the cache; explicit Release or application shutdown invalidates the service. No per-scene Inspector data assignment is required.

Editor-only configuration: `ProjectSettings/GameDataTool.asset`, default CSV folder `Assets/Settings/Data/Csv`, spreadsheet disabled, no URLs configured. This configuration is not a runtime data SO and is not distributed through Addressables.

## Completed verification

- Unity EditMode tests: **32 passed, 0 failed, 0 skipped**, reported duration 0.56 seconds. Includes malformed/quoted/multiline UTF-8 CSV, ID uniqueness and canonicalization, finite numeric conversion, overflow, schema errors, Idler headers, explicit keys, immutable arrays/rows, separate table indexes, generated schema safety, stale-preview rejection, and Google URL conversion/host validation.
- Read actual reference `Idler/Assets/JJData/Csv/ItemData2_test2.csv`: **122 rows**, first key `1`. Reference project was read only.
- Reapplied the unchanged example through the real Editor importer: zero changes, same serialized database values, same GUID.
- Real Play Mode service check: pre-initialization access rejected; concurrent calls shared a Task; repeated reads returned the same cached row; generated typed getters matched CSV; release during loading cancelled that generation; a subsequent initialization succeeded with the expected value.
- Repeated lookup observation: 100,000 table/ID/field queries on the two-row example took 19.9923 ms in this Editor session, checksum 1,000,000. This is a smoke measurement, not a platform-wide performance guarantee.
- Addressables content build succeeded. Then loaded the built `Library/com.unity.addressables/aa/Windows/catalog.bin`, resolved the explicit `data/catalog` location, and loaded through **BundledAssetProvider** (two dependencies). Loaded database had one table, expected source hash, and example price 10. This verification used the built bundle, not an AssetDatabase location.
- Inspected the Data Manager window capture: Korean labels, CSV path, validation message, table selector, search, pagination, types, and both example rows displayed correctly. Capture: `.utmp/data-manager.png` (temporary local QA output).
- Final observed Unity script state: not compiling, scriptCompilationFailed=false. All tool code/asset files have Unity-generated `.meta` files.

## Interruption and remaining limits

- First content build failed while concurrently edited Farming/Shelter cameras referenced a not-yet-imported `FixedAspectCamera`. The separate camera file became available; a later content build succeeded. This task did not change camera code. The failed build report is preserved alongside successful reports.
- An extra final asynchronous test rerun overlapped a separate Play transition and remained pending; that rerun was cancelled. The completed 32-test run and the independent actual Play/compiled-bundle checks above are the recorded pass evidence. The active Play session was left running.
- No user spreadsheet URL was supplied. Google Sheets URL validation is tested; authenticated/public-sheet live downloading has not been exercised against a user sheet. Current download workflow supports public/link-readable or CSV-published Google Sheets; private-sheet OAuth is not implemented.
- No standalone Player executable was requested or built. Content bundles and the running Editor were verified.
- Existing unrelated changes in the shared workspace were preserved. A whole-workspace whitespace check reported trailing whitespace in `Assets/Scenes/SampleScene.unity`, outside this tool's files.

Usage and formats: [DataManager.md](../DataManager.md).
