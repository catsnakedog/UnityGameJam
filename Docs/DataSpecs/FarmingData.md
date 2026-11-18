# FarmingData 명세

실행 시 시트 연동이 켜져 있으면 같은 FarmingData 연결과 CSV 파서를 사용한다. 게임 실행용 FarmingDatabase는 공용 테이블과 함께 검증되며 파밍 씬과 벙커 물자 소비자가 동일한 인스턴스를 받는다. 꺼짐 또는 파밍 연결 비활성 시 기존 내부 CSV 기반 DB를 사용한다. 데이터 수명은 GameData.Release 또는 게임 종료까지이고 파밍 씬의 규칙 핸들은 씬에서 해제한다. [선택 설정](DataToolSettings.md)

## 전체 시트 가져오기 연결 (2026-11-18)

연결은 [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615)의 sheetName=FarmingData 행에서 관리한다. 구 farmingSheetUrl 필드는 제거했다. `Tools > Data Manager > 전체 시트 가져오기 · CSV / 클래스 갱신`에서 공용 시트와 함께 내려받되 기존 `FarmingCsvImporter`가 원래 FarmingData.csv/FarmingDatabase.asset만 갱신한다. Config에는 같이 등록하지만 공용 DB에는 중복 등록하지 않는다. 파싱 클래스는 기존 FarmingRecord를 재사용한다. 비활성/미등록 연결은 기존 파밍 데이터를 유지한다.

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.farming.pickups`. 편집 정본: [FarmingData](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264).
- 로컬 스냅샷: `Assets/Settings/Farming/FarmingData.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager의 전체 시트 가져오기 버튼. SpreadConfig의 id/gid를 사용해 내려받고 기존 경로에 UTF-8 BOM으로 저장하며 기존 FarmingCsvImporter가 파싱한다. 공용 CSV 폴더/DB에는 등록하지 않는다.
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기와 현재 18행의 헤더·셀을 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

2026-11-18 시트 양식 지원: 기존 경로/ID/값/DB/Addressables/전용 임포터를 유지한다. `FarmingCsvImporter.Parse`는 기존 `id,type,weight` 헤더 외에 설명 1행, `string,string,int` 2행, `id,type,weight` 3행도 지원한다. 파밍 xlsx는 해당 양식의 게시 준비 산출물이며 별도 정본/공용 DB 테이블이 아니다. [노션/시트 계획](../DataHistory/20261118-notion-sheets-01-planned.md). 상단의 실제 시트 URL에서 내려받은 12행을 전용 임포터로 적용했다.

- 데이터 ID: `data.farming.pickups`; 상태: active; 담당: 파밍.
- 스키마: v1 (`id,type,weight`); 확인일: 2026-11-18.
- 저장 인코딩: UTF-8 BOM 포함. 전용 임포터는 기존 BOM 없는 UTF-8도 지원한다. 인코딩 표시만 바꿀 때 본문/ID/값은 보존한다.
- 소유 의미: 파밍에서 들 수 있는 사람/물자 종류와 대상 하나당 운반 칸 수. 검색 별칭: pickup, carry cost, 무게, 사람, 물자.
- 제외: 최대 용량·시간·속도는 [FarmingRules](FarmingRules.md), 현재 운반 목록/투입량은 런타임 상태. 가격·제작법은 아직 이 계약에 없다.

## 정본과 소비

| 항목 | 현재 연결 |
|---|---|
| 로컬 스냅샷 | [FarmingData.csv](../../Assets/Settings/Farming/FarmingData.csv) |
| 생성 결과 | `Assets/Settings/Farming/FarmingDatabase.asset`, `ShelterRun.FarmingDatabase` / `FarmingRecord` |
| GUID | `117bb726d48c1a0439f32806bea7df2d` |
| Addressables | `data/farming/database`, 그룹 `Farming Local`, 라벨 `farming` |
| 가져오기 | `Tools > Farming > Import FarmingData CSV`; CSV 저장 시 전용 postprocessor도 실행 |
| 콘텐츠 빌드 | `Tools > Farming > Build Data Content` |
| 로드/조회 | 씬 소유 `FarmingDataLoader.LoadAsync()` → `Database.Get(recordId)` / `Records` |
| 소비자 | FarmingSession이 FarmingPickup.RecordId를 조회해 Initialize. FarmingInventory/Player/Hud가 Type/Weight를 사용. EventPopupController는 기존 GameData 설정 로더로 같은 DB를 조회해 예정된 획득 이벤트의 파밍 경로를 검증 |
| 수명/실패 | FarmingSession 종료 시 로더 핸들 해제. 로드/Validate 실패 시 시작 차단 |

생성 SO는 직접 수정하지 않는다. 전용 검증기가 있는 기존 CSV이므로 공용 CSV 폴더에 복사하거나 Data Manager에 두 번째로 등록하지 않는다. 파밍 시작 시 전체 행에서 생성 대상을 읽는다. 신규 구조 대상이나 물자는 같은 recordId의 프리팹을 Farming Session의 pickupPrefabs에 연결한다. Person은 Characters 행과 1:1로 대응하며, 행 수는 플레이어 한 명과 방별 인원의 합이다. Item은 ShelterItems 행과 빈 책상 자리 수를 함께 확인한다.

## 필드와 관계

| 필드 | 타입 | 의미/단위 | 필수·기본값 | 실제 검사 |
|---|---|---|---|---|
| id | string | 사람/물자 정의 ID / 단위 없음 | 필수, 기본값 없음 | 공백 문자가 없어야 함, 테이블 내 고유, 대소문자 구분 |
| type | FarmingObjectType | Person(직렬화 0), Item(1) | 필수, 기본값 없음 | CSV는 정확히 Person/Item 문자열, enum 유효성 검사 |
| weight | int | 한 개체의 운반 비용 / **칸** | 필수, 기본값 없음 | Person 2~4, Item 1~2; 정수만 허용 |

검사 구현: `FarmingCsvImporter.Parse` 및 `FarmingDatabase.Validate`. `FarmingPickup.recordId`는 이 테이블 `id`의 외래키다. `FarmingSession`은 배치 전에 모든 생성 대상에 대응하는 프리팹을 확인한다. 시작 Person 행은 플레이어와 벙커 명단에 쓰인다. 방에는 나머지 사람과 일반·희귀·레전드 물자를 ID당 한 번 생성한다. 통조림만 방마다 하나씩 생성하며 세 개체는 각자 운반·투입할 수 있다. [방별 규칙](FarmingRules.md)

## 현재 레코드

시트의 18행은 사람 6종과 물자 12종이다. 물자의 종류와 사용 효과는 [벙커 물자](ShelterItems.md)를 같은 ID로 참조한다. 운반 비용은 이 시트에서만 편집한다. 카페인 알약(`caffeine`)의 비용은 사용자가 확정한 1칸이다.

CSV 순서로 열거하며 활성화 필드는 없다. 예고 없는 ID 삭제는 임포터가 거부한다. ID를 은퇴시킬 때 씬 배치, 프리팹, 스프라이트와 가방 참조를 함께 전환한다. 투입 기록은 현재 플레이 회차에 한정된다.

## 중복 조사와 검증

- 검색한 범위: Farming CSV/Database/Rules/Definitions, Session/Inventory/Player/Pickup/Hud, 공용 ExampleItems.
- `ExampleItems.weight`는 테스트용 실수이고 이 데이터는 정수 슬롯 비용이다. 실제 파밍 비용은 여기에만 둔다.
- `FarmingRules.capacity`는 운반자의 전체 용량으로 다른 속성이다. 개체마다 전체 용량을 복제하지 않는다.
- 목록/명세 작성은 기존 계약을 기록한 것이며 이관이나 수치 변경은 하지 않았다.
- 실제 기능 검증 이력: [파밍 검증 스냅샷](../DataHistory/20261118-143045-3883526-verified.md), [검증 설명](../Farming/Verification.md). 이번 문서 작업에서는 파일/소비 코드만 재확인했다.

## 2026-11-18 캐릭터 연결
Person의 기존 id를 별도 [Characters](Characters.md)의 id에서 참조한다. 기존 CSV의 type/weight/ID는 유지한다. FarmingSession은 시작 전에 1:1 관계를 검증하며 벙커 이동 시 플레이어와 구조 완료한 사람의 ID를 전달한다. 영구 저장은 없다.

## 2026-11-18 사이드뷰 최초 배치 이력

최초 사이드뷰 제작 당시 `FarmingSideViewBuilder`는 325호에 여섯 사람과 물자를 고정 배치했다. 현재 배치는 회차 시작 시 `FarmingSession`이 [방별 규칙](FarmingRules.md)으로 생성한다. `FarmingPlayer`는 같은 공간에서만 탐색하고 문 이동 시 Inventory와 개체 상태를 유지한다. [전환 계획](../DataHistory/20261118-farming-sideview-01-planned.md), [현재 맵](../Farming/Map2D.md).

## 벙커 가방 연결

`FarmingRunTransfer.Capture`는 벙커에 투입한 개체에서 Item 한 개당 기존 `FarmingRecord` 한 건을 `Items`에 전달한다. Person은 기존의 고유 구조 명단으로 전달한다. `ShelterSupplies.OwnedItems()`가 동일한 파밍 ID의 수량을 집계하고, 가방은 개당 한 칸을 사용한다. 툴팁의 파밍 운반 비용은 `FarmingRecord.Weight`에서 읽는다. 이 결과는 같은 파밍 회차의 벙커 재시작에도 유지되며 새 파밍 시작 시 비워진다. [실행 검증](../DataHistory/20261118-shelter-inventory-verified.md)
## 확정 물자 적용

통조림(`canfood`)은 독립적인 1칸 물자다. 세 호실에 각 한 캔을 배치한다. 한 캔의 사용 횟수와 허기 회복은 [벙커 규칙](BunkerRules.md)이 담당한다. 기존 325호 테이블 배치를 확정 아이템으로 교체하고 다른 두 호실의 배치를 유지했다. 구 물자 water, food, medkit, radio, toolbox, blanket을 씬·프리팹·DB에서 제거했다. 이후 `FarmingDatabase.Get`은 현재 행만 조회한다. [원본 대조 및 플레이 검증](../DataHistory/20261118-item-roster-02-verified.md)
