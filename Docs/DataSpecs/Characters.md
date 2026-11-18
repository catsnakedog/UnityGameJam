# 캐릭터 능력 명세

## 2026-11-18 좀비 전투 소비 연결

기존 시트/CSV/DB/ID/초기값은 유지한다. ZombieAgent가 Definition.Combat 및 CurrentMental/SetMental을 사용한다. BunkerRules(data.bunker.rules)의 mentalMaximum=100, faintDurationTicks=40, wakeMental=30을 공용 CharacterIdentity에 연결했다. SetMental은 0 하한 계약을 유지하고 상한을 적용하며, 0인 동안 이동·작업·전투를 차단한다. 양수 회복 또는 40틱 경과 시 다시 행동 가능. ResetMental은 기존 Definition.Mental로 복원. 이전 본문의 전투/상한/기절 미연결 설명은 이 계약으로 대체한다. [좀비 명세](Zombies.md).

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.characters`. 편집 정본: [Characters](https://docs.google.com/spreadsheets/d/1OHkc6LfkRrNK5U2ZG6ENSyFSC5L_QOgUnAex0D5l6X8/edit?gid=897086903#gid=897086903).
- 로컬 스냅샷: `Assets/Settings/Data/Csv/Characters.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager에서 전체 시트 가져오기 · CSV / 클래스 갱신을 누른다. 고급 변경은 미리보기 후 적용한다. 연결 목록은 SpreadConfig의 sheetName/id/gid/enabled에서 관리하며 Unity에는 Config URL만 저장한다. [연결 명세](DataToolSettings.md).
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기로 6행의 모든 셀/헤더를 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

## 현재 계약 v2 — 노션 콘텐츠 연결 (2026-11-18)

정본과 수명은 아래 기존 CSV/DB 경로를 유지한다. [노션 개발 요구사항](https://app.notion.com/p/41caca30f5dc80239e4dd03a4d451a46)의 캐릭터 표를 기준으로 갱신한다. 기존 person_01~06은 파밍 참조 ID이며 순서대로 jiwoo, taesan, saebyeok, dasom, dohyun, gunwoo와 대응한다. designId는 노션 ID를 조회할 수 있는 고유 별칭이다. 기존 id 변경/삭제 없음. 아래 v1의 임시 이름/능력/멘탈 값 설명은 이번 계약으로 대체한다.

| 필드 | 타입 | 필수/기본값/단위/제약 |
|---|---|---|
| id | string | 필수, 기본값 없음, 기존 Farming Person와 1:1 |
| designId | string | 필수, 기본값 없음, 노션 인물 ID, 테이블 내 고유 |
| displayName | string | 필수 일반 텍스트, 기본값 없음 |
| programming / art / planning / combat | int | 필수 능력 포인트, 각 1~6, 네 능력 합 12, 기본값 없음 |
| mental | int | 필수 초기 멘탈 포인트, 0 이상, 노션 현재값 60. 런타임 상한/기절은 별도 미연결 |
| role | string | 필수 직군 표시명, 기획/아트/개발, 기본값 없음 |
| description | string | 필수 인물 한 줄 소개, 기본값 없음 |

CharacterDefinition의 동일 이름 프로퍼티(DesignId/Combat/Role/Description 포함) 및 CharacterCatalog.Get/GetByDesignId로 조회한다. 기존 소비자는 Id/DisplayName/개발 능력/Mental을 그대로 사용한다. 전투 스탯은 저장·조회만 제공하며 좀비 기능은 범위 밖. 직군/소개 표시 UI는 미연결이다. 원본 초기 멘탈과 현재 멘탈은 분리한다. 별칭 변경 시 외부 참조를 먼저 조사한다. 생성 DB/접근자는 별도 정본이 아니다.

검증 계획/중복 조사: [계획](../DataHistory/20261118-notion-sheets-02-content-plan.md). 실제 6명 Play 로드·능력·이름·초기 멘탈 검사 및 [로컬 검증 결과](../DataHistory/20261118-notion-sheets-03-local-verified.md). 시트 게시와 외부 편집 정본 전환을 완료했으며 로컬 경로/ID는 유지했다.

벙커 작업 소비 연결/검증 이력: [20261118-bunker-ticks-02-verified](../DataHistory/20261118-bunker-ticks-02-verified.md). 기존 수치 변경 없음.

- ID: data.characters; active (수치 임시) / Codex character-link-20261118 / v1 / 2026-11-18.
- 소유: 캐릭터 표시 이름, 개발 능력, 초기 멘탈. 무게/용량/진행도 퍼센트 제외.
- 로컬 스냅샷: Assets/Settings/Data/Csv/Characters.csv, UTF-8 BOM. 편집 원본은 상단 Google Sheets.
- 생성물: 기존 Assets/Settings/Data/GameDatabase.asset (GameJam.Data.GameDatabase), DataTables.cs. GUID 62a272592b807b3469b072879c349759, data/catalog, Game Data Local, 라벨 없음 재사용.
- 갱신: Tools > Characters > Import and Validate → 공용 DataImporter → 공용 Addressables 콘텐츠 빌드.
- 조회: CharacterCatalog.Get(id), CharacterIdentity.Definition/CurrentMental. 기존 GameData가 애플리케이션 로드·캐시·해제 담당. 별도 로더 없음. 실패 시 파밍 시작/벙커 생성 차단.

| 필드 | 타입 | 의미/단위 | 필수/기본값/제약 |
|---|---|---|---|
| id | string | 캐릭터 ID | 필수, 기본값 없음, 대소문자 구분 고유, FarmingData의 Person ID와 1:1 |
| displayName | string | 표시 이름 | 필수, 공백만 불가, 기본값 없음 |
| programming | int | 프로그래밍 능력/능력치 | 필수, 0 이상, 기본값 없음, 상한 미정 |
| art | int | 아트(Graphics) 능력/능력치 | 필수, 0 이상, 기본값 없음, 상한 미정 |
| planning | int | 기획 능력/능력치 | 필수, 0 이상, 기본값 없음, 상한 미정 |
| mental | int | 초기 멘탈/포인트 | 필수, 0 이상, 기본값 없음, 최대치/증감 규칙 미정 |

person_01~06에 사용자 예시 1/2/7/10을 임시 공통값으로 적용. 이름은 기존 MIRA/JIN/NOA 및 임시 PERSON 04~06. 능력→진행도 환산은 [BunkerRules](BunkerRules.md)의 계수를 사용하며 능력 정본은 이 CSV를 유지한다. 생성은 구조 순서.

FarmingData에는 능력/멘탈을 복제하지 않는다. 종료 시 투입 Person ID만 FarmingRunTransfer에 복사. 같은 ID는 한 명만 생성. 물자 및 들고만 있는 사람 제외. 새 파밍/Play 진입 시 초기화; 벙커 재로드는 명단 유지. 직접 벙커 실행은 빈 명단. 영구 저장 없음. 현재 멘탈은 인스턴스 상태이며 원본 SO를 바꾸지 않는다.

현재 생성 위치는 `ShelterPrototype.unity` 거실 바닥의 기존 6개 spawnPoints 참조다. 6개 방 배치에 맞춰 Transform 위치만 이동했으며 생성 순서·캐릭터 정본은 유지한다. 설치 도구는 기존 spawnPoints를 보존하며, 신규 배치 시 ENTRY가 없으면 유일한 Common 방을 사용한다. [맵 변경 이력](../DataHistory/20261118-shelter-map-layout.md).

ID 변경/삭제는 두 CSV와 씬 recordId를 함께 이전하며 고아 참조 거부. 누락 값 폴백 없음. CharacterCatalog에서 음수/필수값/고유 ID, ValidateFarming에서 모든 Person의 1:1 관계 검사.

중복 조사: Assets/Scripts, Assets/Settings, ProjectSettings, Docs에서 character/ability/mental/멘탈/능력 검색. 기존 능력 DB 없음. ShelterWalker 이름은 이동 예제이며 생성 인스턴스는 CSV 이름 사용. FarmingData 무게 및 GameDevProgress 퍼센트와 의미가 달라 분리. 공용 GameData/DB 재사용. planned 충돌 없음.

검증: 컴파일, Edit 검사 5개, Play 검사 40개 통과. Addressables 콘텐츠 빌드 완료. 최신 이력 ../DataHistory/20261118-character-link-02-verified.md. Player 빌드/영구 저장/멘탈 증감 게임 규칙은 범위 밖. 공용 Data Manager는 타입/중복 검사만 제공하므로 관계·음수 의미 검사에는 Characters 메뉴를 사용한다.

## 2026-11-18 사건 효과 연결

EventEffectExecutor가 CurrentMental/SetMental을 사용한다. 멘탈은 0 하한, 최대치 미정(임의 상한 없음), all/random1/room 대상이며 원본 초기 멘탈은 유지한다. 개발 재시작 시 Definition.Mental로 복원. 방 기준은 현재 방 소속이며 빈 방 효과는 건너뛴다. [검증 이력](../DataHistory/20261118-event-effects-02-verified.md). 이전 범위 밖 설명 중 멘탈 증감은 이 구현으로 대체된다.
