# 엘리베이터 두 명 동승

같은 층에서 출발 전에 도착한 호출자 두 명이 한 차를 탄다. 각자 목적층에서 하차하고, 세 번째 호출자는 다음 차를 이용한다. 승강장을 가로지르는 같은 층 이동은 호출을 등록하지 않는다. 지상에서 엘리베이터를 기다리는 좀비 옆도 같은 층으로 걸어서 통과할 수 있다.

- `data.shelter.elevators`의 [정본](../../Assets/Settings/Shelter/ElevatorRules.asset)에서 승강장 간격과 차량 속도를 읽는다. 동승 인원, 현재 좌석, 목적층은 ShelterElevator에서 관리하는 운행 상태다. [명세](../DataSpecs/ElevatorRules.md)
- `data.zombies`의 기존 minimumSpacing을 좀비 하차 시 빈자리 판정에 사용한다. [ZombieRules 시트와 소비 코드](../DataSpecs/ZombieRules.md)
- DataRegistry의 planned 항목과 Assets·ProjectSettings·Docs의 정원·탑승·엘리베이터 주소를 대조했다. 차량 규칙 에셋과 `data/elevators` 주소는 각각 한 개이며, 별도 편집 정본을 추가하지 않았다.

## 검증

Unity 6000.3.28f1에서 작업 중인 프로젝트의 복사본을 열어 검사했다.

- [Edit 검사 54개](../Elevators-checks-edit.txt). 혼자 출발하기, 같은 쪽·반대쪽에서 두 명 타기, 세 번째 호출자의 순서, 서로 다른 목적층에서 내리기, 승강장 수평 통과, 탑승자 한 명 비활성화를 확인했다.
- [벙커 씬 Play 검사 54개](../Elevators-checks-play.txt). 구조자 두 명의 동승·일시정지·탈진 후 개별 하차, 좀비 여섯 마리의 동승·진입 순서·독립 HP, 출구 혼잡 시 지상 간격, 대기 중인 좀비 옆을 걷는 같은 층 이동을 확인했다.
- [좀비 대기열 Play 검사 19개](../Zombies/Verification-queue-play.txt). 선두 방어·처치, 승강장 진입 순서, 지상 최소 간격과 재시작을 확인했다.
