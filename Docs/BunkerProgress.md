# 벙커 진행 사용법

`Assets/Settings/Shelter/BunkerRules.asset`에서 총 틱, 초/틱, 랜덤 이벤트·침공·작업 간격과 능력당 증가량을 편집한다. 같은 SO의 **Mental Cost Per Work**는 작업자 한 명이 한 번 일할 때 쓰는 멘탈 포인트다. 기본 설정에서는 1초마다 1점씩 소모한다. 변경 후 기존 Data Manager의 Addressables 콘텐츠 빌드를 실행한다. 현재 Play는 Use Existing Build 모드이므로 빌드 갱신이 필요하다.

씬 `ShelterPrototype`의 각 방 `ShelterArea > Room Type`에서 6종을 선택한다. 프로그래밍/그래픽/기획만 작업하며 기존 캐릭터 클릭 → 방 클릭 이동을 사용한다. 도착 후 소속이 생기고, 새 이동 명령 수락 시 소속이 해제된다. 단순히 목표 방을 선택하거나 다른 방을 통과하는 동안은 작업하지 않는다. 처음부터 방 바닥에 생성된 캐릭터는 해당 방에 소속된다.

작업 1회 증가량 = 방 소속 캐릭터들의 해당 능력 합 × Percent Per Ability (퍼센트포인트). 예: 프로그래밍 능력 10, 계수 0.1이면 작업마다 프로그래밍 게이지 +1%p. 분야별 최대 100%. 세 분야 모두 채워도 정해진 총 틱까지 진행하며 마지막 작업 이후 종료한다.

GameDevManager 오브젝트의 BunkerSimulation Inspector에서 현재/남은 틱, Pause, Open Bunker Rules를 사용할 수 있다. 랜덤/침공은 `RandomEventDue(int tick)` / `ZombieInvasionDue(int tick)` C# 이벤트만 제공하며 확률 추첨/좀비 생성은 아직 없다. `WorkProcessed`, `TickOccurred`, `DurationEnded`로 후속 기능을 연결한다. `GameDevManager.RestartDevelopment()`는 진행도와 틱을 초기화한다.

파밍을 마치고 벙커에 진입하면 1번과 추가로 구조한 생존자들이 작업자로 합류한다. 벙커 씬에서 직접 Play하면 파밍 명단이 비어 있다. 현재 점수 공식은 미정이므로 시간 종료 후 기존 AwaitingScore 상태로 넘어간다.

[데이터 명세](DataSpecs/BunkerRules.md) · [검증 기록](DataHistory/20261118-bunker-ticks-02-verified.md)
