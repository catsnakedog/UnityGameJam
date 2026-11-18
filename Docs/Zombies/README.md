# 좀비 침입 시스템

`ShelterPrototype.unity`의 습격 일정은 BunkerRules에서 편집한다. 960틱 회차에서 첫 습격은 20틱(00:30), 이후 80틱마다 마지막 900틱(22:30)까지 총 12회 발생한다. 시계의 붉은 원은 해당 습격이 시작되면 사라진다.

## 플레이

1. 파밍을 마치고 벙커로 진입하면 1번과 추가 구조한 생존자가 배치된다. 벙커 씬을 직접 실행하면 파밍 명단이 비어 있다.
2. 캐릭터를 클릭하고 위쪽 `전투 장소`를 클릭해 보초를 배치한다.
3. 좀비는 전투 방 입구까지 공용 보행 속도로 6틱이 걸리는 위치에서 나타난다. 보초는 접근하는 선두 좀비에게 2틱마다 각자 공격한다. 원거리에서 공격이 빗나가도 보초에게 반격 피해가 들어오지 않는다.
4. 선두가 막힘없이 접근하면 생성 후 2·4·6틱에 사격한다. 마지막 사격으로 처치하면 전투 방 인원이 처치 보상을 받는다. 좀비가 살아서 들어오면 구조 인원 전원의 멘탈이 즉시 2 줄고, 방 안에서 행동 가능한 수비자와 곧바로 한 차례 근접전을 벌인다.
5. 근접전은 5틱마다 반복한다. 참여한 캐릭터가 각자 공격하고 좀비는 한 명만 노린다. 좀비가 죽거나 싸우는 아군 모두가 탈진할 때까지 계속한다. 수비자가 없는 방을 통과한 좀비는 남은 HP로 연결된 방에 이동한다. 경로에서 만난 사람과도 쓰러질 때까지 싸운다.
6. 목표 방을 점거한 좀비는 방 기능을 정지시킨다. 마지막 점거자를 처치하면 방이 풀린다. 점거한 좀비의 처치 보상은 그 좀비와 전투한 캐릭터가 받는다.

보초는 전투 방에 배치된 동안 매 틱 멘탈 2를 소모한다. 좀비가 없을 때도 적용하며 기절하면 다른 생존자가 수면실로 구조해야 한다. 팝업 중에는 접근·전투·배치 시간이 함께 멈춘다.

여러 좀비는 전투 방의 외벽 쪽에서 같은 속도로 접근하고 각자의 HP를 유지한다. 앞이 막히면 뒤쪽 좀비는 간격을 두고 기다린다. 자유 시점에서는 화면 밖이나 가장자리에 새로 나타난 좀비를 카메라가 한 번 비춘다. 마우스 휠로 접근 모습을 확대하거나 전체 경로를 볼 수 있다. 이동 간격은 ZombieRules의 minimumSpacing을 사용한다. 엘리베이터에는 같은 층에서 기다리는 두 명까지 함께 탑승한다. [벙커 카메라](../ShelterCamera.md) · [엘리베이터 공용 규칙](../DataSpecs/ElevatorRules.md)

## 배치와 조정

- 기존 `ShelterArea` 방·통로, `ShelterElevator` 엘리베이터를 배치한다. 가장 위쪽의 활성 `ZombieInvasion` 타입 방이 생성/방어 공간이다. 방 종류/좌표를 직접 바꿔도 기존 자동 경로를 사용한다.
- 점거는 `accessible`을 끄지 않는다. 다른 방 기능을 추가할 때 `ShelterArea.IsOperational`을 확인한다. 기존 작업은 `ShelterWalker.CanWorkInCurrentRoom`으로 연결되어 있다.
- [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193)에서 종류별 값, [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)에서 전투/이동 값을 수정한다. `Tools > Data Manager > 전체 시트 가져오기` 후 Addressables 콘텐츠 빌드 실행. `ZombieData.asset`은 읽기 전용 생성물이다. 방 배치나 프리팹에 HP/전투 수치를 복제하지 않는다.
- 개발 매니저의 `ZombieInvasionController` Inspector에 Play용 `좀비 1마리 생성` 버튼이 있다. 정식 습격은 BunkerSimulation 이벤트로 발생한다.
- 설치 재연결은 `Tools > Zombies > Install Invasion`. 기존 방 배치와 SO 값은 보존한다.

## 데이터 정본·명세

| ID | 정본 / 이번 변경 | 명세 |
|---|---|---|
| data.zombies | [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193) / [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486) → CSV 스냅샷 → 기존 ZombieData.asset / data/zombies | [Zombies](../DataSpecs/Zombies.md), [ZombieRules](../DataSpecs/ZombieRules.md) |
| data.bunker.rules | Assets/Settings/Shelter/BunkerRules.asset의 접근 시간, 보초 원거리 사격 주기, 침입 시 전원 멘탈 손실, 보초 피로 주기 | [BunkerRules](../DataSpecs/BunkerRules.md) |
| data.characters | 기존 Characters 시트 → Assets/Settings/Data/Csv/Characters.csv → 공용 DB. 전투력/멘탈 소비 재사용, 값 변경 없음 | [Characters](../DataSpecs/Characters.md) |
| data.events | 기존 Events 시트 → Assets/Settings/Data/Csv/Events.csv → 공용 DB. NEXT_WAVE_HP 실제 생성 연결, 값 변경 없음 | [Events](../DataSpecs/Events.md) |

Assets/ProjectSettings/Docs와 DataRegistry planned까지 중복 검사 완료. 새 캐릭터 DB/이벤트 DB/CSV 로더 없이 기존 GameData/Addressables·자동 경로를 재사용했다.

최신 플레이 검사는 원거리 공격, 입구 침입, 보초 피로, 지속 교전, 점거 해제와 재시작을 확인한다. [Play 검사](Verification-play.txt) · [대기열 검사](Verification-queue-play.txt)

엘리베이터 배치/소유권과 차량 속도는 [자동 이동 안내](../ShelterMovement.md), [data.shelter.elevators 명세](../DataSpecs/ElevatorRules.md)를 따른다. 기존 ZombieRules의 climbSpeed 열은 호환 보존하며 런타임에서 사용하지 않는다.
