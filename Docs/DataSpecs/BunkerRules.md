# 벙커 틱·작업 규칙

## 2026-11-18 엔딩 평가식 확장

연결·검증 완료: [엔딩 팝업 검증](../DataHistory/20261118-ending-popup-02-verified.md). 기존 Addressables 콘텐츠를 갱신하고 실제 최종120틱 평가/표시를 확인했다.

기존 `data.bunker.rules` SO/주소/수명에 필수 `evaluationTargetProgress:double`(진척 퍼센트포인트 합, 유한 >0, 사용자 확정 96), `evaluationReferenceTicks:int`(기준 틱 수, >0, 사용자 확정 24)을 추가한다. 암묵 기본값 없음. `BunkerRules.Validate` 검사. `BunkerSimulation`이 기존 로드된 Rules와 CurrentTick을 `GameDevScoreCalculator`에 제공한다. 평가 = 세 분야 진척 합 / (evaluationTargetProgress × CurrentTick / evaluationReferenceTicks), ×100 없음. 틱 0에서는 점수 보류, 종료 때 현재 틱 기준 확정. 기존 totalTicks/작업 속도/기절 틱은 변경하지 않는다. 상수 삭제·이전 시 계산 소비자를 함께 전환하며 새 SO/CSV/로더를 만들지 않는다. 중복 조사와 검증은 [계획](../DataHistory/20261118-ending-popup-01-planned.md) 및 후속 완료 기록 참조.

## 2026-11-18 좀비 전투용 전역 멘탈 확장

기존 정본/주소/GUID/주기를 유지. 필수 int 필드 `mentalMaximum`(멘탈 포인트, >0, 최초100), `faintDurationTicks`(기본 틱, >0, 최초40), `wakeMental`(멘탈 포인트, >0 및 상한 이하, 최초30)을 추가한다. 빈 값/암묵 기본값 없음. 근거는 노션 전역 상수의 멘탈 상한100/기절1시간=40틱/깨어남30. CharacterIdentity.ConfigureMental/SetMental/AdvanceMentalTick과 BunkerSimulation.ProcessTick에서 소비. 멘탈 0은 행동 차단, 양수 회복은 탈진 해제, 기절 기간이 끝나면 wakeMental로 복원. 새 게임은 Characters의 원래 초기 멘탈로 리셋. 수치 원본은 이 SO만 소유. 기존 이벤트의 멘탈 효과도 같은 SetMental 경계에서 상한을 적용한다. [계획](../DataHistory/20261118-zombie-invasion-01-planned.md), [좀비 명세](Zombies.md). 기존 검증 기록의 멘탈 상한 미정 설명은 이 계약으로 대체한다.

## 현재 추가 계약 — 기획 선행 조건 (2026-11-18)

사용자 확정은 **기획 50% 초과**다. 기존 SO에 `planningUnlockPercent:float` (필수, 기본값 없음, 유한한 0 이상 100 미만, 단위 퍼센트포인트, 현재 50)을 추가한다. 기존 정본/GUID/Addressables/캐시는 유지한다. `BunkerSimulation.ProcessWork`가 틱 시작의 기획 진행률과 비교해 아트/프로그래밍 작업만 잠근다. 기획 진행/방 이동/통과 및 명시적 이벤트 진척 효과는 막지 않는다. [확정 이력](../DataHistory/20261118-notion-sheets-04-planning-gate.md). 아래 임시 총 틱/작업 계수는 별도 미반영 항목으로 유지한다.

- ID `data.bunker.rules`, v1, active (밸런스 임시) / Codex bunker-ticks-20261118.
- 정본: `Assets/Settings/Shelter/BunkerRules.asset`, BunkerRules SO, GUID `01a9f70ed623c5341bec7f05cc07781d`. 외부 원본/CSV/생성 DB 없음.
- 주소 `data/bunker-rules`, 기존 Game Data Local 그룹. GameData.LoadSettingsAsync가 애플리케이션 캐시/해제 소유. 실패 시 진행 차단, 상수 폴백 없음.
- 소비: BunkerSimulation → BunkerTickClock → ShelterWalker.CurrentRoom / CharacterIdentity.Definition → GameDevManager.AddProgress.

| 필드 | 타입 | 의미/단위 | 필수·기본값·제약 |
|---|---|---|---|
| totalTicks | int | 벙커 게임 길이/틱 | 필수 >0, 초기 임시 120 |
| secondsPerTick | float | 틱 길이/게임 시간 초 | 필수 유한 >0, 초기 임시 1 |
| randomEventEveryTicks | int | 랜덤 이벤트 알림 간격/틱 | 필수 >0, 초기 임시 10 |
| invasionEveryTicks | int | 침공 알림 간격/틱 | 필수 >0, 초기 임시 30 |
| workEveryTicks | int | 작업 간격/틱 | 필수 >0, 초기 임시 1 |
| percentPerAbility | float | 작업 1회 능력 1당 퍼센트포인트 | 필수 유한 >=0, 초기 임시 0.1 |

모든 필드는 Validate에서 검사한다. 초기값은 플레이 확인용이며 확정 밸런스는 미정. 첫 발생은 간격에 도달한 틱, 틱 0에는 발생 없음. 마지막 틱의 작업/알림 처리 후 FinishDevelopment 호출. 완료된 개발도 재시작 시 틱 0으로 초기화한다. 일시정지와 Time.timeScale을 따른다. 세 분야 100%는 시간 종료를 앞당기지 않는다.

방 분류는 닫힌 기술 분류 `ShelterRoomType` (Common=0, Programming=1, Graphics=2, Planning=3, Sleeping=4, ZombieInvasion=5). 씬의 기존 ShelterArea.roomType이 방별 배치 정본이다. 캐릭터 능력을 복제하지 않는다. 시작 시 위치한 방/이동 도착 방에 소속되며 이동 지시가 수락되면 소속 해제. 복도·이동·사다리·비활성·닫힌 방은 작업 제외. 공용/수면/침공은 작업 없음. 여러 작업자는 합산; 분야별 100% 상한은 기존 GameDevProgress 사용.

단일 설정 인스턴스만 등록한다. 주소 변경/삭제 시 모든 소비자를 함께 전환하며 기존 주소 폐기 전 검색 및 로드 검증한다. 영구 저장 없음; 틱/진행도/방 소속은 런타임 상태이며 SO를 수정하지 않는다.

중복 조사: Assets, ProjectSettings, Docs의 tick/RoomType/작업 진행/secondsPerTick 검색. 기존 ShelterWalker.Tick은 이동 적분이고 FarmingSession.Tick은 파밍 초 카운트로 범위가 다르다. data.characters 능력과 기존 GameDevProgress/FinishDevelopment 재사용. 목록 planned 캐릭터 예약은 기존 구현 완료 내용 확인; 새 캐릭터 DB 생성 없음. 기존 벙커 시간 설정/방 종류 정본 없음. 적용 직전 목록 재확인 예정.

적용 직전 및 완료 시 목록 재확인: 동일 의미 예약/정본 충돌 없음. 현재 씬은 기획(Planning)·거실(Common)·수면(Sleeping), 아래 아트(Graphics)·프로그래밍(Programming), 위 전투 장소(ZombieInvasion)의 6개 방이다. 기존 ShelterArea와 enum을 재사용하며 SO 수치는 변경하지 않았다. 설치 도구는 사용자 방 타입/표시 이름과 설정 값을 보존한다. [배치 및 검증](../DataHistory/20261118-shelter-map-layout.md).

검증: Edit 9개, Play 21개 통과. Use Existing Build 모드의 실제 Addressables 콘텐츠 로드 및 실제 시간 Update 확인. Player 실행 파일/저장/의도적 로드 실패 복구/프레임 드롭 중 이동과 틱의 과거 위치 재구성은 미검증 또는 범위 밖. 틱 따라잡기 시 작업은 처리 시점의 방 소속을 사용한다. 최신 이력 [검증](../DataHistory/20261118-bunker-ticks-02-verified.md).

## 2026-11-18 사건 팝업 소비 연결

기존 randomEventEveryTicks 값(현재 10) 유지. `BunkerSimulation.RandomEventDue`를 `EventPopupController`가 구독해 Events CSV 사건을 표시한다. 마지막 틱은 팝업을 열지 않는다. `AcquirePause(owner)`/`ReleasePause(owner)`와 `IsPaused`로 로딩·읽기 중 틱을 정지하며 기존 수동 `Paused` 값을 보존한다. 능력/작업 수치 및 정본 변경 없음. [이력](../DataHistory/20261118-event-popup-04-verified.md).

## 2026-11-18 선택형 사건·다음 습격

사용자 확정: 모든 시간 단위는 틱, 기존 이벤트 간격 유지. 사건으로 멈춘 틱의 작업 이후 처리는 닫힌 뒤 한 번 이어간다. 같은 틱의 ZombieInvasionDue도 이때 발생하며 PendingWaveHpModifier를 CurrentWaveHpModifier로 1회 이동/초기화한다. GetCurrentWaveEnemyHp(baseHp)는 현재 습격 보정을 더해 최소 1을 반환한다. 적 기본 HP/전투는 아직 없고 외부 생성기가 이 API를 소비해야 한다. 보정은 런타임 상태, 재시작 시 0이며 SO 수치 변경 없음. 팝업 정지 중 캐릭터 Update 이동도 정지한다. [검증](../DataHistory/20261118-event-effects-02-verified.md).
