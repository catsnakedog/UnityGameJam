# GameDatabase 컨테이너 명세

2026-11-18 노션 콘텐츠 확장: 예제 제거 후 공용 4개 테이블을 유지하며 Characters/ShelterItems의 v2 필드를 기존 CSV에서 생성했다. 공용 CSV 파서는 제공 스프레드시트의 설명/타입/변수명 3행을 추가 지원한다. DataTables.cs와 DB는 계속 생성물이며 편집 정본은 연결된 Google Sheets이며 CSV는 단방향 가져오기 스냅샷이다. [대조 및 연결 상태](../NotionDevelopmentCheck.md).

벙커 설정 확장 (2026-11-18): 공용 `GameData.LoadSettingsAsync<T>(address)`가 SO를 주소별 공유 로드/캐시하며 Release/Play 초기화/종료 시 해제한다. 진행 중 해제는 완료 후 결과 폐기/핸들 해제. 실패 캐시는 제거하여 재요청 가능. CSV DB 스키마/정본 변경 없음. [BunkerRules](BunkerRules.md), [변경 이력](../DataHistory/20261118-bunker-ticks-02-verified.md). 캐시 공유/실제 콘텐츠 로드 검증 완료; 의도적인 실패 복구 미검증.

- 데이터 ID: `system.game-database`; 상태: **generated**; 담당: 공용 데이터 도구; 스키마 v1; 확인일: 2026-11-18.
- 소유 의미: 공용 CSV 테이블을 로드 가능한 하나의 결과물로 묶는 형식. 개별 콘텐츠 속성의 편집 정본이 아니다.
- 생성 파일: [GameDatabase.asset](../../Assets/Settings/Data/GameDatabase.asset), 타입 `GameJam.Data.GameDatabase`.
- GUID `62a272592b807b3469b072879c349759`, Addressables `data/catalog`, 그룹 `Game Data Local`, 라벨 없음.
- 원본: [DataToolSettings](DataToolSettings.md)의 csvFolder에서 발견한 등록 CSV들. 현재 Characters, Events, GameDevEndings, ShelterItems 네 테이블. 원본별 계약은 각 테이블의 명세가 소유한다.
- 파생 코드: `Assets/Scripts/Data/Generated/DataTables.cs`. 스키마 접근자이며 콘텐츠 값은 포함하지 않는다. 직접 수정하지 않는다.

## 직렬화 계약

| 경로 | 타입 | 의미 / 제약 |
|---|---|---|
| schemaVersion | int | 컨테이너 버전, 현재 1만 지원 |
| sourceHash | string | 입력 파일명/내용의 SHA256; 임포터가 생성 |
| tables | List<DataTable> | CSV에서 생성한 테이블 목록; 이름 고유 |
| tables[].name | string | CSV 파일의 확장자 없는 이름; 생성 코드 식별자 |
| tables[].source | string | CSV 폴더 기준 상대 원본 경로 |
| tables[].keyIndex | int | 키 열의 0 기반 위치; columns 범위 안이어야 함 |
| tables[].columns | List<DataColumn> | CSV 순서의 유효 열 |
| columns[].name | string | 테이블 안에서 고유한 열 이름 |
| columns[].type | string | 지원 타입명; [도구 형식](../DataManager.md) 참조 |
| columns[].description | string | Idler 설명 행; 간단한 헤더 형식에서는 빈 문자열 |
| tables[].rows | List<DataRow> | CSV 데이터 행 순서 |
| rows[].cells | string[] | 유효 열 수와 동일한 셀 수; 값은 초기화 시 타입 변환 |

인덱스·타입 변환 결과·읽기 전용 뷰는 비직렬화 런타임 캐시다. 필드값은 생성 과정에서 모두 채우며 콘텐츠 누락을 임의 값으로 보충하지 않는다. 컨테이너 필드에는 게임 재화/무게 등의 단위가 없다.

## 조회와 갱신

`await GameData.InitializeAsync()` 후 `GameData.GetTable(tableName)`, `Get(tableName, id)`, `TryGet(tableName, id, out row)`를 사용한다. 테이블과 행/열 인덱스를 공유하며 C# 생성 접근자도 같은 서비스에서 읽는다. 메인 스레드 호출, 초기화 전 접근 오류, 동시 로드 공유, 실패 후 재시도, 애플리케이션 종료/명시적 Release의 수명 규칙은 [Data Manager](../DataManager.md)에 따른다.

CSV → 전체 검증/미리보기 → 단일 DB 갱신 → 콘텐츠 빌드 순서다. 기존 GUID/주소를 보존한다. 누락된 행·열·테이블과 타입/키 변경은 기본 차단하며 명시적인 삭제 허용 이후 적용한다. 빈 폴더는 가져오지 않는다. 미리보기 후 CSV/DB가 바뀌면 새 검증이 필요하다.

다른 기능이 데이터를 사용한다는 이유로 두 번째 GameDatabase.asset, data/catalog 주소, 전역 캐시/로더를 만들지 않는다. 새 콘텐츠 테이블은 개별 명세와 색인 등록 후 기존 컨테이너에 포함한다. FarmingDatabase는 기존 전용 계약이므로 복제하지 않으며, 통합 요청이 생기면 소비자 이전을 포함한 별도 작업으로 처리한다.

## 중복 조사와 검증

`ScriptableObject`, `GameDatabase`, `FarmingDatabase`, Addressables 그룹을 검색해 컨테이너 1개와 전용 파밍 DB의 역할을 대조했다. 이 컨테이너와 그 원본 CSV는 생성 관계이지 독립 데이터 두 벌이 아니다. 새로운 의미 중복 검사를 자동 제공하지는 않는다.

기존 32개 테스트·실제 Play·번들 로드 기록: [검증 이력](../DataHistory/20261118-data-tool-02-verified.md). 이번 문서 작성에서는 스키마/에셋 GUID/주소/소비 코드를 확인했으며 DB를 다시 생성하거나 값을 바꾸지 않았다.

2026-11-18 게임 개발 작업에서 엔딩 테이블을 추가했다. 컨테이너 GUID/주소/그룹과 기존 ExampleItems를 보존했다. 이후 검증은 [게임 개발 완료 이력](../DataHistory/20261118-game-dev-03-verified.md)에 기록한다. `GameDevManager`가 `GameData.GetTable(GameDevKeys.EndingsTable)`로 조회하는 소비자로 추가되었으며 별도 로더는 없다.

## 2026-11-18 Characters 테이블 추가
[Characters](Characters.md)의 정본 CSV를 기존 DB와 GameData로 제공한다. 기존 GUID/주소/그룹 및 엔딩/ExampleItems 값 유지.

## 2026-11-18 Events 테이블 추가

[Events](Events.md)의 id/title/content를 기존 DB와 GameData로 제공한다. EventPopupController → EventCatalog → EventLaptopPopup 소비 연결. 기존 CSV 값·DB GUID·주소 유지, 별도 로더 없음. 콘텐츠 빌드 및 실제 Addressables 로드 검증: [이력](../DataHistory/20261118-event-popup-04-verified.md).

## 2026-11-18 선택형 이벤트 v2

Events의 임시 int ID 1~3을 사용자 ev_* string ID 12건과 선택 효과/조건으로 이전하고 ShelterItems 3건을 추가했다. 기존 Characters/GameDevEndings/ExampleItems 값과 컨테이너 GUID/data/catalog 유지. 당시 테이블에는 ExampleItems가 있었으며 현재는 폐기했다. 현재 테이블: Characters, Events, GameDevEndings, ShelterItems. Effect/ItemCatalog는 이미 로드된 테이블의 읽기 전용 해석기이며 별도 로더가 아니다. [이력](../DataHistory/20261118-event-effects-02-verified.md).

예제 정리: 기존 임포터로 ExampleItems만 제거해 DB와 접근자를 재생성했다. GUID/주소는 유지한다. [검증](../DataHistory/20261118-example-cleanup-02-verified.md).
