# 2026-11-19 방 멘탈과 구조 계획

대상은 UnityGameJam의 기존 `data.bunker.rules`다. 담당은 jam-rescue다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`(GUID `01a9f70ed623c5341bec7f05cc07781d`, Game Data Local, `data/bunker-rules`)이며 GameData의 기존 로딩 수명을 유지한다.

사용자가 요청한 수면실 +5/인·틱, 전투방 −2/인·틱, 수면실 인계 후 50 이상 회복을 반영한다. 기존 보초 8틱당 1 감소 및 기절 40틱 후 멘탈 30 복원 소비자를 교체한다. 수면실에 인계되기 전에는 소지품이나 사건으로 멘탈을 얻어도 행동을 재개하지 않는다. 기절자 예약·구조자·인계 상태는 씬의 런타임 값이다.

목록과 연결 명세, Assets/ProjectSettings/Docs에서 방 분류, guardTicksPerMental, faintDurationTicks, wakeMental, sleep과 소비자를 조사했다. 침입 시 전원 피해는 별도 사건 효과이고 피곤 표정 임계값은 표시 전용이다. 방 위치는 기존 ShelterArea, 경로와 엘리베이터는 ShelterNavigation/ShelterWalker, 원래 멘탈은 data.characters의 값을 사용한다. 같은 의미를 예약한 새 정본은 없다.
