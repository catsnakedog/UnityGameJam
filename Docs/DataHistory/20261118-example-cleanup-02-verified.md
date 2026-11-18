# 도구 예제 제거 및 스프레드시트/CSV 양식 검증

2026-11-18. 사용자 확정: 도구 예제만 삭제하고 임시 엔딩 3개는 유지한다.

## 변경 데이터와 정본

- `data.example-items`: 폐기. ExampleItems Google Sheets는 휴지통으로 이동했다(문서 ID `12naSgbJf2enFqYVX6VWJKxfTLTbE5K97FIAKn6UZ67E`). 로컬 `Assets/Settings/Data/Csv/ExampleItems.csv`/meta, 생성 DB의 2행과 테이블, 생성 접근자, 가져오기 설정을 제거했다. 업로드용 outputs/20261118-notion-data/ExampleItems.xlsx 및 검사 부속 파일도 삭제했다. [폐기 명세](../DataSpecs/ExampleItems.md).
- `editor.data-tool`: `ProjectSettings/GameDataTool.asset`의 공용 연결 5개 → 4개. [명세](../DataSpecs/DataToolSettings.md).
- `system.game-database`: 기존 `Assets/Settings/Data/GameDatabase.asset`, `data/catalog` 및 GUID 보존. 기존 DataImporter에서 삭제할 변경이 ExampleItems 2행 테이블 1개뿐임을 확인하고 Apply(allowRemovals:true)로 재생성. [명세](../DataSpecs/GameDatabase.md).
- GameDevChecks의 예제 테이블 존재 강제를 현재 CSV의 모든 테이블 이름/행 수와 DB의 일치 검사로 교체. 보고서 생성기의 예제 전용 문구 제거, DataManager 문서의 조회 예시는 기존 Characters로 교체.

## 유지 데이터와 양식 대조

편집 정본은 아래 명세에 연결된 Google Sheets다. 내부 CSV는 그 스냅샷이며 기존 데이터 ID와 경로를 유지한다.

| 데이터 ID | 명세/편집 시트 링크 | 내부 CSV | 행 | 열 |
|---|---|---|---:|---:|
| data.characters | [Characters](../DataSpecs/Characters.md) | Assets/Settings/Data/Csv/Characters.csv | 6 | 10 |
| data.events | [Events](../DataSpecs/Events.md) | Assets/Settings/Data/Csv/Events.csv | 12 | 7 |
| data.shelter.items | [ShelterItems](../DataSpecs/ShelterItems.md) | Assets/Settings/Data/Csv/ShelterItems.csv | 3 | 5 |
| data.game-dev.endings | [GameDevEndings](../DataSpecs/GameDevEndings.md) | Assets/Settings/Data/Csv/GameDevEndings.csv | 3 | 5 |
| data.farming.pickups | [FarmingData](../DataSpecs/FarmingData.md) | Assets/Settings/Farming/FarmingData.csv | 12 | 3 |

5개 실제 시트의 최신 CSV 내보내기를 내려받아 총 36행을 내부 CSV와 비교했다. **1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터**가 전부 일치하며 열 순서·행 순서·모든 셀도 동일하다. 내부 CSV 5개는 UTF-8 BOM 포함. BOM과 줄바꿈 차이를 제외한 전체 텍스트도 동일하다. 데이터 수치/행/스키마를 추가 변경할 필요가 없었다.

공용 4개는 Unity CsvTableReader로 설명·타입·필드명·셀 값을 재대조했다. 공용 DB 및 설정이 각 4개임을 확인했고, CharacterSetup.Validate로 파밍 ID 관계도 확인했다. FarmingData는 전용 경로/임포터를 유지했으며 공용 DB에 복제하지 않았다.

## 검증 결과와 범위

- GameDevChecks.RunEdit: 29개 통과. 현재 CSV와 DB 해시 일치, 모든 테이블/행 수 일치, 엔딩 3종 경계와 UI 참조 검사 포함.
- ExampleItems CSV/meta, 생성 DB 테이블/행, 생성 접근자, 설정 연결 부재 검사 통과. Assets/ProjectSettings의 cs/csv/asset/prefab/unity 검색에서 ExampleItems/example_wood/example_stone 참조 0건.
- Addressables 콘텐츠 빌드 성공. Editor 연결 명령 실행 성공으로 이번 수정의 컴파일 완료 확인. 이번 작업에서 Play/Player 빌드는 수행하지 않았다.
- Drive 폴더 새로고침 후 관리 시트 5개와 SpreadConfig파일(삭제X) 폴더만 존재함을 확인. 기존 테스트 파일의 휴지통 상태 및 문서는 보존. 영구 삭제 없음.
- 최신 DataRegistry의 다른 작업 planned를 재확인했으며 새 ID/DB/CSV/로더 또는 동일 의미 정본 복제를 추가하지 않았다. 임시 엔딩 3개, 실제 사건 12개와 나머지 게임 데이터는 보존했다.
- 이전 DataHistory는 수정/삭제하지 않았다. 테스트의 메모리 내 fixture와 이전 검증 기록은 실제 배포 데이터가 아니므로 보존한다.
