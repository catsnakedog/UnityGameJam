# 벙커 맵 배치

2026-11-18 사용자 스케치 반영. 편집 정본은 `Assets/Scenes/ShelterPrototype.unity`의 ShelterArea/Transform/ShelterElevator다. 방 배치는 유지하고 후속 이동 변경으로 엘리베이터를 설치했다. 차량 규칙은 별도 [ElevatorRules](DataSpecs/ElevatorRules.md)를 사용한다.

```text
                 전투 장소
                     │
                   엘리베이터
                     │
기획 ─────┬──────── 거실 ───── 수면
          │
        엘리베이터
          │
아트 ─────┴────── 프로그래밍
```

좌표는 방 바닥 중앙 기준이다. 모든 방은 기존 폭 8.65, 높이 3.8을 유지한다.

| 방 | X | 바닥 Y | 기존 분류 |
|---|---:|---:|---|
| 기획 | -12.7 | 7.5 | Planning |
| 거실 | 0 | 7.5 | Common |
| 수면 | 12.7 | 7.5 | Sleeping |
| 아트 | -11.75 | 0 | Graphics |
| 프로그래밍 | 2.2 | 0 | Programming |
| 전투 장소 | -4.5 | 17.5 | ZombieInvasion |

- 아래 엘리베이터: X=-5.8, Y=0 → 7.5. 위 끝은 기획–거실 통로, 아래 끝은 아트–프로그래밍 통로에 연결된다.
- 위 엘리베이터: X=-1.5, Y=7.5 → 17.5. 거실과 전투 장소를 연결한다.
- 기존 통로 3개에 엘리베이터 승강장 4개를 추가했다. 두 사다리 객체는 비활성 보존했고 같은 위치에 Elevator 프리팹을 배치했다. 승강장 폭은 6이며 호출/하차 지점을 덮는다.
- 구조된 캐릭터의 기존 spawnPoints 6개를 거실로 옮겼다. Farming에서 구조한 명단만 생성한다.
- 이전 ENTRY/ATTIC/REST AREA는 `99 - Previous layout (inactive)` 아래 비활성 보존했다. 기존 사각 건물 외곽도 비활성화했다.
- 한글 이름은 기존 NotoSansCJKkr 폰트를 사용한다. 카메라는 후속 [자유 이동 카메라 작업](ShelterCamera.md)에서 부분 시야·거실 시작으로 변경했다. 아래 이미지는 변경 전 전체 배치 참고용이다.
- 수면은 기존 방 분류를 유지하며 전투는 후속 [좀비 침입](Zombies/README.md) 구현을 따른다.

검증 결과는 [이력](DataHistory/20261118-shelter-map-layout.md)을 참고한다.

![6개 방과 엘리베이터 배치 — 임시 검증 캐릭터의 전투 장소 도착](Images/ShelterMapLayout.png)

