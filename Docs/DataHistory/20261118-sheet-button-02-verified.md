# Google Sheets 단일 편집·전체 가져오기 버튼 검증

2026-11-18 / UnityGameJam / Codex sheet-button-20261118. [계획](20261118-sheet-button-01-plan.md).

## 구현

- `Tools > Data Manager` 상단에 **전체 시트 가져오기 · CSV / 클래스 갱신** 버튼을 배치했다. Google Sheets가 유일한 편집 원본이며 Drive에 CSV를 만들거나 업로드하지 않는다.
- 기존 `SpreadsheetImporter.DownloadCsvAsync`가 CSV 내보내기 다운로드·취소·HTTP/HTML 오류를 공용 처리한다. 공용 시트 가져오기와 파밍 가져오기 모두 이를 사용한다.
- `Assets/Editor/ProjectSpreadsheetSync.cs`는 기존 프로젝트 검증기/임포터를 연결한다. 공용 Editor asmdef에는 비동기 콜백으로 연결해 어셈블리 경계를 유지했다. 런타임 로더/새 DB/새 CSV 정본 없음.
- 모든 다운로드/캐릭터↔파밍 참조/아이템↔이벤트 참조/이벤트 효과/엔딩 규칙/파밍 제약/삭제 정책을 검증한 뒤 적용한다. 중복 실행과 Play 중 적용을 거부하며 다운로드 취소를 지원한다. 다운로드 중 로컬 변경도 감지한다.
- 내부 CSV는 기존 경로에 UTF-8 BOM으로 저장한다. 공용 데이터는 기존 `Assets/Scripts/Data/Generated/DataTables.cs` 타입 접근자를 생성하고 기존 GameDatabase를 갱신한다. 파밍은 기존 FarmingRecord로 파싱해 기존 FarmingDatabase만 갱신한다.
- 정상 전체 가져오기는 누락 행/열/테이블을 자동 삭제하지 않는다. 고급 미리보기의 명시적 삭제 검토를 유지한다. 파밍 적용 후 공용 적용 실패 시 파밍 CSV/DB를 복구하고, 공용 롤백은 기존 SpreadsheetImporter/DataImporter가 담당한다.
- 시트 연결/로컬 고급 미리보기는 접이식 영역에 두었다. Addressables 콘텐츠 빌드 버튼은 별도로 유지하며 완료 메시지에서 안내한다.

## 변경·사용한 데이터 ID / 정본

| ID | 정본/산출물 | 명세 |
|---|---|---|
| editor.data-tool | ProjectSettings/GameDataTool.asset, 기존 공용 sheets 4개 + farmingSheetUrl 1개 | [설정](../DataSpecs/DataToolSettings.md) |
| data.characters | 기존 Characters Google Sheets → Assets/Settings/Data/Csv/Characters.csv | [캐릭터](../DataSpecs/Characters.md) |
| data.events | 기존 Events Google Sheets → Assets/Settings/Data/Csv/Events.csv | [이벤트](../DataSpecs/Events.md) |
| data.shelter.items | 기존 ShelterItems Google Sheets → Assets/Settings/Data/Csv/ShelterItems.csv | [물자](../DataSpecs/ShelterItems.md) |
| data.game-dev.endings | 기존 GameDevEndings Google Sheets → Assets/Settings/Data/Csv/GameDevEndings.csv | [엔딩](../DataSpecs/GameDevEndings.md) |
| data.farming.pickups | 기존 FarmingData Google Sheets → Assets/Settings/Farming/FarmingData.csv → 기존 전용 DB | [파밍](../DataSpecs/FarmingData.md) |
| system.game-database | Assets/Settings/Data/GameDatabase.asset 및 DataTables.cs는 계속 생성물 | [공용 DB](../DataSpecs/GameDatabase.md) |

기존 5개 시트 URL/탭은 각 명세에 유지한다. 신규 설정 farmingSheetUrl은 해당 FarmingData URL을 저장하는 단일 필드다. 공용 DB GUID `62a272592b807b3469b072879c349759`, data/catalog 주소 및 파밍 DB GUID/주소/ID를 유지했다. 게임 데이터 수치와 임시 엔딩 3개 변경 없음. 오디오/좀비/파밍 맵 작업의 정본을 추가하거나 이전하지 않았다. 최신 registry planned를 재확인했고 파밍 CSV의 공용 폴더 복제/이중 임포터 등록은 없다.

## 실제 검증

- Unity 컴파일 완료, 실제 5개 시트 다운로드 → 내부 CSV 저장 → 클래스 파싱/생성 → 공용/파밍 DB 갱신 성공. 4개 공용 테이블과 파밍 12행 확인.
- DataManagerWindow의 실제 ImportAllSheets 버튼 핸들러 호출 후 busy 해제/error=false 및 `완료: 5개 시트` 메시지 확인. 창을 열어 둔 상태로 전달했다.
- 정상 데이터 의미 검증, 잘못된 파밍 무게 거부, 파밍 ID 누락 거부, 캐릭터 외래키 오류 거부, 물자 획득 이벤트 참조 오류 거부, 시작 전 취소, 검증/취소 시 CSV 무변경, 중복 실행 거부를 검사했다.
- 공용 시트 다운로드 이후 파밍 다운로드가 HTTP 오류로 실패하는 경우를 실제 주입했다. 임시 URL은 메모리에서만 설정하고 finally로 복원했다. CSV 5개·DB 2개·생성 C#이 바이트 단위로 그대로임을 확인했다.
- 기존 GameJam.Data.Tests: NUnit 36/36 통과. GameDevChecks.RunEdit: 29개 통과.
- Addressables 콘텐츠 빌드 성공. 이번 기능을 위한 Player 빌드/추가 Play 테스트는 하지 않았다. 검증 시작 시 진행 중이던 Play에서 가져오기 거부를 확인한 뒤 Play를 종료하고 Editor 작업을 수행했다.
- 디스크 쓰기 실패를 강제하는 커밋 중 롤백은 이번에 실제 주입하지 않았다. 해당 복구 코드는 기존 공용 롤백 및 파밍 이전 CSV/JSON 복원을 사용한다.

사용 절차: [Data Manager](../DataManager.md). 별도 Drive CSV 또는 수동 파밍 다운로드가 필요하지 않으며 자동 실시간 동기화가 아닌 사용자 버튼 실행 방식이다.
