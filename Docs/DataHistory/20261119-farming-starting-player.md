# 파밍 플레이어와 기본 확보 인원

이전 회차에서는 1번을 플레이어와 별도의 운반 대상으로 생성해 운반 2칸을 사용했다. 변경된 회차에서는 1번을 직접 조작한다. 복도에서 0/4칸으로 출발하고 확보 인원은 시작부터 1/6명이다. 나머지 다섯 명은 벙커에 가까운 325·324·323호에 각각 2·2·1명씩 들어간다. 추가 구조 없이 파밍을 마쳐도 1번은 벙커 명단에 한 번 들어간다.

- `data.farming.rules`의 기존 `startingPersonId`가 플레이어, 플레이어의 애니메이션, 최초 확보 인원을 결정한다. 기존 에셋에 저장된 값은 `person_01`이며 방별 인원 값도 그대로 사용한다. [편집 에셋](../../Assets/Settings/Farming/FarmingRules.asset) · [필드 명세](../DataSpecs/FarmingRules.md)
- `data.farming.pickups`의 Person ID와 `data.characters`의 동일 ID로 인물을 식별한다. 두 데이터의 스키마와 값은 이번 변경에 포함되지 않는다. [FarmingData 정본과 명세](../DataSpecs/FarmingData.md) · [Characters 정본과 명세](../DataSpecs/Characters.md)
- 중복 조사에서 데이터 목록, 파밍 씬·프리팹·규칙·생성 코드, 벙커 명단 전달 경로를 확인했다. 플레이어 프리팹에 있던 별도 외형 ID도 규칙의 시작 ID로 통일한다. 확보 명단과 운반 칸은 회차 상태다.

Unity 6000.3.28f1 Play 모드에서 복도의 1번, 빈 운반 칸 4개, 확보 인원 1/6명을 화면으로 확인했다. 방 안의 다섯 명, 이동·운반·투입, 시간 만료까지 파밍 검사 127개를 통과했다. [시작 화면](../../outputs/Farming/starting-player.png) · [파밍 검사 기록](../Farming/Verification-SideView.txt)

추가 구조 0명·1명·5명으로 각각 벙커에 진입해 실제 생존자 1명·2명·6명의 ID를 대조했다. 파밍 재시작으로 초기 명단과 운반 칸이 복구되고, 벙커 씬을 다시 열어도 각 인물이 한 번씩만 생성됐다. 벙커 연동 검사 80개를 통과했다. [벙커 연동 검사 기록](../CharacterLink-play.txt)
