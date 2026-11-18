# SpreadConfig 연결 구현과 검증

- 작업 ID: Codex spread-config-20261118 / 2026-11-18 KST.
- 변경 ID: `editor.data-tool`. 연결 목록 정본은 [SpreadConfig 탭](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615). 로컬 설정 정본은 `ProjectSettings/GameDataTool.asset`의 `spreadConfigUrl`, `csvFolder`뿐이다. 구 sheets/farmingSheetUrl/spreadsheetEnabled 직렬화 필드를 Unity API로 폐기했다.
- 현재 계약: [DataToolSettings](../DataSpecs/DataToolSettings.md), [사용법](../DataManager.md). Config는 1행 헤더, sheetName 파일명과 id/gid URL 및 enabled를 매 버튼 실행 시 읽는다. Config 원격 내용은 변경하지 않았다.
- 재사용 데이터 ID와 정본: `data.characters`, `data.events`, `data.shelter.items`, `data.game-dev.endings`, `data.farming.pickups`의 기존 Google Sheets 5개. 각각 [Characters](../DataSpecs/Characters.md), [Events](../DataSpecs/Events.md), [ShelterItems](../DataSpecs/ShelterItems.md), [GameDevEndings](../DataSpecs/GameDevEndings.md), [FarmingData](../DataSpecs/FarmingData.md)에 원본 URL/필드 계약이 있다. 콘텐츠 수치와 스키마를 변경하지 않았다. 임시 엔딩 3개 유지.
- 생성물: 공용 4 CSV는 `Assets/Settings/Data/Csv`, 파밍은 `Assets/Settings/Farming/FarmingData.csv`. 기존 GameDatabase/FarmingDatabase GUID와 Addressables 주소 및 DataTables.cs/FarmingRecord를 재사용한다. 새 게임 DB/런타임 로더/편집 정본 없음. Config는 메모리 연결 목록이며 게임 CSV나 클래스로 생성하지 않는다.
- 중복 검사: 최신 registry와 planned, Assets/ProjectSettings/Docs의 기존 연결 필드·소비자를 재확인했다. 개별 URL 설정 중복을 제거했고 파밍을 공용 폴더/DB에 등록하지 않았다. ExampleItems/SpreadConfig/FarmingData가 공용 테이블에 없는 것을 실제 DB 검증으로 확인했다.

## 실제 검증

1. Unity 컴파일 성공. GameJam.Data.Tests 57개 전부 통과(기존 36+Config 21). Config의 필수/중복 헤더, 빈 값, 파일명/경로, URL 구성, enabled/비활성, 메타데이터, 중복 연결 검증 포함.
2. 실제 Config의 활성 5개(GameDevEndings, ShelterItems, FarmingData, Events, Characters)를 읽어 ProjectSpreadsheetSync.ImportAsync 실행 성공. 다시 내려받은 각 CSV 본문과 로컬 CSV가 정확히 일치하며 5개 모두 UTF-8 BOM을 확인했다. 공용 DB SourceHash/4테이블과 파밍 DB 12행 및 의미 검증 통과.
3. Data Manager의 FetchConfig 버튼 처리로 연결 5개 표시, ImportAllSheets 실제 버튼 처리로 완료 상태를 확인했다. 마지막 UI 메시지: `완료: SpreadConfig의 활성 5개 시트 → CSV 저장 / 클래스 파싱 / DB 갱신.`
4. 잘못된 Config URL, 사전 취소, 진행 중 취소, 중복 실행 차단 후 5 CSV/2 DB/생성 코드의 바이트 불변을 확인했다. 원격 설정은 수정하지 않고 로컬 설정의 임시 테스트 값은 finally에서 복구했다.
5. [기존 Google 양식 문서](https://docs.google.com/document/d/1wl9JY-Iiv316ucy_pDvmogp1xr20tYZP/edit)의 연결 설명을 SpreadConfig 방식으로 갱신했다. 내려받은 DOCX의 전체 텍스트, 5개 스키마/30필드, 5개 시트 링크, 반복 표 헤더를 대조했다. Google PDF 10페이지 중 변경 구간 9~10페이지를 포함해 레이아웃을 확인했다.

검증 중 Editor Play/플레이어 빌드로 일부 CLI 호출이 실패하거나 시간 초과했으나 빌드 종료 후 실제 검증을 다시 실행해 위 항목을 통과했다. 이번 작업에서 플레이어 또는 Addressables 빌드를 실행하지 않았다. 스키마 변경/적용 중 강제 디스크 오류에 대한 복구 경로를 새로 주입 검증하지는 않았으며 기존 임포터의 복구 정책을 유지한다. 배포용 데이터 갱신은 별도 Addressables 콘텐츠 빌드 버튼을 사용한다.
