# 벙커 틱·작업 규칙

## 회차 난이도

일반 시작 기본값은 어려움, 튜토리얼 완료는 보통(Medium)이다. 기획자와 함께 선택한 난이도는 파밍, 벙커, 엔딩의 다시 하기까지 회차 메모리에 보관한다. 모든 난이도는 같은 `totalTicks`, 발표 일정, 작업 주기와 평가식을 쓴다.

| 난이도 | 초/틱 | 사건 간격 | 첫 습격 | 습격 간격 |
|---|---:|---:|---:|---:|
| 쉬움 | 1 | 총 틱 ÷ 16 | 총 틱 ÷ 12 | 총 틱 ÷ 8 |
| 중간 | 0.5 | 총 틱 ÷ 20 | 총 틱 ÷ 24 | 총 틱 ÷ 10 |
| 어려움 | 0.33 | 40틱 | 20틱 | 80틱 |

기존 SO의 `easy`와 `medium` 각각에 필수 유한 양수 float `secondsPerTick`(초/틱), 필수 양의 int `eventDivisions`, `firstInvasionDivisor`, `invasionDivisions`(총 틱 분할 수)를 둔다. 표의 값으로 초기화한다. 분할 결과를 가장 가까운 정수 틱으로 반올림하고 최소 1틱을 적용한다. 어려움은 기존 초/틱·사건 간격·첫 습격·습격 간격 필드를 직접 사용한다. 시뮬레이션, 시계 표시와 좀비 이동은 같은 회차 규칙을 읽는다. 쉬움·중간의 파생 규칙은 벙커 씬이 끝날 때 해제한다. 이전 시 세 소비자를 함께 전환한다.

작업 후 `WorkProgressFeedback`은 `ProcessWork`가 적용한 분야별 실제 증가량을 작업자의 유효 능력에 비례해 표시한다. 진행률 상한·작업 시점·잠금 조건은 이 규칙과 `GameDevManager`가 관리한다. [작업 완료 표시](../UI/WorkProgressFeedback.md)

## 중간 발표와 첫 좀비 습격

기존 `data.bunker.rules` 정본 [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset)에 다음 필드를 둔다. 현재 회차의 개발 진척과 발표 결과는 런타임 상태다.

| 필드 | 타입과 단위 | 값과 제약 |
|---|---|---|
| firstInvasionTick | 양의 int, 첫 습격 틱 | 20. 이후 기존 invasionEveryTicks마다 발생하며 종료 틱은 제외한다. |
| presentationTicks | int 배열, 회차 내 틱 | 240·480·720. 양의 틱을 오름차순으로 중복 없이 설정한다. 종료 틱 이후의 예약은 실행하지 않는다. |
| presentationGrades | 등급 배열 | 최저 점수 minimumScore(double, 0부터 오름차순), 표시 label(필수), 전원 mentalDelta(int, 포인트), 연출 color(RGBA)를 갖는다. 0부터 망겜 −15 빨강, 0.7부터 흠 0 회색, 1.0부터 굿 +10 초록, 1.2부터 갓겜 +20 금색. |

발표 점수는 세 진척 합 ÷ (기존 evaluationTargetProgress × 현재 틱 / totalTicks)이다. 발표 틱에는 방 작업 후 전원의 멘탈을 한 번 반영하고 일반 사건을 대체한다. 시뮬레이션과 시계는 같은 일정과 실행 기록을 읽는다. 설정을 이전하거나 제거하면 두 소비자를 함께 전환한다. [노션 중간평가](https://app.notion.com/p/41caca30f5dc801c873be391bddeeade) · [시간표](https://app.notion.com/p/41caca30f5dc8065a8aae3eccbbd09c0)

## 방 멘탈과 기절 구조

| 필드 | 타입 · 단위 | 필수 · 초기값 · 제약 |
|---|---|---|
| sleepMentalPerTick | int · 사용 중인 텐트 1인당 멘탈 포인트/틱 | 필수 5, 1 이상 |
| workRoomCapacity | int · 작업방 한 곳의 동시 작업 인원(명) | 초기 3, 1 이상. 기획·프로그래밍·아트 방에 도착한 순서로 행동 가능한 인원이 작업한다. 추가 입실자는 방 안에서 대기하며 자리가 나면 작업한다. |
| sleepingRoomCapacity | int · 한 수면실의 동시 텐트 예약·사용 상한(명) | 필수 2, 1 이상. 직접 입실과 기절자 구조가 같은 텐트를 예약한다. 방 바닥 입장에는 제한 없음 |
| guardMentalLossPerTick | int · 전투방 1인당 멘탈 포인트/틱 | 필수 2, 0 이상 |
| wakeMental | int · 수면실에 인계된 기절자의 각성 기준(이상) | 필수 50, 1 이상 mentalMaximum 이하 |

CharacterIdentity.AdvanceMentalTick이 방 소속·정지·활성 여부를 확인해 기존 멘탈에 적용한다. 운영 중인 수면실의 텐트에 들어간 사용자는 매 틱 회복한다. 이동 예약 중에는 텐트가 열려 있고 실제 입실 때 닫힌다. 구조된 사람은 각성 후에도 멘탈 상한까지 텐트 안에서 쉰다. 전투방은 주둔 중 매 틱 소모하며 교전 중에도 적용한다. 범위는 0부터 mentalMaximum까지다. 기절 후 회복량을 받아도 수면실 인계 전에는 행동할 수 없다. 구조는 다른 행동 가능한 캐릭터를 선택한 뒤 기절자를 클릭하면 시작한다. 운반자와 피구조자는 같은 경로로 이동한다. 방 멘탈과 기절 상태는 회차마다 초기화한다.

기존 guardTicksPerMental과 faintDurationTicks는 폐기하고 wakeMental은 각성 기준으로 전환한다. 시뮬레이션, 이동 명령, 초기 설치 및 검증 코드가 새 규칙을 소비한다. 별도 CSV·로더는 없으며 [변경 전 조사](../DataHistory/20261119-room-rescue-01-plan.md)에 근거를 기록했다.

## 생존자 피곤 상태 표시

`tiredMentalThreshold`는 필수 정수이며 단위는 현재 멘탈 포인트다. 사용자가 정한 초기값은 50이며 허용 범위는 0부터 `mentalMaximum`까지다. 0은 기절이 우선한다. 행동 가능한 캐릭터의 현재 멘탈이 이 값 이하일 때 피곤한 대기와 걷기를 재생한다. 이동 속도와 능력은 기존 규칙을 따른다.

씬 수명의 CharacterIdentity가 이미 받은 BunkerRules와 CurrentMental로 IsTired를 계산한다. 표시 기준을 이전할 경우 이 소비자를 먼저 전환한다. 정본은 기존 `Assets/Settings/Shelter/BunkerRules.asset`, 주소는 `data/bunker-rules`다. [중복 검사와 이전 상태](../DataHistory/20261119-survivor-animation-plan.md)

Windows 번들로 불러온 값 50과 6인의 51·50·0 전환을 확인했다. [원화 반영 방법](../SurvivorAnimation.md) · [검증 기록](../DataHistory/20261119-survivor-animation-verified.md)

## 벙커 시계와 머리 위 멘탈 바

`DevGageUI`는 시뮬레이션의 `CurrentTick`과 `Rules.TotalTicks`에서 하루의 경과 비율을 계산한다. 표시할 분은 `floor(CurrentTick × 1440 / TotalTicks)`이며 마지막 틱은 24:00이다. `CharacterIdentity`는 생존자마다 `CurrentMental / Rules.MentalMaximum`을 머리 위 바에 표시한다. 두 비율은 씬 수명 동안 계산하며 게임 데이터에 저장하지 않는다. [배치·이미지·검수](../UI/DevGageUI.md)

## 개발 효율과 작업 멘탈

현재 SO 값과 실제 벙커 씬에서의 결과는 [작업 멘탈 검증 기록](../DataHistory/20261118-work-mental-02-verified.md)에 있다.

같은 `data.bunker.rules`에서 `percentPerAbility`로 개발 효율을, `mentalCostPerWork`로 작업자 한 명의 1회 비용을 조정한다. `mentalCostPerWork`는 필수 정수(멘탈 포인트/인·회), 0 이상, 초깃값은 임시 1이다. 0이면 작업으로 멘탈을 소모하지 않는다. 기존 `workEveryTicks`가 두 효과의 주기다.

`BunkerSimulation.ProcessWork`는 작업 시작 시점에 열린 미완료 분야에서 진행도를 만드는 각 작업자에게 `Rules.MentalCostPerWork`를 한 번 적용한다. 같은 틱에 분야를 완성한 작업자 모두가 그 회차 비용을 낸다. 이동·잠금·점거·탈진 중, 개발 효율이 0인 경우와 이미 100%인 분야는 작업에서 제외된다. 현재 멘탈은 `CharacterIdentity.SetMental`로 최소 0까지 차감하고 기존 기절·회복 규칙을 따른다. SO 값은 런타임에 수정하지 않는다. 필드를 이전하거나 제거할 때 시뮬레이션의 소비 지점과 설치 도구를 함께 갱신한다. [중복 조사와 변경 전 값](../DataHistory/20261118-work-mental-01-planned.md)

## 최종 엔딩 평가 기준

최종 점수는 제출한 세 분야의 진척 합을 세 분야의 최대 진척 합으로 나눈 값이다. 중간 발표는 `evaluationTargetProgress`와 `totalTicks`를 사용한다. `evaluationReferenceTicks`는 이전 데이터와의 호환을 위해 보관한다. [최종 점수 변경 이력](../DataHistory/20261124-ending-full-score.md)

## 2026-11-18 엔딩 평가식 도입 당시 값

연결·검증 완료: [엔딩 팝업 검증](../DataHistory/20261118-ending-popup-02-verified.md). 기존 Addressables 콘텐츠를 갱신하고 실제 최종120틱 평가/표시를 확인했다.

기존 `data.bunker.rules` SO/주소/수명에 필수 `evaluationTargetProgress:double`(진척 퍼센트포인트 합, 유한 >0, 사용자 확정 96), `evaluationReferenceTicks:int`(기준 틱 수, >0, 사용자 확정 24)을 추가한다. 암묵 기본값 없음. `BunkerRules.Validate` 검사. `BunkerSimulation`이 기존 로드된 Rules와 CurrentTick을 `GameDevScoreCalculator`에 제공한다. 평가 = 세 분야 진척 합 / (evaluationTargetProgress × CurrentTick / evaluationReferenceTicks), ×100 없음. 틱 0에서는 점수 보류, 종료 때 현재 틱 기준 확정. 기존 totalTicks/작업 속도/기절 틱은 변경하지 않는다. 상수 삭제·이전 시 계산 소비자를 함께 전환하며 새 SO/CSV/로더를 만들지 않는다. 중복 조사와 검증은 [계획](../DataHistory/20261118-ending-popup-01-planned.md) 및 후속 완료 기록 참조.

## 2026-11-18 좀비 전투용 전역 멘탈 확장

벙커의 캐릭터 정보 카드는 씬 시뮬레이션이 로드한 `Rules.MentalMaximum`을 멘탈 게이지 상한으로 사용한다. 현재 포인트는 선택한 생존자에게서 읽는다. [카드 검증](../DataHistory/20261118-shelter-character-card-verified.md)

당시 계약이며 현재 기절·각성은 위의 방 멘탈과 기절 구조를 따른다. 기존 정본/주소/GUID/주기를 유지. 필수 int 필드 `mentalMaximum`(멘탈 포인트, >0, 최초100), `faintDurationTicks`(기본 틱, >0, 최초40), `wakeMental`(멘탈 포인트, >0 및 상한 이하, 최초30)을 추가한다. 빈 값/암묵 기본값 없음. 근거는 노션 전역 상수의 멘탈 상한100/기절1시간=40틱/깨어남30. CharacterIdentity.ConfigureMental/SetMental/AdvanceMentalTick과 BunkerSimulation.ProcessTick에서 소비. 멘탈 0은 행동 차단, 양수 회복은 탈진 해제, 기절 기간이 끝나면 wakeMental로 복원. 새 게임은 Characters의 원래 초기 멘탈로 리셋. 수치 원본은 이 SO만 소유. 기존 이벤트의 멘탈 효과도 같은 SetMental 경계에서 상한을 적용한다. [계획](../DataHistory/20261118-zombie-invasion-01-planned.md), [좀비 명세](Zombies.md). 기존 검증 기록의 멘탈 상한 미정 설명은 이 계약으로 대체한다.

## 현재 추가 계약 — 기획 선행 조건 (2026-11-18)

사용자 확정은 **기획 50% 초과**다. 기존 SO에 `planningUnlockPercent:float` (필수, 기본값 없음, 유한한 0 이상 100 미만, 단위 퍼센트포인트, 현재 50)을 추가한다. 기존 정본/GUID/Addressables/캐시는 유지한다. `BunkerSimulation.ProcessWork`가 틱 시작의 기획 진행률과 비교해 아트/프로그래밍 작업만 잠근다. 기획 진행/방 이동/통과 및 명시적 이벤트 진척 효과는 막지 않는다. [확정 이력](../DataHistory/20261118-notion-sheets-04-planning-gate.md). 아래 임시 총 틱/작업 계수는 별도 미반영 항목으로 유지한다.

- ID `data.bunker.rules`, v1, active (밸런스 임시) / Codex bunker-ticks-20261118.
- 정본: `Assets/Settings/Shelter/BunkerRules.asset`, BunkerRules SO, GUID `01a9f70ed623c5341bec7f05cc07781d`. 외부 원본/CSV/생성 DB 없음.
- 주소 `data/bunker-rules`, 기존 Game Data Local 그룹. GameData.LoadSettingsAsync가 애플리케이션 캐시/해제 소유. 실패 시 진행 차단, 상수 폴백 없음.
- 소비: BunkerSimulation → BunkerTickClock → ShelterWalker.CurrentRoom / CharacterIdentity.Definition → GameDevManager.AddProgress.

| 필드 | 타입 | 의미/단위 | 필수·기본값·제약 |
|---|---|---|---|
| totalTicks | int | 벙커 게임 길이/틱 | 필수 >0, 현재 960 |
| secondsPerTick | float | 어려움의 틱 길이/게임 시간 초 | 필수 유한 >0, 현재 0.33 |
| randomEventEveryTicks | int | 랜덤 이벤트 알림 간격/틱 | 필수 >0, 현재 40. 발표와 종료 틱 제외 |
| invasionEveryTicks | int | 첫 습격 이후 침공 간격/틱 | 필수 >0, 현재 80 |
| workEveryTicks | int | 작업 간격/틱 | 필수 >0, 초기 임시 1 |
| percentPerAbility | float | 작업 1회 능력 1당 퍼센트포인트 | 필수 유한 >=0, 초기 임시 0.1 |

모든 필드는 Validate에서 검사한다. 첫 습격은 firstInvasionTick에, 일반 사건은 첫 정시에 발생한다. 마지막 틱의 작업 처리 후 FinishDevelopment를 호출한다. 재시작하면 시계와 발행 기록을 초기화한다. 일시정지와 Time.timeScale을 따른다. 세 분야가 모두 100%이거나 참여 생존자가 모두 탈진하면 남은 틱을 건너뛰고 즉시 진척을 제출한다. 같은 작업에서 두 조건이 성립하면 완성을 우선한다. 최종 평가는 마감 시점의 기존 점수 기준을 적용한다.

방 분류는 닫힌 기술 분류 `ShelterRoomType` (Common=0, Programming=1, Graphics=2, Planning=3, Sleeping=4, ZombieInvasion=5). 씬의 기존 ShelterArea.roomType이 방별 배치 정본이다. 캐릭터 능력을 복제하지 않는다. 시작 시 위치한 방/이동 도착 방에 소속되며 이동 지시가 수락되면 소속 해제. 복도·이동·사다리·비활성·닫힌 방은 작업 제외. 공용/수면/침공은 작업 없음. 여러 작업자는 합산; 분야별 100% 상한은 기존 GameDevProgress 사용.

단일 설정 인스턴스만 등록한다. 주소 변경/삭제 시 모든 소비자를 함께 전환하며 기존 주소 폐기 전 검색 및 로드 검증한다. 영구 저장 없음; 틱/진행도/방 소속은 런타임 상태이며 SO를 수정하지 않는다.

중복 조사: Assets, ProjectSettings, Docs의 tick/RoomType/작업 진행/secondsPerTick 검색. 기존 ShelterWalker.Tick은 이동 적분이고 FarmingSession.Tick은 파밍 초 카운트로 범위가 다르다. data.characters 능력과 기존 GameDevProgress/FinishDevelopment 재사용. 목록 planned 캐릭터 예약은 기존 구현 완료 내용 확인; 새 캐릭터 DB 생성 없음. 기존 벙커 시간 설정/방 종류 정본 없음. 적용 직전 목록 재확인 예정.

적용 직전 및 완료 시 목록 재확인: 동일 의미 예약/정본 충돌 없음. 현재 씬은 기획(Planning)·거실(Common)·수면(Sleeping), 아래 아트(Graphics)·프로그래밍(Programming), 위 전투 장소(ZombieInvasion)의 6개 방이다. 기존 ShelterArea와 enum을 재사용하며 SO 수치는 변경하지 않았다. 설치 도구는 사용자 방 타입/표시 이름과 설정 값을 보존한다. [배치 및 검증](../DataHistory/20261118-shelter-map-layout.md).

검증: Edit 9개, Play 21개 통과. Use Existing Build 모드의 실제 Addressables 콘텐츠 로드 및 실제 시간 Update 확인. Player 실행 파일/저장/의도적 로드 실패 복구/프레임 드롭 중 이동과 틱의 과거 위치 재구성은 미검증 또는 범위 밖. 틱 따라잡기 시 작업은 처리 시점의 방 소속을 사용한다. 최신 이력 [검증](../DataHistory/20261118-bunker-ticks-02-verified.md).

## 2026-11-18 사건 팝업 소비 연결

기존 randomEventEveryTicks 값(현재 10) 유지. `BunkerSimulation.RandomEventDue`를 `EventPopupController`가 구독해 Events CSV 사건을 표시한다. 마지막 틱은 팝업을 열지 않는다. `AcquirePause(owner)`/`ReleasePause(owner)`와 `IsPaused`로 로딩·읽기 중 틱을 정지하며 기존 수동 `Paused` 값을 보존한다. 능력/작업 수치 및 정본 변경 없음. [이력](../DataHistory/20261118-event-popup-04-verified.md).

## 2026-11-18 선택형 사건·다음 습격

사용자 확정: 모든 시간 단위는 틱, 기존 이벤트 간격 유지. 사건으로 멈춘 틱의 작업 이후 처리는 닫힌 뒤 한 번 이어간다. 같은 틱의 ZombieInvasionDue도 이때 발생하며 PendingWaveHpModifier를 CurrentWaveHpModifier로 1회 이동/초기화한다. GetCurrentWaveEnemyHp(baseHp)는 현재 습격 보정을 더해 최소 1을 반환한다. 적 기본 HP/전투는 아직 없고 외부 생성기가 이 API를 소비해야 한다. 보정은 런타임 상태, 재시작 시 0이며 SO 수치 변경 없음. 팝업 정지 중 캐릭터 Update 이동도 정지한다. [검증](../DataHistory/20261118-event-effects-02-verified.md).
## 캐릭터 허기와 음식 사용

편집 정본은 기존 `Assets/Settings/Shelter/BunkerRules.asset`, 주소는 `data/bunker-rules`다. 허기 눈금은 10칸이며 구조한 인물마다 가득 찬 상태에서 시작한다.

| 필드 | 타입 · 단위 | 필수 · 초기값 · 제약 |
|---|---|---|
| hungerEveryTicks | int · 벙커 틱 | 필수, 임시 5, 1 이상. 첫 감소는 해당 틱에 발생 |
| hungerLoss | int · 한 주기에 잃는 칸 | 필수, 임시 1, 0~10 |
| emptyHungerMentalLossMultiplier | int · 멘탈 감소 배율 | 필수, 초기 2, 1 이상. 생존자 개인의 허기가 0일 때 음수 변화량에 적용 |
| foodRestore | int · 한 입으로 채우는 칸 | 필수, 5, 1~10 |
| servingsPerCan | int · 새 통조림 한 개의 사용 횟수 | 필수, 4, 1 이상 |

허기와 남은 회분, 장착물은 플레이 상태다. 시뮬레이션은 틱의 허기를 먼저 갱신한다. CharacterIdentity는 감소 시점의 개인 허기가 0이면 작업·전투방·좀비 공격 및 침입·사건·발표의 멘탈 감소량에 배율을 한 번 적용한다. 허기가 1칸 이상이거나 회복 효과이면 기본량을 적용한다. 통조림을 먹은 직후부터 새 허기를 사용하며, 재시작은 초기 멘탈과 허기를 복원한다.

## 좀비 접근과 보초 배치

기존 벙커 시계와 전역 멘탈 규칙에 아래 필수 필드를 추가한다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이며 `BunkerSimulation.Rules`로 조회한다.

| 필드 | 타입 · 단위 | 초기값 · 제약 | 소비 |
|---|---|---|---|
| invasionApproachTicks | int · 선두가 막힘없이 생성 지점부터 입구까지 걷는 벙커 틱 | 6, 양수 | 공용 이동 속도 × 틱당 초 × 접근 틱으로 스폰 거리를 계산 |
| guardVolleyEveryTicks | int · 보초의 원거리 사격 간격(벙커 틱) | 2, 필수 양수 | `BunkerRules.GuardVolleyEveryTicks`를 통해 `ZombieAgent`가 도착 즉시 첫 공격을 처리하고 이후 공격을 예약한다. |
| doorHitLimit | int · 좀비 한 마리가 문 앞에서 받는 총 공격 횟수 | 3, 필수 양수 | 보초 전체가 공유한다. 마지막 피해 적용 후 생존하면 즉시 진입한다. |
| invasionMentalLoss | int · 침입 1마리당 1인 멘탈 | 2, 0 이상 | 실내 진입 시 구조 인원 전원에 1회 |
| guardMentalLossPerTick | int · 보초 1인당 멘탈 포인트/틱 | 2, 0 이상 | 전투방에 배치된 행동 가능한 인원별로 매 틱 차감 |

빈 값이나 암묵적 대체값을 사용하지 않는다. 기존 SO 주소·GUID와 로딩 수명을 유지한다. 개체의 남은 거리와 배치 시간은 해당 플레이에서만 사용한다. 기존 좀비 시트의 주사위 피해와 전원 침입 손실은 적용 대상과 시점이 다르다. [이전 접근 방식 검증](../DataHistory/20261119-zombie-volley-02-verified.md)
