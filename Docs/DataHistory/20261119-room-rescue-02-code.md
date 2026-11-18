# 2026-11-19 방 멘탈과 기절 구조 구현

UnityGameJam의 `data.bunker.rules`를 쓰는 CharacterIdentity, BunkerSimulation, ShelterWalker, ShelterCommander 및 초기 설치 코드를 변경했다. 조정 항목은 `sleepMentalPerTick` 5점/인·틱, `guardMentalLossPerTick` 2점/인·틱, `wakeMental` 50점 이상이다. 수면실 인계, 기절 판정과 각성은 같은 CharacterIdentity 상태를 사용한다. 캐릭터 정의(`data.characters`)의 초기 멘탈/ID, 기존 수면실·전투방의 ShelterArea, 엘리베이터와 경로 탐색을 재사용했다.

기존 BunkerRules SO가 편집 정본이며 GUID `01a9f70ed623c5341bec7f05cc07781d`, Game Data Local 그룹과 주소 `data/bunker-rules`를 유지한다. [변경 전 조사](20261119-room-rescue-01-plan.md) · [필드·이전 명세](../DataSpecs/BunkerRules.md)

워크트리의 전체 런타임/Editor C#을 Unity 6000.3.28f1의 Roslyn과 연결 프로젝트의 Bee 참조로 별도 임시 경로에 컴파일했다. 두 어셈블리 모두 성공했다. 폐기 필드의 소비자 재검색과 git diff --check를 통과했다. BunkerChecks.RunPlay에 틱당 회복/차감, 취소되지 않는 기절, 클릭 구조, 복수 구조자 예약, 엘리베이터 운반, 인계 후 각성과 일시정지 검사를 추가했다. SO 실값 저장·Addressables 재빌드·Play 실행은 씬을 담당한 메인 프로젝트에서 통합 검증한다.
