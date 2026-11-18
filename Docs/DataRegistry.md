# 데이터 목록과 중복 방지 규칙

AI가 데이터 작업을 시작할 때 가장 먼저 읽는 공용 색인이다. 현재 계약은 이 목록과 `DataSpecs`에, 과거 값과 변경 근거는 [DataHistory](DataHistory)에 기록한다. 목록과 명세는 현재 상태에 맞춰 갱신하고 과거 이력은 새 파일로만 추가한다.

최초 문서화 근거와 검사 결과: [20261118-data-governance-01-documented](DataHistory/20261118-data-governance-01-documented.md).

## 등록된 데이터

중간 발표의 시각·등급별 멘탈과 첫 좀비 습격 틱은 기존 `data.bunker.rules`에서 편집한다. 시계·팝업은 이 규칙과 시뮬레이션의 회차 상태를 읽는다. [필드 명세](DataSpecs/BunkerRules.md) · [원문 대조와 검증](DataHistory/20261119-midterm-clock.md)

수면실 정원은 기존 `data.bunker.rules.sleepingRoomCapacity`가 소유한다. 이동 중 자리 예약과 구조 대상의 병상은 생존자의 회차 상태로 계산한다. [방 규칙](DataSpecs/BunkerRules.md) · [변경 전 조사](DataHistory/20261119-sleep-capacity-plan.md)

수면실·전투방의 틱당 멘탈과 구조 후 각성 기준은 기존 `data.bunker.rules`에서 편집한다. 구조 대상과 운반자는 회차 상태다. [규칙 명세](DataSpecs/BunkerRules.md) · [파밍·벙커 통합 Play 검증](DataHistory/20261119-farming-shelter-play-verified.md)

벙커 생존자의 피곤한 표정 전환 기준은 기존 `data.bunker.rules`에서 담당한다. 캐릭터 식별과 현재 멘탈은 기존 `data.characters` 및 CharacterIdentity를 사용한다. [멘탈 표시 명세](DataSpecs/BunkerRules.md) · [번들·Play 검증](DataHistory/20261119-survivor-animation-verified.md)

게임 실행 소스는 기존 `editor.data-tool`에서 선택한다. 연결 목록은 SpreadConfig를 재사용하며 켜면 실행 때마다 활성 시트를 읽고, 끄면 내부 CSV 기반 데이터를 사용한다. [설정 명세](DataSpecs/DataToolSettings.md) · [두 모드 Windows 실행 검증](DataHistory/20261119-live-sheets-02-verified.md)

파밍 플레이어는 `data.farming.rules.startingPersonId`가 참조하는 `data.farming.pickups`의 Person이다. 현재 1번은 빈손으로 시작하며 처음부터 벙커 명단에 들어간다. 나머지 다섯 명은 세 방에 2·2·1명씩 배치한다. [필드 명세](DataSpecs/FarmingRules.md) · [변경 이력](DataHistory/20261119-farming-starting-player.md)

파밍 무작위 배치는 기존 `data.farming.rules`의 거리순 방별 인원과 레전드 가중치를 사용한다. 생성 대상은 `data.farming.pickups`, 물자 등급은 `data.shelter.items.rank`에서 조회한다. [배치 규칙](DataSpecs/FarmingRules.md) · [3,000회 배치와 Play 검증](DataHistory/20261119-farming-spawns-02-verified.md)

확정 아이템은 기존 `data.shelter.items`의 12종과 `data.farming.pickups`의 물자 12종으로 연결한다. 멘탈 회복 물자 4종은 ShelterItems의 useKind와 mentalRestore로 가방 사용 효과를 결정한다. 통조림의 회분과 허기 회복량은 `data.bunker.rules`에서 읽는다. [아이템 효과](DataSpecs/ShelterItems.md) · [기존 12종 연결](DataHistory/20261118-item-roster-02-verified.md)

좀비 접근 시간 6틱과 보초 원거리 사격 주기 2틱은 `data.bunker.rules`가 담당한다. `data.zombies`는 좀비의 공용 보행 속도와 근접전 주기를 공급한다. [벙커 규칙](DataSpecs/BunkerRules.md) · [거리·사격 검증](DataHistory/20261119-zombie-volley-02-verified.md)

좀비 다인 전투는 기존 `data.zombies`의 공용 속도·판정 주기·피해와 `data.characters`의 개인 전투력을 사용한다. 공격 대상, 남은 HP, 점거 여부는 전투 중인 개체가 관리한다. [전투 명세](DataSpecs/Zombies.md) · [다인 전투 도입 이력](DataHistory/20261118-zombie-group-combat.md)

음식·장비 작업(Codex, 2026-11-18) 완료. 기존 `data.farming.pickups`와 `data.shelter.items`의 공통 행 `canfood`를 독립 통조림으로 연결했다. 허기·회분은 `data.bunker.rules`에서 편집한다. [검증 및 편집 위치](DataHistory/20261118-hunger-02-verified.md).

벙커 HUD의 24시간 시계와 생존자 머리 위 멘탈 바는 기존 `data.bunker.rules`의 전체 틱·멘탈 상한과 플레이 중의 현재 틱·멘탈을 읽는다. [화면 명세](UI/DevGageUI.md) · [소비 연결 이력](DataHistory/20261119-shelter-hud.md)

벙커 캐릭터 정보 카드는 기존 `data.characters`의 이름·직군·네 가지 능력과 `data.bunker.rules`의 멘탈 상한을 읽는다. 현재 멘탈은 선택한 캐릭터의 런타임 상태다. [화면 명세](UI/ShelterCharacterCard.md) · [검증](DataHistory/20261118-shelter-character-card-verified.md)

벙커 가방은 `data.farming.pickups`의 투입 결과와 `data.shelter.items`의 이벤트 재고를 함께 읽는다. 같은 종류를 여러 개 보유하면 개수만큼 슬롯을 표시한다. [가방 UI](UI/ShelterInventory.md) · [연동 검증](DataHistory/20261118-shelter-inventory-verified.md)

엘리베이터는 `data.shelter.elevators`의 차량 속도·승하차 지점·대기 간격을 사용한다. 같은 층에서 대기하는 사람·좀비 두 명까지 함께 타고, 지상의 좀비 간격은 `data.zombies`의 minimumSpacing을 사용한다. [명세](DataSpecs/ElevatorRules.md) · [동승 변경 이력](DataHistory/20261118-shelter-elevator-pairs.md).

벙커 카메라 작업 `Codex shelter-camera-20261118` 완료: 기존 `data.characters.id`의 person_01~06을 번호키 참조로 재사용한다. 캐릭터 정본/수치/스키마/로더 변경 및 새 데이터 정본 없음. 화면/입력 설정은 기존 씬의 ShelterCamera에서 조정한다. [캐릭터 명세](DataSpecs/Characters.md), [카메라](ShelterCamera.md), [57개 Play 검사·시각 검증](DataHistory/20261118-shelter-camera-verified.md).

좀비 시트 작업 `Codex zombie-sheets-20261118` 완료: 기존 `data.zombies`의 편집 정본을 [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193)/[ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)로 이전했다. 기존 SO/GUID/주소와 런타임 로더를 재사용하고 SpreadConfig에 2개 탭을 추가했다. 초기값 변경 없음, 총 7개 활성 연결 및 실제 가져오기 대조 완료. [종류 명세](DataSpecs/Zombies.md), [규칙 명세](DataSpecs/ZombieRules.md), [최종 검증](DataHistory/20261118-zombie-sheets-03-remote-verified.md).

파밍 사이드뷰 작업 `Codex farming-sideview-20261118` 완료: 기존 `data.farming.rules`/`data.farming.pickups`를 재사용하여 수평 이동 및 E 문 왕복으로 소비를 전환했다. 92개 Play 검사·실제 60초·재시작·아트 교체 검증 통과. 데이터 값/정본 변경 및 새 CSV/로더 없음. [검증](DataHistory/20261118-farming-sideview-02-verified.md).

좀비 대기열 작업 `Codex zombie-queue-20261118` 완료: 기존 `data.zombies` SO에 최소 간격 minimumSpacing=2.4만 확장. 생성 순서·간격·정지는 런타임 상태이며 새 DB/CSV/로더 없음. [명세](DataSpecs/Zombies.md), [검증](DataHistory/20261118-zombie-queue-02-verified.md).

SpreadConfig 연결 작업 `Codex spread-config-20261118` 완료: 기존 `editor.data-tool`의 시트 연결 목록을 사용자 [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615)로 이전했다. Unity에는 Config 주소와 CSV 폴더만 저장한다. 기존 5개 데이터 정본/스키마/경로/DB를 재사용한다. 자동 테스트 57개, 실제 5개 시트 가져오기·UI 버튼·오류/취소 파일 보존 검증 통과. [검증](DataHistory/20261118-spread-config-02-verified.md), [명세](DataSpecs/DataToolSettings.md).

양식 문서 작업 `Codex data-reference-document-20261118`: 사용자 지정 [CSV 데이터 레퍼런스 문서](https://docs.google.com/document/d/1wl9JY-Iiv316ucy_pDvmogp1xr20tYZP/edit)를 현재 5개 시트 계약으로 갱신했다. 데이터 값·스키마·정본 위치 변경 없음. 엔딩 평가 단위 전환은 기존 ending-popup 담당 작업을 참조하며 중복 구현하지 않는다. [기록](DataHistory/20261118-data-reference-document.md).

엔딩 팝업 작업 `Codex ending-popup-20261118`: 기존 `data.game-dev.endings` 시트의 minimumScore를 비율 단위 0/0.3/0.5로 조정하고 팝업을 연결했다. `data.bunker.rules`에 평가 목표 진척 합96/기준 틱24를 추가했다. 새 정본·로더 없음. [검증](DataHistory/20261118-ending-popup-02-verified.md).

시트 버튼 작업 `Codex sheet-button-20261118`: 전체 시트 가져오기 버튼을 연결했다. 당시 추가한 farmingSheetUrl은 후속 SpreadConfig 작업에서 폐기하고 같은 파밍 시트를 Config로 연결했다. 편집 정본은 기존 Google Sheets 5개. 내부 CSV/클래스/DB는 생성물이며 새 게임 데이터 ID/CSV/DB/로더는 없다. 파밍을 공용 DB에 중복 등록하지 않는다. [사용법](DataManager.md), [계획](DataHistory/20261118-sheet-button-01-plan.md).

파밍 맵 작업 `Codex farming-map2d-20261118`: 2D 복도/호실, 방향 정정, 아이소메트릭 깊이 정렬과 방 노출 적용 완료. 기존 `data.farming.rules`에 방 노출 시간을 추가하고 경계 소비를 이전했다. `data.farming.pickups`의 ID/무게/CSV 경로와 정본은 재사용한다. 새 데이터 정본 없음. [검증](DataHistory/20261118-farming-map2d-04-verified.md).

좀비 침입 작업 `Codex zombie-invasion-20261118`: `data.zombies` active, 정본 `Assets/Settings/Shelter/ZombieData.asset`, 좀비 HP/전투/이동 수치. [명세](DataSpecs/Zombies.md). 기존 `data.bunker.rules`에 전역 멘탈 상한·기절 틱·회복 멘탈을 확장했다. 캐릭터 능력과 NEXT_WAVE_HP는 기존 정본 재사용. [검증](DataHistory/20261118-zombie-invasion-03-verified.md).

노션/시트 작업 `Codex notion-sheets-20261118`: 기존 data.characters와 data.shelter.items를 v2 콘텐츠로 확장했다. 기존 ID/CSV 경로/DB를 재사용한다. 최초 6개 Google Sheets를 게시했고 예제 제거 후 현재 게임 데이터 5개·36행 전체 대조를 통과했다. 편집 정본을 시트로 전환하며 로컬 CSV는 내려받은 스냅샷으로 관리한다. [개발 대조/연결 상태](NotionDevelopmentCheck.md), [계획](DataHistory/20261118-notion-sheets-02-content-plan.md).

맵 재배치(2026-11-18): `data.characters`의 생성 위치와 `data.bunker.rules`의 방 분류 소비는 기존 씬 참조를 재사용한다. CSV/SO 수치·ID·주소 변경이나 새 데이터 정본은 없다. [배치 및 검증 이력](DataHistory/20261118-shelter-map-layout.md).

2026-11-18에 프로젝트 파일·소비 코드·Addressables 그룹을 대조했다. `데이터 ID`는 문서에서 쓰는 안정적인 식별자이며 CSV의 행 ID나 Addressables 주소와 구분한다.

| 데이터 ID | 상태 / 담당 | 담당 의미 · 검색 별칭 | 편집 정본 | 명세 |
|---|---|---|---|---|
| `data.shelter.elevators` | active (밸런스 임시) / Codex shelter-elevators-20261118 | 공용 엘리베이터 차량 속도·승하차 지점·호출 대기 간격 | `Assets/Settings/Shelter/ElevatorRules.asset` | [ElevatorRules](DataSpecs/ElevatorRules.md) |
| `data.zombies` | active (이동 수치 임시) / Codex zombie-sheets-20261118 | 좀비 ID·웨이브 구간·기본 HP·전투력·주사위 판정·이동/접촉 | [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193) / [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486) → `Assets/Settings/Shelter/Zombies.csv`, `ZombieRules.csv` → 기존 `ZombieData.asset` | [Zombies](DataSpecs/Zombies.md), [ZombieRules](DataSpecs/ZombieRules.md) |
| `data.shelter.items` | active (사용·장착 연결) / 벙커 물자 | 물자 ID·표시명·사용 또는 장착 분류·멘탈 회복량·등급·획득 이벤트·장착 전투/개발 보정 | [ShelterItems](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376) → `Assets/Settings/Data/Csv/ShelterItems.csv` | [ShelterItems](DataSpecs/ShelterItems.md) |
| `data.events` | active (사용자 예시) / 선택형 이벤트 / Codex event-effects-20261118 | 사건 ID·타이틀·본문·Yes/No 효과·발생 조건, Events, eventId, laptop; 《60 Seconds!》식 사건 알림 참고 | [Events](https://docs.google.com/spreadsheets/d/1OCAvPsKuDhUua0sQuHHT0L1Hr6R24ZBA2fU4QzJ6CMU/edit?gid=1622955486#gid=1622955486) → `Assets/Settings/Data/Csv/Events.csv` | [Events](DataSpecs/Events.md) |
| `data.bunker.rules` | active / Codex 벙커 시뮬레이션 | 벙커 전체 틱, 초/틱, 이벤트·작업·습격 주기, 좀비 접근 시간, 보초 원거리 사격 주기, 침입 시 전원 멘탈 손실, 전투방 틱당 멘탈 소모·수면실 회복, 개발 효율·작업 멘탈, 멘탈 상한·기절·회복, 허기·통조림 | `Assets/Settings/Shelter/BunkerRules.asset` | [BunkerRules](DataSpecs/BunkerRules.md) |
| `data.example-items` | deprecated (삭제 완료) / Codex example-cleanup-20261118 | 폐기한 도구 시연 데이터, ExampleItems | 없음. 시트/CSV/생성 DB/연결 설정에서 제거 | [폐기 명세](DataSpecs/ExampleItems.md) |
| `data.farming.pickups` | active / 파밍 | 사람·물자 종류, 파밍 운반 무게/슬롯 비용, pickup weight, carry cost | [FarmingData](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264) → `Assets/Settings/Farming/FarmingData.csv` | [FarmingData](DataSpecs/FarmingData.md) |
| `data.farming.rules` | active / 파밍 | 제한시간, 운반 용량, 이동속도, 상호작용 거리, 던지기, 경고, 기본 호실 크기, 방 노출 시간, 기본 확보된 플레이어, 거리순 호실별 인원과 레전드 스폰 가중치 | `Assets/Settings/Farming/FarmingRules.asset` | [FarmingRules](DataSpecs/FarmingRules.md) |
| `audio.cue.ui-click` | placeholder / 오디오 | 공용 UI 클릭음, UI_Click, button click | `Assets/Settings/Audio/UI_Click.asset` | [SoundCue](DataSpecs/SoundCue.md) |
| `audio.cue.sfx-default` | placeholder / 오디오 | 기본 효과음 설정 예제, SFX_Default | `Assets/Settings/Audio/SFX_Default.asset` | [SoundCue](DataSpecs/SoundCue.md) |
| `audio.cue.ambience-loop` | placeholder / 오디오 | 반복 환경음 설정 예제, Ambience_Loop | `Assets/Settings/Audio/Ambience_Loop.asset` | [SoundCue](DataSpecs/SoundCue.md) |
| `audio.manager-defaults` | active / 오디오 | 음성 풀, 채널 기본 볼륨, 씬 전환/포커스 정책, 믹서 경로 | `Assets/Prefabs/Audio/Resources/SoundManager.prefab` | [SoundSettings](DataSpecs/SoundSettings.md) |
| `audio.user-settings` | active / 오디오 | 사용자 Master/Music/SFX/UI/Ambience 볼륨·음소거 저장 | `GameJam.Audio.*` PlayerPrefs | [SoundSettings](DataSpecs/SoundSettings.md) |
| `system.game-database` | generated / 공용 데이터 도구 | 공용 CSV 결과 컨테이너, GameDatabase, data/catalog | 자체 편집 정본 없음. 등록 CSV에서 생성 | [GameDatabase](DataSpecs/GameDatabase.md) |
| `data.game-dev.endings` | active (밸런스 임시) / 게임 개발·엔딩 / Codex ending-popup-20261118 | 비율 평가값별 엔딩, GameDevEndings, minimumScore, Ending, 필요 점수·팝업 | [GameDevEndings](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846) → `Assets/Settings/Data/Csv/GameDevEndings.csv` | [GameDevEndings](DataSpecs/GameDevEndings.md) |
| `data.characters` | active (노션 콘텐츠) / Codex notion-sheets-20261118 | 캐릭터 이름·능력·초기 멘탈·직군·소개·노션 ID, Characters | [Characters](https://docs.google.com/spreadsheets/d/1OHkc6LfkRrNK5U2ZG6ENSyFSC5L_QOgUnAex0D5l6X8/edit?gid=897086903#gid=897086903) → `Assets/Settings/Data/Csv/Characters.csv` | [Characters](DataSpecs/Characters.md) |
| `editor.data-tool` | active / Codex live-sheets-20261118 | 실행 시 시트/내부 CSV 선택, CSV 폴더·Config URL·시트 연결 목록, Excel 공유 읽기 | 로컬 설정 `ProjectSettings/GameDataTool.asset`; 연결 목록 [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615) | [DataToolSettings](DataSpecs/DataToolSettings.md) |

`data.events`는 사용자 예시 12건과 Yes/No 효과를 연결했고 `data.shelter.items`는 벙커 물자 참조 목록이다. 현재 planned 예약은 없다. data.characters는 구조 ID 기반 배치 연결과 검증을 완료했다. `data.game-dev.endings`는 연결·검증을 마쳤으며 엔딩 이름/점수는 사용자 요청대로 임시안을 유지한다. 벙커 진행도 증가와 총 틱 종료는 data.bunker.rules가 담당한다. 새 작업에서는 아래 절차에 따라 이 표에 행을 먼저 추가한다. 새 CSV는 **테이블마다** 명세를 작성한다. 같은 SO 스키마를 공유하는 에셋들은 SoundCue처럼 명세 한 곳의 인스턴스 목록으로 관리할 수 있지만, 각 에셋의 역할·경로·GUID·상태를 개별 등록해야 한다.

상태: `planned`(정본 생성 전 예약), `active`(실사용), `example`(실게임 규칙 아님), `placeholder`(미연결 입력이 있음), `generated`(파생 결과), `deprecated`(대체 데이터와 폐기 조건 필수). `planned`에는 기능 담당 외에 실제 작업 ID/담당자도 적고, 종료 시 상태를 갱신한다.

## 생성 전 중복 검사

1. 요청을 의미로 풀어 쓴다. 예: “파밍에서 대상 하나를 들 때 차지하는 칸 수”. 파일명뿐 아니라 한국어·영문 별칭, 단위와 소비 기능을 비교한다.
2. 위 목록 및 연결 명세를 읽고 실제 파일을 검색한다. 미등록 파일과 진행 중 변경도 확인한다.

```powershell
rg --files Assets ProjectSettings Docs -g '*.csv' -g '*.asset' -g '*.cs' -g '*.prefab' -g '*.md'
rg -n -i 'weight|carry|capacity|무게|운반|용량' Assets/Scripts Assets/Editor Assets/Settings Assets/Prefabs Docs
rg -n 'm_Address:' Assets/AddressableAssetsData/AssetGroups -g '*.asset'
```

3. 기존 계약이 맞으면 그 데이터/필드/ID를 재사용한다. 부족한 속성만 기존 명세에 추가한다. UI 표시나 다른 씬의 사용은 새 정본의 근거가 아니다.
4. 별도 데이터가 필요하면 [명세 양식](DataSpecs/_Template.md)의 중복 조사표에 비교 대상, 재사용 불가 이유, ID 관계를 기록한다. 같은 필드명을 써도 단위·대상·적용 범위가 다르면 그 차이를 명시한다.
5. 정본 파일을 만들기 전에 이 색인에 `planned`로 등록한다. 최신 파일/다른 작업의 예약과 충돌하는지 적용 직전 다시 확인한다. Markdown 예약은 잠금 장치가 아니므로 병합 시에도 정본이 둘이 되지 않았는지 검사한다.

## 정본과 관계의 규칙

- 하나의 의미를 바꿀 때 독립적으로 편집해야 하는 파일이 둘이면 정본 중복을 의심한다. `CSV → 생성 SO → 캐시`처럼 자동 생성되는 관계는 허용하며 명세에 방향을 적는다.
- 기존 테이블에서 파생 가능한 표시 목록·집계는 조회로 계산한다. 성능 때문에 캐시가 필요하면 원본, 생성식, 갱신/무효화 시점을 적고 캐시를 별도 편집하지 않는다.
- 추가 테이블은 기존 엔티티 ID를 외래키로 참조한다. 예를 들어 제작법이 필요해도 기존 아이템 이름/무게 전체를 복사하지 않는다. 참조 대상·삭제 시 동작·실제 검증기를 명시한다.
- 기본 설정, 현재 플레이 상태, 저장값을 구분한다. 남은 시간/현재 운반 목록/획득량을 FarmingRules/FarmingData에 쓰지 않는다. 사용자 볼륨은 SoundCue나 CSV에 저장하지 않는다.
- 테이블 이름은 공용 CSV 폴더의 하위 폴더까지 대소문자 무시로 고유해야 한다. 레코드 ID는 명세의 범위에서 고유해야 하며, 표시 이름 변경 시 ID를 바꾸지 않는다. 주소도 프로젝트 전체에서 고유해야 한다.
- CSV 저장 형식은 **UTF-8 BOM 포함**으로 통일해 Excel의 기본 열기에서 한글 인식을 돕는다. 기존 BOM 없는 UTF-8 입력도 임포터는 계속 지원한다. BOM 추가는 데이터/스키마 변경과 구분하고 본문을 그대로 보존한다.
- Google Sheets의 각 `tableName`은 기존 테이블 명세에 연결한다. 동일한 시트를 로컬 CSV와 별도 테이블 두 개로 등록하지 않는다. 시트는 선택적 가져오기 원본이며 로컬 CSV/생성 DB와의 갱신 방향을 명세에 적는다.
- 새로운 공용 테이블은 Data Manager를 기본으로 사용한다. 기존 파밍/오디오에는 현재 계약을 유지한다. 전용 로더를 새로 만들 때는 기존 로더를 확장할 수 없는 구체적인 이유와 수명 소유자를 명세에 기록한다.

## 현재 혼동하기 쉬운 경계

| 항목 | 사용할 계약 | 중복 방지 판단 |
|---|---|---|
| 파밍 대상의 운반 비용 | `data.farming.pickups.weight` | 단위는 정수 칸 수. `ExampleItems.weight`는 데모용 실수이며 실게임 무게 단위가 아님 |
| 파밍 최대 운반 용량 | `data.farming.rules.capacity` | 인벤토리/HUD의 복제 기본값으로 따로 관리하지 않음 |
| 실게임 아이템 가격/판매 규칙 | 아직 등록된 정식 계약 없음 | ExampleItems.price를 정식 가격으로 오인하지 않음. 새 요청 시 기존 파밍 엔티티와의 관계를 먼저 설계 |
| 개별 소리 크기 | `SoundCue.volume` | 음원별 gain. 사용자 채널 볼륨과 다른 의미 |
| 사용자 음량·음소거 | `audio.user-settings` | 기존 PlayerPrefs 키 사용. 새로운 AudioSettings.csv/저장 키를 중복 도입하지 않음 |
| DB 컨테이너 | `system.game-database` | GameDatabase.asset와 DataTables.cs는 생성물. CSV 값을 직접 복사해 관리하지 않음 |

## 완료 조건과 검증 한계

- 새 데이터/인스턴스가 목록과 명세에 등록되어 있고 필드·단위·참조·소비자가 실제 코드와 맞는다.
- 재사용 또는 신규 분리의 근거, 중복 검색 범위/결과를 기록했다. 발견한 미등록/충돌 항목을 조용히 무시하지 않았다.
- 명세에서 요구하는 제약을 검증했다. 도구가 자동 검사하지 않는 외래키·의미 중복·업무 범위는 별도로 검사하고 결과를 남긴다.
- CSV/SO/ID/주소/소비 연결 변경 시 `Docs/DataHistory`에 이전/이후와 검증을 새 기록으로 추가했다. 문서만 변경했으면 그 범위를 기록한다.
- 이전 작업은 모든 소비자를 정본으로 전환한 뒤 구 정본을 `deprecated`로 표시하고 남은 참조/저장 ID 호환성을 확인한다. 이름에 New/V2를 붙여 두 정본을 영구 유지하지 않는다.

현재 임포터는 CSV의 중복 파일명/열/ID와 타입 오류를 검사한다. 이 문서의 **의미가 같은 데이터 간 중복 방지와 명세 존재 여부는 AI 작업 절차**이며, 임포터가 Markdown을 읽어 자동 차단하는 기능은 아니다. 이 목록은 프로젝트 게임 데이터·오디오 설정·데이터 도구 설정을 다루며 Unity 패키지 내부 데이터와 렌더러/머티리얼 설정 전체의 명세는 아니다.
