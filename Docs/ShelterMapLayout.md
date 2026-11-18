# 벙커 맵 배치

2026-11-18 사용자 스케치 반영. 편집 정본은 `Assets/Scenes/ShelterPrototype.unity`의 ShelterArea/Transform/ShelterLadder다. 별도 CSV·SO·런타임 생성기는 추가하지 않았다.

```text
                 전투 장소
                     │
                   사다리
                     │
기획 ─────┬──────── 거실 ───── 수면
          │
        사다리
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

- 아래 사다리: X=-5.8, Y=0 → 7.5. 위 끝은 기획–거실 통로, 아래 끝은 아트–프로그래밍 통로에 연결된다.
- 위 사다리: X=-1.5, Y=7.5 → 17.5. 거실과 전투 장소를 연결한다.
- 통로 3개는 기존 Corridor 프리팹을 사용한다. 사다리·방은 기존 씬 오브젝트를 재배치했다.
- 구조된 캐릭터의 기존 spawnPoints 6개를 거실로 옮겼다. Farming에서 구조한 명단만 생성한다.
- 이전 ENTRY/ATTIC/REST AREA는 `99 - Previous layout (inactive)` 아래 비활성 보존했다. 기존 사각 건물 외곽도 비활성화했다.
- 한글 이름은 기존 NotoSansCJKkr 폰트를 사용한다. 카메라는 6개 방과 HUD가 겹치지 않도록 조정했다.
- 수면·전투는 기존 방 분류만 연결했으며 새 수면/전투 규칙은 추가하지 않았다.

검증 결과는 [이력](DataHistory/20261118-shelter-map-layout.md)을 참고한다.

![6개 방과 사다리 배치 — 임시 검증 캐릭터의 전투 장소 도착](Images/ShelterMapLayout.png)
