# FarmingData 명세

## 전체 시트 가져오기 연결 (2026-11-18)

연결은 [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615)의 sheetName=FarmingData 행에서 관리한다. 구 farmingSheetUrl 필드는 제거했다. `Tools > Data Manager > 전체 시트 가져오기 · CSV / 클래스 갱신`에서 공용 시트와 함께 내려받되 기존 `FarmingCsvImporter`가 원래 FarmingData.csv/FarmingDatabase.asset만 갱신한다. Config에는 같이 등록하지만 공용 DB에는 중복 등록하지 않는다. 파싱 클래스는 기존 FarmingRecord를 재사용한다. 비활성/미등록 연결은 기존 파밍 데이터를 유지한다.

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.farming.pickups`. 편집 정본: [FarmingData](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264).
- 로컬 스냅샷: `Assets/Settings/Farming/FarmingData.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager의 전체 시트 가져오기 버튼. SpreadConfig의 id/gid를 사용해 내려받고 기존 경로에 UTF-8 BOM으로 저장하며 기존 FarmingCsvImporter가 파싱한다. 공용 CSV 폴더/DB에는 등록하지 않는다.
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기로 12행의 모든 셀/헤더를 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

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
| 소비자 | FarmingSession이 FarmingPickup.RecordId를 조회해 Initialize. FarmingInventory/Player/Hud가 Type/Weight를 사용 |
| 수명/실패 | FarmingSession 종료 시 로더 핸들 해제. 로드/Validate 실패 시 시작 차단 |

생성 SO는 직접 수정하지 않는다. 전용 검증기가 있는 기존 CSV이므로 공용 CSV 폴더에 복사하거나 Data Manager에 두 번째로 등록하지 않는다. 신규 ID는 씬/프리팹에 자동 배치되지 않는다.

## 필드와 관계

| 필드 | 타입 | 의미/단위 | 필수·기본값 | 실제 검사 |
|---|---|---|---|---|
| id | string | 사람/물자 정의 ID / 단위 없음 | 필수, 기본값 없음 | 공백 문자가 없어야 함, 테이블 내 고유, 대소문자 구분 |
| type | FarmingObjectType | Person(직렬화 0), Item(1) | 필수, 기본값 없음 | CSV는 정확히 Person/Item 문자열, enum 유효성 검사 |
| weight | int | 한 개체의 운반 비용 / **칸** | 필수, 기본값 없음 | Person 2~4, Item 1~2; 정수만 허용 |

검사 구현: `FarmingCsvImporter.Parse` 및 `FarmingDatabase.Validate`. `FarmingPickup.recordId`는 이 테이블 `id`의 외래키이며 `FarmingSession` 초기화 시 `Get`으로 누락을 검출한다. 같은 물자 ID를 가진 여러 씬 개체는 허용한다. 정의 행은 하나이고 각 개체의 운반 상태만 별도로 유지한다.

## 현재 레코드

| ID | 역할/type | weight(칸) |
|---|---|---|
| person_01 | 사람 1 / Person | 2 |
| person_02 | 사람 2 / Person | 3 |
| person_03 | 사람 3 / Person | 4 |
| person_04 | 사람 4 / Person | 2 |
| person_05 | 사람 5 / Person | 3 |
| person_06 | 사람 6 / Person | 4 |
| water | 물 / Item | 1 |
| food | 식량 / Item | 1 |
| medkit | 의료품 / Item | 2 |
| radio | 라디오 / Item | 2 |
| toolbox | 도구 / Item | 2 |
| blanket | 담요 / Item | 1 |

표의 역할 설명은 문서상 의미이며 CSV에 표시 이름 필드가 추가된 것은 아니다. 사람 6행/물자 6행이며 씬의 물자 개체 수와 정의 행 수는 다를 수 있다. ID를 재사용/개명해 새 의미를 덮어쓰지 않는다.

정렬은 CSV 순서. 활성화 필드는 없다. 기존 ID가 누락된 가져오기는 전용 임포터가 거부한다. 삭제/ID 변경은 씬/프리팹 참조와 후속 저장 계약을 검사하는 명시적 이전 작업으로 수행한다. 현재 후속 씬·세이브 연결은 없으며 미래 저장 키와의 호환성은 별도 설계한다.

## 중복 조사와 검증

- 검색한 범위: Farming CSV/Database/Rules/Definitions, Session/Inventory/Player/Pickup/Hud, 공용 ExampleItems.
- `ExampleItems.weight`는 테스트용 실수이고 이 데이터는 정수 슬롯 비용이다. 실제 파밍 비용은 여기에만 둔다.
- `FarmingRules.capacity`는 운반자의 전체 용량으로 다른 속성이다. 개체마다 전체 용량을 복제하지 않는다.
- 목록/명세 작성은 기존 계약을 기록한 것이며 이관이나 수치 변경은 하지 않았다.
- 실제 기능 검증 이력: [파밍 검증 스냅샷](../DataHistory/20261118-143045-3883526-verified.md), [검증 설명](../Farming/Verification.md). 이번 문서 작업에서는 파일/소비 코드만 재확인했다.

## 2026-11-18 캐릭터 연결
Person의 기존 id를 별도 [Characters](Characters.md)의 id에서 참조한다. 기존 CSV의 type/weight/ID는 유지한다. FarmingSession은 시작 전에 1:1 관계를 검증하며 벙커 이동 시 투입 Person ID를 전달한다. 이전 문서의 후속 연결 없음 설명은 이 계약으로 대체한다. 영구 저장은 없다.
