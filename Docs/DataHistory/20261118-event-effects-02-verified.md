# 선택형 이벤트 효과 구현 결과

2026-11-18 / Codex event-effects-20261118.

사용자 후속 확정: 모든 게임 시간은 틱 단위이며 정시는 설명 표현이다. 기존 10틱 간격을 유지하고 별도 시계/시간 환산을 추가하지 않았다. 결과창의 자동 닫힘은 게임 진행과 무관한 UI 연출 시간이다.

## 데이터와 정본

| ID | 정본/생성물 | 변경 및 명세 |
|---|---|---|
| data.events | Assets/Settings/Data/Csv/Events.csv | 임시 3행→사용자 12행, int→ev_* string ID, Yes/No 효과 및 조건 추가. [명세](../DataSpecs/Events.md) |
| data.shelter.items | Assets/Settings/Data/Csv/ShelterItems.csv | choco/monster/bacchus ID·표시명, 실제 수량은 런타임만. [명세](../DataSpecs/ShelterItems.md) |
| data.characters | Assets/Settings/Data/Csv/Characters.csv | 정본 수치 변경 없음. CurrentMental 소비·재시작 초기화. [명세](../DataSpecs/Characters.md) |
| data.bunker.rules | Assets/Settings/Shelter/BunkerRules.asset | 정본 수치 변경 없음. 같은 틱의 사건→습격 처리 및 런타임 HP 보정. [명세](../DataSpecs/BunkerRules.md) |
| system.game-database | Assets/Settings/Data/GameDatabase.asset (생성물) | 기존 공용 DB/로더/GUID/주소 유지, Events v2 및 ShelterItems 포함. [명세](../DataSpecs/GameDatabase.md) |

정본 CSV는 UTF-8 BOM 포함. 스키마 이전 미리보기에서 제거 대상은 Events.id:int와 임시 ID 1~3뿐임을 확인한 뒤 적용. 다른 테이블·행 삭제 없음. 이전 이력 보존. DataRegistry planned와 Assets/ProjectSettings/Docs 별칭·소비 코드를 재확인했으며 동일 의미 정본 중복 없음. 기존 FarmingData/ExampleItems를 벙커 소비 재고로 오용하지 않았다.

## 동작

- EventEffect가 제한된 문법을 파싱하고 모든 아이템 참조를 검증. NONE 혼합·누락 효과·잘못된 대상/부호/명령은 거부.
- EventEffectExecutor는 전체 선택을 사전 계산한 뒤 반영. 재고 부족/수치 범위 초과 시 일부 효과만 적용하지 않음. 멘탈 0 하한, 진행도 0~100%p. open=선택 시점 미완료 축, 방 효과=현재 방 소속 활성 캐릭터.
- 팝업은 타이핑 → Yes/No → 실제 결과 한 줄 → 자동 닫기(기본 3초) 또는 계속. 결과 동안 틱/캐릭터 이동/새 월드 명령 정지. 중복 선택 금지. 재시작/종료/비활성 시 정리.
- ev_party는 choco>=1일 때만 추첨/직접 표시 가능하며 선택 직전에도 재검사. 직전 사건은 추첨 제외. 후보 0건이면 건너뛰어 연속 금지 보존.
- BunkerSimulation은 같은 틱에 발생한 습격을 팝업 종료 후 처리. PendingWaveHpModifier를 다음 습격에서 1회 CurrentWaveHpModifier로 이동. 실제 적 생성·전투는 현재 없으므로 GetCurrentWaveEnemyHp(baseHp) 계약까지 제공하며 실제 적 HP 적용 검증을 주장하지 않는다.

## 검증

- Unity 6000.3.28f1 최종 스크립트 컴파일 오류/경고 0, Addressables 콘텐츠 빌드 및 실제 Play 로드 성공.
- [Edit 검사](../Events/Verification-effects-edit.txt): 12건·조건·BOM·잘못된 문법·NONE 혼합 거부.
- [Play 검사](../Events/Verification-effects-play.txt): 사용자 표의 12건×Yes/No 24가지 예상값을 멘탈 합계·축별 진척·각 물자·HP 보정까지 대조. 버튼 onClick 경로, 중복 선택, 결과 자동 닫기, 재고 조건, 원자적 거부, 범위 제한, 연속 금지, 틱 간격, 같은 틱 습격 지연, 1회 소비, 재시작 검사 포함.
- [추가 경계 검사](../Events/Verification-effects-edge.txt): 없는 아이템 참조, 빈 방, 후보 0건, 선택 직전 조건 변경, 틱 후반부 중복 처리 방지, 외부 정지 보존, 마지막 틱 종료.
- 실제 Game View [선택 화면](../Events/Preview-choices.png), [결과 화면](../Events/Preview-result.png), [4:3](../Events/Preview-choices-4x3.png) 시각 검수. 임시 테스트 해상도 제거·원래 설정 복원. 런타임 결과 표시 시간 테스트 오버라이드는 Play 종료로 폐기.
- 기존 물자/캐릭터/설정 SO 원본은 효과 적용 중 변경하지 않음. 신규 Console 오류 없음.

## 제한

Player 실행 파일, 실제 적 생성/전투, 영구 저장, 파밍 물자와 소비 물자 간 매핑, 의도적인 Addressables 실패 복구는 범위 밖. 직접 벙커 실행은 기존 계약상 구조된 사람이 없으므로 멘탈 대상도 없으며, Play 검사는 파밍 전달 명단의 실제 3명으로 수행했다. 물자는 게임 시작 시 0개이며 이벤트로 획득한다. 원고·효과 값은 사용자가 제공한 예시를 그대로 초기 데이터로 사용했다.
