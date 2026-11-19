# FarmingRules 명세

- 데이터 ID: `data.farming.rules`; 상태: active; 담당: 파밍; 문서 스키마 v5; 확인일: 2026-11-19.
- 소유 의미: 한 파밍 세션의 제한시간·운반 용량·이동/상호작용/연출 규칙. 개체별 운반 비용은 [FarmingData](FarmingData.md)가 소유한다.
- 정본: [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset), 타입 `ShelterRun.FarmingRules`. CSV 원본 없음.
- GUID: `22168f09c24cbfb49aeb55b136dd0298`; 주소 `data/farming/rules`; 그룹 `Farming Local`; 라벨 `farming`.
- 기존 SO를 Unity Inspector/Editor API로 편집하고 변경 전후 이력을 기록한 뒤 `Tools > Farming > Build Data Content`로 콘텐츠를 갱신한다. 생성 도구의 초기값을 편집 기준으로 삼지 않는다.
- 조회/수명: `FarmingDataLoader.LoadAsync()` → `Rules`, 씬 소유·종료 시 해제. Validate 실패 시 파밍 시작 차단.

## 필드 계약과 현재 값

모든 필드는 필수다. 아래 값은 현재 에셋 스냅샷이며 누락 시 적용하는 기본값이 아니다. float는 모두 유한한 양수여야 한다.

| 필드 | 타입 | 의미/단위 | 현재 값 | 추가 제약 / 소비자 |
|---|---|---|---|---|
| duration | float | 제한시간 / 초 | 30 | FarmingSession.Remaining 초기화, HUD 제목·남은 시간·게이지 |
| preparationDuration | float | 시작 입력 후 사전 탐색 시간 / 초 | 15 | 양의 유한값. FarmingSession.PreparationRemaining과 HUD 카운트다운에 사용한다. 이 시간 동안 위치 이동과 문 출입이 가능하며, 종료 시 같은 위치에서 구조를 시작한다. |
| capacity | int | 최대 운반량 / 칸 | 4 | 1 이상; FarmingInventory 생성, HUD 슬롯 |
| moveSpeed | float | 플레이어 수평 이동속도 / Unity 거리 단위·초⁻¹ | 5.8 | FarmingPlayer A/D 이동 |
| interactionRadius | float | 대상 탐색 반경 / Unity 거리 단위 | 2 | 같은 공간에서 FarmingPlayer 대상 및 문 근접 판정 |
| dropDistance | float | 내려놓을 목표 거리 / Unity 거리 단위 | 1.6 | FarmingPlayer 전방 배치/충돌 검사 |
| throwDuration | float | 던지기 시간 / 초 | 0.45 | FarmingPickup 이동 보간 |
| fadeDuration | float | 투입 후 사라지는 시간 / 초 | 0.5 | FarmingPickup 투명도 보간 |
| throwArc | float | 던지기 최고 추가 높이 / Unity 거리 단위 | 1.5 | FarmingPickup 포물선 형태 연출 |
| warningSeconds | float | 남은 시간 경고 임계값 / 초 | 10 | duration 이하; FarmingHud 색 변경 |
| mapHalfExtents | Vector2 | 기본 호실 설계 반크기 / Unity 거리 단위 | (11, 8) | x는 FarmingSideViewBuilder의 방 너비 설계에 사용. y는 이전 2축 설계 호환용 보존값이며 사이드뷰에서 미사용. 실제 경계는 씬의 Walk left/right |
| roomRevealDuration | float | 입실 시 해당 공간 노출 페이드 시간 / 초 | 0.5 | 필수, 유한한 양수; FarmingMap2D → FarmingRoom2D |
| startingPersonId | string | 캐릭터 선택 없이 파밍 씬을 직접 실행할 때의 시작 인물 ID | person_01 | 필수. FarmingData의 Person 행을 참조한다. 타이틀에서 시작하면 선택한 회차 ID로 외형·방 배치·기본 확보 명단을 결정한다. |
| roomSpawns[].people | int | 해당 방에 배정할 고유 인원 / 명 | 가까운 순 2, 2, 1 | 필수, 기본값 없음, 0 이상. 합이 플레이어를 제외한 파밍 Person 행 수와 일치하고 방별 책상 수 이내 |
| roomSpawns[].legendWeight | int | 레전드 물자 하나를 배치할 때 해당 방의 추첨 가중치 / 상대값 | 가까운 순 1, 2, 3 | 필수, 기본값 없음. 양수이고 거리가 멀어질수록 증가. 합은 int 범위 |

배열은 복도에서 벙커 투입 지점과 방문 사이의 수평 거리순이다. 방마다 한 행이 필요하다. 파밍 회차 시작 시 `FarmingSpawnLayout`이 통조림 한 개씩의 자리를 먼저 확보하고, 레전드를 하나씩 별도 추첨한다. 빈 책상 자리가 있는 방만 가중치 합에 포함한다. 방 안의 빈 책상 자리는 균등 추첨한다. 나머지 물자는 전체 빈 책상 자리에서 균등 추첨하고, 사람은 각 책상 중앙 앞의 빈 바닥 자리를 사용한다. 배정 결과의 수명은 현재 씬 한 회차다.

검사 구현은 `FarmingRules.Validate`. 사이드뷰의 이동/근접/투하 거리는 수평 월드 단위, throwArc는 수직 시각 높이다. mapHalfExtents는 기본 호실 설계용이며 플레이 중 경계를 중복 저장하지 않는다. Inspector 직렬화로 0이 들어와도 유효 기본값으로 간주하지 않는다. 값·스키마·GUID·주소 변경 없이 소비만 전환했다. [사이드뷰 전환 계획](../DataHistory/20261118-farming-sideview-01-planned.md).

단일 설정 에셋이다. 시작 인물의 FarmingData ID를 삭제·변경할 때는 시작 인물 참조와 방별 배정 가능 인원도 함께 갱신한다. 구간별 규칙이 필요해지면 먼저 적용 범위와 프로필 선택 계약을 설계한다. 단순한 씬 추가나 난이도 조정 때문에 같은 주소/역할의 Rules 복제본을 만들지 않는다. 삭제/주소 변경은 모든 소비자 전환이 포함된 이전 작업으로 처리한다.

## 중복 조사와 검증

- 준비 단계의 런타임·HUD 연결과 컴파일 범위는 [준비 단계 변경 이력](../DataHistory/20261119-farming-preparation-code.md)에 기록했다.

- 플레이어 외형·방 배치·벙커 기본 확보 명단은 같은 회차의 `FarmingSession.StartingPersonId`를 참조한다. [캐릭터 선택](../UI/Title.md)

- 이전 3·2·1 배치 검증에서 3,000회 모두 DB 행별 수량과 방별 인원이 맞았다. 두 레전드의 방 배정 6,000회와 실제 회차 재시작을 확인했다. [결과와 편집 위치](../DataHistory/20261119-farming-spawns-02-verified.md)

- 확인: FarmingRules 필드/Validate, FarmingDataLoader, Session, Player, Inventory, Pickup, Hud 및 CSV.
- `capacity`와 `FarmingData.weight`는 같은 칸 단위를 쓰지만 전체 한도와 개체 비용으로 구분한다. HUD/인벤토리에는 이 값을 참조해 전달한다.
- 현재 Remaining, Inventory.Used, 투입량은 런타임 상태이며 SO에 저장하지 않는다.
- 2D 전환: [적용 이력](../DataHistory/20261118-farming-map2d-02-applied.md), [배치/타일 계약](../Farming/Map2D.md). 방 노출 진행률은 런타임 상태다. 기존 ID/GUID/주소와 로더의 씬 단위 수명을 유지한다.
- 사이드뷰 전환 당시 검증: [92개 Play 검사·당시 60초·재시작](../DataHistory/20261118-farming-sideview-02-verified.md). 제작 도구는 잘못된 Rules 값을 임의 기본값으로 덮어쓰지 않고 Validate로 실패시킨다.
- 이전 아이소메트릭 검증: [방향·소팅·60초 흐름](../DataHistory/20261118-farming-map2d-04-verified.md). 현재 맵 이전의 기록이다.
- v1 기능 검증: [이전 파밍 검증 스냅샷](../DataHistory/20261118-143045-3883526-verified.md). v2의 roomRevealDuration 추가와 2D 소비 이전은 위 적용 이력을 기준으로 한다.
