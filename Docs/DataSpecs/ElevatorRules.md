# 엘리베이터 공용 규칙

- ID `data.shelter.elevators`, v1, 2026-11-18, active (밸런스 임시) / Codex shelter-elevators-20261118.
- 담당: 사람/좀비가 공동 사용하는 차량 이동 속도, 승하차 지점 및 호출 대기 간격. 검색 별칭 elevator, lift, 엘리베이터, 승강기, 호출 주도권.
- 제외: 개인 수평 이동 속도, 전투 수치, 층/방 배치, 런타임 소유자/대기열. 정차 층은 씬 ShelterElevator 높이 범위와 ShelterArea 바닥을 자동 연결.
- 편집 정본 `Assets/Settings/Shelter/ElevatorRules.asset`, 타입 ElevatorRules. CSV/외부 편집 원본 없음. 새 Addressables `data/elevators`, 기존 Game Data Local, 라벨 없음, GUID 6facc373492cc584ca04ebc3968d1007.
- 로더 GameData.LoadSettingsAsync<ElevatorRules> → ShelterTransit → ShelterElevator. 공용 앱 수명 캐시/핸들 사용. 로딩 동안 기존 BunkerSimulation pause와 Commander input block 사용. 실패 시 오류 표시, 임의 수치 대입 없음.

| 필드 | 타입 | 의미·단위 | 필수/기본/제약 |
|---|---|---|---|
| travelSpeed | float | 빈 차/탑승 차량 속도, world units/sec | 필수 유한 >0, 최초 임시3 |
| queueSpacing | float | 같은 층·같은 방향 호출 대기 간격, world units | 필수 유한 >0, 최초 임시2.4 |
| boardingOffset | float | 샤프트 중심과 호출 지점의 수평 거리, world units | 필수 유한 >0, 최초 임시2.4 |
| exitOffset | float | 샤프트 중심과 하차 지점의 수평 거리, world units | 필수 유한 >0 및 boardingOffset 미만, 최초 임시1.2 |

모든 필드는 ElevatorRules.Validate에서 검사. 값은 Inspector에서 편집하고 기존 Addressables 콘텐츠 빌드 갱신. 단일 규칙 인스턴스이며 카탈로그/행 ID 없음. 주소/에셋 이전 전 소비자 전환과 콘텐츠 검증 필요. 영구 저장 없음. 새 게임/씬 종료/비활성 시 호출 소유권과 대기열 정리.

## 주도권 계약

1. 다음 엘리베이터가 포함된 이동 명령이 들어오면 그 엘리베이터에 1회 등록. 같은 요청 반복으로 순서를 바꾸지 않는다.
2. 먼저 등록한 유효 요청자가 Owner가 된다. 차가 호출층에 정차하고 Owner가 호출 지점에 오면 탑승한다. 출발 전 같은 층의 좌우 호출 지점이나 바로 뒤 대기 칸에 도착한 호출자가 있으면 접수순으로 한 명 더 태운다. 대기자가 한 명이면 바로 출발한다. 같은 층 통행자는 호출 없이 걸어간다.
3. 탑승자 두 명의 목적층을 각각 처리한다. 첫 호출자의 경로 중간에 동승자의 목적층이 있으면 먼저 정차한다. 반대 방향이면 첫 호출자의 목적층을 먼저 방문한다. 모두 내린 뒤 남은 호출을 접수순으로 처리한다.
4. 탑승 중 새 이동 명령은 하차 후 적용한다. 정지도 현재 운행을 안전한 도착층까지 끝낸 뒤 적용한다. 사다리용 팔/다리 모션은 제거하고 차 내부에 인물이 서 있는 상태로 운반한다. 문 애니메이션 없음.
5. 대기자의 취소/제거/탈진/전투 잠금은 큐에서 제거. 탑승자의 탈진은 하차까지 운행 후 행동 제한. 차량 비활성/제거 시 탑승자를 연결된 안전한 층으로 내려놓고 잠금 해제. 팝업 일시정지 중 차량·사람·좀비 모두 정지.
6. 사람과 좀비는 같은 탑승 순서를 따른다. 좀비의 하차 지점이 막혔으면 같은 층에서 걸어서 연결된 가장 가까운 빈자리에 내린다. 빈자리는 기존 `data.zombies.minimumSpacing`으로 판정한다. 지상에서 엘리베이터를 기다리는 좀비는 서로 간격을 유지하고, 같은 층으로 이동 중인 좀비는 대기자를 통과한다. 탑승자의 개별 HP와 목적지를 유지하며 하차 후 다시 교전할 수 있다.

## 중복 조사와 이전

DataRegistry planned 및 Assets/ProjectSettings/Docs에서 elevator/lift/엘리베이터/속도/대기열 검색. 기존 차량 정본은 위 ElevatorRules 에셋 하나다. data.zombies의 climbSpeed는 개인 사다리 속도로 썼던 시트 호환 필드다. queueSpacing은 승강기 호출 대기 위치를, minimumSpacing은 지상 좀비 간격을 정한다. 두 명 동승은 ShelterElevator의 운행 규칙이다.

배치 정본은 기존 ShelterPrototype 씬, 새 Elevator.prefab은 배치 템플릿. 기존 사다리 객체는 비활성 보존하고 활성 경로/생성 메뉴는 Elevator로 이전. 역사적 사다리 빌더는 Legacy 메뉴로 구분. [계획](../DataHistory/20261118-shelter-elevators-01-planned.md). 컴파일·Addressables 콘텐츠 빌드·Edit/Play·실제 Update 검증 완료. [검증 이력](../DataHistory/20261118-shelter-elevators-02-verified.md).

