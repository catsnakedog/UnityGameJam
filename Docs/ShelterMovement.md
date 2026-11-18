# 캐릭터 선택 → 방 클릭 자동 이동

현재 씬은 사용자 스케치에 따른 [6개 방·2개 사다리 배치](ShelterMapLayout.md)를 사용한다. 아래의 MIRA/JIN/NOA 예제와 3.8 층 간격 설명은 최초 이동 프로토타입의 기록이며, 현재 층 높이·생성 위치는 링크한 배치 문서를 따른다.

`Assets/Scenes/ShelterPrototype.unity`를 열고 Play한다. 캐릭터를 왼쪽 클릭한 뒤 목적지 방의 빈 공간을 클릭하면 방·통로·사다리를 거쳐 자동으로 이동한다. WASD와 방향키 직접 이동은 제거했다.

예제에는 MIRA, JIN, NOA 세 캐릭터가 있다. 각자 목적지를 따로 기억하므로 캐릭터를 바꿔 선택해도 앞서 내린 이동 명령은 계속 실행된다. 선택한 캐릭터의 발밑과 목적지 방 바닥은 노란색으로 표시된다. 이동 중 다른 방을 클릭하면 현재 위치에서 경로를 다시 계산하며, 사다리 중간에서도 반대 방향으로 돌아갈 수 있다.

## 자유 배치

`Assets/Prefabs/Shelter`의 네 프리팹을 씬에 드래그하거나 `GameObject > Shelter` 메뉴로 추가한다. 기존 오브젝트를 복제해도 된다. 빈 씬에서 시작할 때는 메뉴를 한 번 사용하면 이동 관리자와 카메라도 준비된다. 기존 예제 씬에는 이미 관리자가 있다.

| 프리팹 | 배치 기준 | Inspector 설정 |
|---|---|---|
| Room | Transform 위치가 방 바닥 중앙 | `ShelterArea`: Display Name, Width, Height |
| Corridor | Transform 위치가 통로 바닥 중앙 | `ShelterArea`: Width, Height |
| Ladder | Transform 위치가 사다리 아래쪽 끝 | `ShelterLadder`: Height |
| Survivor | Transform 위치가 캐릭터 발 위치 | `ShelterWalker`: Character Name, Walk Speed, Climb Speed, Arrival Offset |

방·통로·사다리 연결 참조를 직접 넣거나 내비게이션을 Bake할 필요가 없다.

1. 같은 높이의 방과 통로는 좌우 끝이 맞닿거나 겹치면 연결된다. 방끼리 바로 맞붙여도 연결된다.
2. 서로 떨어진 방은 사이를 통로로 이어 준다. 공간이 비면 건너뛰지 않는다.
3. 사다리 아래·위 끝을 각각 연결하려는 방 또는 통로의 바닥 높이에 맞춘다. X 좌표는 해당 방/통로의 폭 안에 있어야 한다.
4. 캐릭터의 발을 방 또는 통로 바닥 위에 배치한다. 별도의 시작 층 설정은 없다.
5. 가로 방과 수직 사다리를 기준으로 한다. 회전은 0도로 두고, 크기는 Width/Height 또는 양수 Scale로 조절한다.

기본 연결 오차는 0.06 월드 유닛이며 `Shelter navigation and selection > ShelterNavigation > Connection Tolerance`에서 조절할 수 있다. 방을 선택하면 Scene 뷰 Gizmo로 바닥 선과 양 끝이 표시되고, 사다리를 선택하면 연결할 양 끝이 표시된다.

### 배치 예시

프리팹 기본 크기는 Room 8×4, Corridor 2×4, Ladder 높이 4이다. 다음 배치는 수동 참조 없이 연결된다.

| 오브젝트 | 위치 X, Y |
|---|---|
| Room A | 0, 0 |
| Corridor | 5, 0 |
| Room B | 10, 0 |
| 위층 Room C | 10, 4 |
| Ladder | 10, 0 |
| Survivor | 0, 0 |

캐릭터를 선택하고 Room C를 클릭하면 A → 통로 → B → 사다리 → C로 이동한다. 예제 ShelterPrototype은 기존 미술 배치를 유지해 방 폭 8.65, 층 간격 3.8을 사용한다. 이 씬에 추가할 사다리는 Height를 3.8로 맞춘다.

## 연결 변경과 경로

- 이동 시간 기준 최단 경로를 찾는다. 가로 구간은 걷기 속도, 사다리는 사다리 속도를 사용한다.
- 이동 명령을 내릴 때, 또는 Play 중 배치·폭·활성 상태가 바뀌면 연결을 다시 조사한다. Play 중 변경 확인 주기는 0.2초다.
- 방/통로/사다리의 `Accessible`을 끄거나 오브젝트를 비활성화하면 그 경로를 사용할 수 없다.
- 도달할 수 없는 방을 클릭하면 빨간색으로 표시하고 `NO ROUTE`를 표시한다. 거부된 명령은 기존 이동 명령을 바꾸지 않는다.
- 이동하던 경로가 끊어지면 다른 경로를 찾고, 대안이 없으면 현재 위치에서 정지한다. 방을 이동시켜도 캐릭터를 새 위치로 순간이동시키지 않는다.
- Arrival Offset은 방 중앙에서 도착 위치를 좌우로 벌리는 값이다. 예제 캐릭터는 서로 다른 값을 사용한다. 캐릭터 간 충돌·대기열은 구현하지 않았다.

배경 가구는 이동 경로를 막지 않는다. 방의 이동 구간 안에 별도 장애물을 놓고 피하는 기능은 현재 범위에 포함하지 않는다.

## 코드와 검증

런타임: `Assets/Scripts/Shelter`. 씬 변환·프리팹 생성·검증: `Assets/Editor/ShelterNavigationSetup.cs`, `ShelterMovementChecks.cs`. 기존 `ShelterFloor`는 최초 블록아웃 제작용으로만 남아 있으며 자동 경로 탐색에는 사용하지 않는다.

2026-11-18, Unity 6000.3.28f1에서 확인:

- 컴파일 오류 없음.
- `Tools > Shelter > Validate Click Navigation`: 27개 검사 통과. 선택 우선순위, 캐릭터별 동시 이동, 통로·사다리 재배치, 연결 해제·복구, 사다리 중간 재명령, 최단 경로, 임의 좌표·배율, 30/60/144 FPS 이동 거리 포함.
- 새 임시 씬에 실제 Room/Corridor/Ladder/Survivor 프리팹만 배치해 자동 연결·목적지 도착 확인. 통로 이동으로 연결이 끊어지는 것도 확인.
- Editor Play에서 Input System 마우스 입력으로 MIRA 선택 → LOOKOUT 클릭 → 사다리 2개를 경유한 3층 도착 확인. 다른 캐릭터는 제자리 유지.
- 같은 Play 검증 중 W/D를 눌러도 직접 이동하지 않는 것을 확인. 테스트 입력 장치는 검증 후 제거했다.
- 화면 캡처로 선택·도착 표시 확인. 검증 종료 시 Console 오류·경고 0개.

![클릭 자동 이동 프로토타입](Images/ShelterMovement.png)

## 구조 명단 연결 (2026-11-18)
현재는 Farming 씬에서 시작하여 사람을 벙커에 투입한 뒤 벙커로 이동해야 캐릭터가 배치됩니다. 기존 MIRA/JIN/NOA 예제 오브젝트는 비활성 보존했으며 직접 벙커 실행 시 캐릭터는 없습니다. 구조한 각 캐릭터는 거실의 spawn point에 생성됩니다. 런타임 CharacterIdentity Inspector에서 ID·능력·멘탈을 확인할 수 있습니다. 데이터 편집은 [Characters 명세](DataSpecs/Characters.md)를 따릅니다.
