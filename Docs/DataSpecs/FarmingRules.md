# FarmingRules 명세

- 데이터 ID: `data.farming.rules`; 상태: active; 담당: 파밍; 문서 스키마 v2; 확인일: 2026-11-18.
- 소유 의미: 한 파밍 세션의 제한시간·운반 용량·이동/상호작용/연출 규칙. 개체별 운반 비용은 [FarmingData](FarmingData.md)가 소유한다.
- 정본: [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset), 타입 `ShelterRun.FarmingRules`. CSV 원본 없음.
- GUID: `22168f09c24cbfb49aeb55b136dd0298`; 주소 `data/farming/rules`; 그룹 `Farming Local`; 라벨 `farming`.
- 기존 SO를 Unity Inspector/Editor API로 편집하고 변경 전후 이력을 기록한 뒤 `Tools > Farming > Build Data Content`로 콘텐츠를 갱신한다. 생성 도구의 초기값을 편집 기준으로 삼지 않는다.
- 조회/수명: `FarmingDataLoader.LoadAsync()` → `Rules`, 씬 소유·종료 시 해제. Validate 실패 시 파밍 시작 차단.

## 필드 계약과 현재 값

모든 필드는 필수다. 아래 값은 현재 에셋 스냅샷이며 누락 시 적용하는 기본값이 아니다. float는 모두 유한한 양수여야 한다.

| 필드 | 타입 | 의미/단위 | 현재 값 | 추가 제약 / 소비자 |
|---|---|---|---|---|
| duration | float | 제한시간 / 초 | 60 | FarmingSession.Remaining 초기화, HUD 시간 비율 |
| capacity | int | 최대 운반량 / 칸 | 4 | 1 이상; FarmingInventory 생성, HUD 슬롯 |
| moveSpeed | float | 플레이어 이동속도 / Unity 거리 단위·초⁻¹ | 5.8 | FarmingPlayer 이동 |
| interactionRadius | float | 대상 탐색 반경 / Unity 거리 단위 | 2 | FarmingPlayer 근접 판정 |
| dropDistance | float | 내려놓을 목표 거리 / Unity 거리 단위 | 1.6 | FarmingPlayer 전방 배치/충돌 검사 |
| throwDuration | float | 던지기 시간 / 초 | 0.45 | FarmingPickup 이동 보간 |
| fadeDuration | float | 투입 후 사라지는 시간 / 초 | 0.5 | FarmingPickup 투명도 보간 |
| throwArc | float | 던지기 최고 추가 높이 / Unity 거리 단위 | 1.5 | FarmingPickup 포물선 형태 연출 |
| warningSeconds | float | 남은 시간 경고 임계값 / 초 | 10 | duration 이하; FarmingHud 색 변경 |
| mapHalfExtents | Vector2 | 기본 호실 설계 반크기 / 논리 격자 단위 | (11, 8) | 두 축 양수; FarmingIsoSceneBuilder의 호실 크기. 실제 이동 경계는 씬 Tilemap/Collider2D |
| roomRevealDuration | float | 입퇴실 시 벽·천장·실내 페이드 시간 / 초 | 0.5 | 필수, 유한한 양수; FarmingMap2D → FarmingRoom2D |

검사 구현은 `FarmingRules.Validate`. 2D 전환 후 이동/상호작용/투하 거리와 throwArc는 XY 월드 단위다. mapHalfExtents는 기본 호실 설계용이며 플레이 중 경계를 중복 저장하지 않는다. Inspector 직렬화로 0이 들어와도 유효 기본값으로 간주하지 않는다. [2D 이전 계획](../DataHistory/20261118-farming-map2d-01-planned.md).

단일 설정 에셋이며 행 ID/외래키/활성화 목록은 없다. 구간별 규칙이 필요해지면 먼저 적용 범위와 프로필 선택 계약을 설계한다. 단순한 씬 추가나 난이도 조정 때문에 같은 주소/역할의 Rules 복제본을 만들지 않는다. 삭제/주소 변경은 모든 소비자 전환이 포함된 이전 작업으로 처리한다.

## 중복 조사와 검증

- 확인: FarmingRules 필드/Validate, FarmingDataLoader, Session, Player, Inventory, Pickup, Hud 및 CSV.
- `capacity`와 `FarmingData.weight`는 같은 칸 단위를 쓰지만 전체 한도와 개체 비용으로 구분한다. HUD/인벤토리에는 이 값을 참조해 전달한다.
- 현재 Remaining, Inventory.Used, 투입량은 런타임 상태이며 SO에 저장하지 않는다.
- 2D 전환: [적용 이력](../DataHistory/20261118-farming-map2d-02-applied.md), [배치/타일 계약](../Farming/Map2D.md). 방 노출 진행률은 런타임 상태다. 기존 ID/GUID/주소와 로더의 씬 단위 수명을 유지한다.
- 최종 검증: [방향·소팅·60초 흐름](../DataHistory/20261118-farming-map2d-04-verified.md). 69개 플레이 검사 및 재시작/표시 앵커 검증 통과. 제작 도구도 잘못된 Rules 값을 임의 기본값으로 덮어쓰지 않고 Validate로 실패시킨다.
- v1 기능 검증: [이전 파밍 검증 스냅샷](../DataHistory/20261118-143045-3883526-verified.md). v2의 roomRevealDuration 추가와 2D 소비 이전은 위 적용 이력을 기준으로 한다.
