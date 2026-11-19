# 벙커 맵 배치

2026-11-18 사용자 스케치 반영. 편집 정본은 `Assets/Scenes/ShelterPrototype.unity`의 ShelterArea/Transform/ShelterElevator다. 방 배치는 유지하고 후속 이동 변경으로 엘리베이터를 설치했다. 차량 규칙은 별도 [ElevatorRules](DataSpecs/ElevatorRules.md)를 사용한다.

```text
                 전투 장소 ───┐
                              │
                          엘리베이터
                              │
기획 ─────┬──────── 거실 ─────┴──── 수면
          │
        엘리베이터
          │
아트 ─────┴────── 프로그래밍
```

방 그림은 `Assets/Art/Shelter/Rooms`에 있다. 원본 PNG를 생존자와 같은 22 PPU로 표시한다. 전투실은 288×128 px(13.09×5.82 유닛), 나머지 방은 240×128 px(10.91×5.82 유닛)이다. [공유 원본](https://drive.google.com/drive/folders/1-XJ1AU6Yk9nNNJWhlBI81PZtpUBUJ58B)

여섯 방의 천장 전등은 `Light.png`의 왼쪽(켜짐)과 오른쪽(꺼짐) 프레임을 사용한다. 방마다 전등 세 개와 URP Light 2D 세 개가 함께 켜지고 꺼진다. 평상시 방마다 독립적으로 평균 45초에 한 번, 0.08~0.16초 동안 깜빡인다. `Assets/Prefabs/Shelter/RoomLighting.prefab`의 Average Flicker Interval에서 평균 간격을 조정할 수 있다. 좀비가 점거하면 바로 소등하고 마지막 점거자가 사라지면 다시 점등한다. [기획실 점등·소등 비교](Images/ShelterRoomLighting.png)

씬의 방 중심 좌표는 그림 아래에서 16 px 위에 있는 보행선을 기준으로 한다. 각 방의 `Room artwork` SpriteRenderer에서 그림을 교체할 수 있다. 방 크기와 클릭 영역은 스프라이트 경계를 따른다. 기획실과 수면실에는 Flip X를 적용해 출입구가 복도를 향한다.

수면실에는 바닥을 따라 텐트 두 동이 있다. 각 텐트의 SpriteRenderer는 `Rooms/Tent.png`의 왼쪽 프레임을 비었을 때, 오른쪽 프레임을 사람이 들어갔을 때 표시한다. 캐릭터를 선택하고 방의 빈 곳을 클릭하면 입장하고, 텐트를 클릭하면 그 텐트까지 걸어가 멘탈이 가득 찰 때까지 쉰다.

기획·아트·프로그래밍 방의 `Workstation - person_XX` 오브젝트를 좌우로 옮기면 해당 캐릭터의 작업 위치를 조정할 수 있다. 방의 ShelterArea에 있는 Workstations 목록에서 인물 ID, 위치 참조, 작업 그림의 좌우 반전을 편집한다. 직군은 기존 Characters 표의 `role`을 따른다.

| 방 | 중심 X | 바닥 Y | 방 분류 | 원본 그림 |
|---|---:|---:|---|---|
| 기획 | -13.83 | 7.5 | Planning | PlanningDepartment.png |
| 거실 | 1.13 | 7.5 | Common | LivingRoom.png |
| 수면 | 16.09 | 7.5 | Sleeping | Sleepingroom.png |
| 아트 | -12.88 | 0 | Graphics | ArtRoom.png |
| 프로그래밍 | 3.33 | 0 | Programming | DevelopmentTeam.png |
| 전투 장소 | 0.68 | 17.63 | ZombieInvasion | FightingRoom.png |

- 아래 엘리베이터: X=-5.8, Y=0 → 7.5. 위 끝은 기획–거실 통로, 아래 끝은 아트–프로그래밍 통로에 연결된다.
- 위 엘리베이터는 X=8.47에서 중층 거실–수면 통로(Y=7.5)와 전투실 오른쪽 승강장(Y=17.63)을 연결한다.
- 방 사이 통로는 3개, 엘리베이터 승강장은 4개다. 승강장 폭은 6이며 양쪽 호출·하차 지점을 덮는다.
- 거실의 기존 spawnPoints 6개에 파밍에서 확보한 생존자를 배치한다. 첫 번째는 파밍 플레이어이며 그 뒤로 추가 구조한 생존자가 합류한다.
- 이전 ENTRY/ATTIC/REST AREA는 `99 - Previous layout (inactive)` 아래 비활성 보존했다. 기존 사각 건물 외곽도 비활성화했다.
- 방과 캐릭터의 이름표는 게임 UI와 같은 Neo둥근모를 사용한다. 게임 카메라는 거실에서 시작한다. [카메라 조작과 시야](ShelterCamera.md)
- 수면은 기존 방 분류를 유지하며 전투는 후속 [좀비 침입](Zombies/README.md) 구현을 따른다.

Unity에서 여섯 방의 이미지 비율·클릭 경계·바닥선과 거실 스폰 6개를 확인했다. 전투실 이동 후 끊겼던 경로 10개를 승강장 재배치로 복구했다. Play 모드에서 방 사이 36개 경로와 양쪽 승하차 지점을 확인하고, 생존자 한 명이 아트실과 전투실 사이를 엘리베이터 두 대로 왕복했다. [최초 배치 이력](DataHistory/20261118-shelter-map-layout.md)

![벙커 전체 배치. 전투실·수면실·프로그래밍실에 생존자가 도착한 플레이 화면](Images/ShelterRoomArtwork.png)

