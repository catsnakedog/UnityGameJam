# Events 데이터 명세 v2

실행 시 시트 연동에서는 기존 일곱 필드의 이름과 타입으로 이벤트를 읽으며 추가 열을 허용한다. 2026-11-18 밤 조회한 시트는 38개 이벤트에 yesResult와 noResult 문자열을 추가했다. 선택한 쪽의 문자열을 노트북 본문에 표시하고 적용된 수치 요약을 아래에 표시한다. 이전 데이터에 해당 열이 없거나 문장이 비었으면 본문에 수치 요약을 표시한다. [변경 이력](../DataHistory/20261119-event-notebook-result.md) 이벤트 선택 효과·조건은 기존 필드를 그대로 사용한다. [실행 모드](DataToolSettings.md)

## 2026-11-18 좀비 생성 소비 연결

시트/CSV 수치 변경 없음. `NEXT_WAVE_HP`는 기존 BunkerSimulation의 다음 웨이브 1회 보정을 유지하며 ZombieInvasionController가 생성 시 GetCurrentWaveEnemyHp(baseHp)로 실제 좀비에 적용한다. 침입 전환은 이미 깎인 남은 HP를 유지한다. MENTAL 효과는 공용 CharacterIdentity의 BunkerRules 멘탈 상한100/탈진40틱/회복30 계약을 따른다. 이전 본문의 상한 없음/실제 적 HP 미연결 설명은 이 계약으로 대체. [좀비 명세](Zombies.md).

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.events`. 편집 정본: [Events](https://docs.google.com/spreadsheets/d/1OCAvPsKuDhUua0sQuHHT0L1Hr6R24ZBA2fU4QzJ6CMU/edit?gid=1622955486#gid=1622955486).
- 로컬 스냅샷: `Assets/Settings/Data/Csv/Events.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager에서 전체 시트 가져오기 · CSV / 클래스 갱신을 누른다. 고급 변경은 미리보기 후 적용한다. 연결 목록은 SpreadConfig의 sheetName/id/gid/enabled에서 관리하며 Unity에는 Config URL만 저장한다. [연결 명세](DataToolSettings.md).
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기로 12행의 모든 셀/헤더를 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

2026-11-18 / data.events / Codex event-effects-20261118 / v2 active (사용자 예시) / 구현·검증 완료.
정본 `Assets/Settings/Data/Csv/Events.csv` (GUID bd9bb7df5f36610478192e5056929ec1 유지). 공용 GameDatabase.asset/data/catalog 및 DataTables.cs는 생성물. 기존 GameData 캐시/초기화/해제를 재사용한다.

## 필드

| 필드 | 타입 | 의미·단위 | 필수·기본·제약 | 참조·검사 |
|---|---|---|---|---|
| id | string | 사건 고유 ID, 무차원 | 필수 ev_[a-z0-9_]+, 기본 없음, 고유/변경 금지 | EventDefinition/공용 임포터 |
| title | string | 제목 | 필수, 공백만/개행 불가, 기본 없음 | EventDefinition, UI 레이아웃 검수 |
| content | string | 본문 | 필수, 공백만 불가, 개행 허용, 기본 없음 | EventDefinition |
| yesEffects | string[] | Yes 효과 목록 | 필수 한 개 이상, | 구분. NONE은 단독 | EventEffect.ParseMany |
| noEffects | string[] | No 효과 목록 | Yes와 동일 | EventEffect.ParseMany |
| requiredItem | string | 발생 재고 조건 ID | 빈 값이면 조건 없음 | data.shelter.items.id 외래키 |
| requiredCount | int | 필요 보유량, 개 | requiredItem이 비면 0, 아니면 >0 | EventDefinition, 추첨·선택 직전 재검사 |

CSV UTF-8 BOM. 쉼표가 포함된 효과 셀은 CSV 큰따옴표로 감싼다. 문자열 배열 안의 효과는 | 로 구분한다. NONE도 명시한다. 도구 문법: [DataManager](../DataManager.md).

## 효과 계약

- MENTAL(all/random1/room:art/room:prog/room:plan, signed int): 현재 활성 CharacterIdentity 대상. 방 기준은 선택 순간 CurrentRoom, 비어 있으면 건너뜀. 멘탈 포인트, 0 하한, 별도 최대치 없음. 원본 Characters 초기값 변경 없음.
- PROGRESS(art/prog/plan/open, signed int): 기존 개발 진행도 %p, 0~100 제한. open은 선택 시작 시 100% 미만 축. 직접 지정한 축은 완료되었어도 증감 가능.
- ITEM(등록 ID, signed int): 벙커 재고 개수 증감. 부족/범위 초과면 선택 전체 거부; 다른 효과도 적용하지 않음.
- NEXT_WAVE_HP(signed int): 다음 습격 HP 가산량 누적. 다음 ZombieInvasionDue 직전에 한 번 소비. BunkerSimulation.CurrentWaveHpModifier 및 GetCurrentWaveEnemyHp(baseHp)로 제공(최소 HP 1). 기본 적 HP/실제 전투는 아직 없음.
- NONE: 변화 없음. 알 수 없는 명령·대상·ID·형식 및 빈 효과 배열은 가져오기/로드 실패. 임의 C# 실행은 하지 않음.
- 모든 효과를 미리 계산·검증한 뒤 한 번 반영. random1은 해당 효과마다 한 명 추첨. 중복 선택 입력은 무시.

## 레코드·이전

사용자 제공 ev_door/ev_radio/ev_blackout/ev_vending/ev_fight/ev_bug/ev_sns/ev_idea/ev_bacchus/ev_barricade/ev_party/ev_commit 12행. 값은 정본 CSV만 편집한다. ev_party에만 choco 1개 조건. 정렬은 표시/추첨 우선순위가 아니다.

임시 int ID 1~3은 사용자 원문 ev_* string ID로 이전한다. 기존 영구 저장·외부 참조 없음 확인, API/검사 코드 및 생성 접근자도 함께 이전. 구 임시 사건을 별도 CSV로 남기지 않는다. 이후 ID 삭제·변경은 사용처와 조건/효과 외래키 검사를 거친다. 초기 이력은 보존한다.

## 소비·수명

GameData.InitializeAsync → ShelterItemCatalog/EventCatalog → EventPopupController → EventEffectExecutor → 기존 CharacterIdentity/GameDevManager/BunkerSimulation 및 ShelterSupplies. 팝업은 EventLaptopPopup. 초기 로드 중 틱 정지; 실패 원인 기록 후 잠금 해제. 공용 캐시는 앱 수명, 사건 상태·재고·HP 보정은 씬/한 게임 수명. 재시작 시 이전 사건/멘탈/물자/HP 보정 초기화. 영구 저장 없음.

발생 시점은 기존 data.bunker.rules.randomEventEveryTicks(현재 10)로 관리. 사용자 확정: 시간은 전부 틱이며 '정시'는 설명 표현이다. 시간 환산/새 시계 없음. 직전 사건과 조건 불충족 사건 제외 후 균등 추첨. 후보 0개이면 건너뜀. 종료 틱은 개발 종료 우선. 같은 틱의 습격은 사건 선택 결과를 닫은 후 진행.

## 중복 조사·변경·검증

Assets/ProjectSettings/Docs 및 DataRegistry planned를 확인. 기존 멘탈은 data.characters, 진척은 GameDevProgress, 주기는 data.bunker.rules 재사용. FarmingInventory는 운반 슬롯, 신규 ShelterSupplies는 벙커 소비물자 수량으로 대상/수명이 다름. FarmingData의 food 등을 choco로 자동 전환하지 않음. 실제 적/HP 정본은 미발견; 기본 HP를 새로 만들지 않음.

[계획 이력](../DataHistory/20261118-event-effects-01-planned.md). 기존 Event CSV·DB 경로/GUID 유지. 신규 물자 정본은 [ShelterItems](ShelterItems.md). [구현·검증 이력](../DataHistory/20261118-event-effects-02-verified.md). 컴파일·콘텐츠 빌드·24가지 선택 결과·재고/방/반복/틱/습격 경계 검증 완료. 가져오기는 Tools > Events > Import Event CSV에서 전체 효과와 참조를 검증하고 공용 도구로 적용한다. CSV 저장 후 콘텐츠 빌드가 필요하다.
