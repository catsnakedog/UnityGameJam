# 파밍 준비 시간 도입

2026-11-19 KST · UnityGameJam · 계획

[기존 규칙](../DataSpecs/FarmingRules.md)의 `duration`은 구조에 쓸 수 있는 30초다. Assets, ProjectSettings, Docs 및 레지스트리의 등록·예약 항목에서 별도의 파밍 사전 탐색 시간 필드를 찾지 못했다. 요청된 15초는 같은 `data.farming.rules` 정본에 `preparationDuration`으로 추가한다. 시작 버튼 이후 이동과 문 출입을 허용하고, 준비 종료 시 현재 위치에서 구조 시간을 시작한다.

정본은 `Assets/Settings/Farming/FarmingRules.asset`, GUID는 `22168f09c24cbfb49aeb55b136dd0298`, 주소는 `data/farming/rules`이다. 회차별 남은 준비 시간은 FarmingSession이 소유하고 FarmingHud가 표시한다.
