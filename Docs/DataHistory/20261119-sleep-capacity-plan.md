# 수면실 정원과 방 도착 위치

2026-11-19, 사용자 요청에 따라 벙커 방에 여러 명이 도착하면 위치를 나누고 수면실 한 곳의 정원을 2명으로 둔다. 이전에는 생존자 프리팹의 동일한 `arrivalOffset`에서 도착했고 수면실에는 인원 한도가 없었다.

- 등록 목록과 planned 항목, 벙커 설정, 이동 명령, 구조, 씬·프리팹·공간 데이터에서 정원과 도착 위치를 조사했다. 엘리베이터 동승 정원은 차량의 이동 규칙이고 좀비 대열 간격은 적의 보행 규칙이다.
- 기존 `data.bunker.rules`에 필수 양의 정수 `sleepingRoomCapacity`를 추가한다. 단위는 수면실 한 곳당 생존자 수이고 초기 설정은 2다. 방별 도착 위치는 실제 방 폭과 현재 도착·예약 위치로 계산한다. 개별 인물의 배치표는 추가하지 않는다.
- 기존 Addressables 정본은 `Assets/Settings/Shelter/BunkerRules.asset`(GUID `01a9f70ed623c5341bec7f05cc07781d`, 주소 `data/bunker-rules`, 그룹 Game Data Local)이다. 시뮬레이션이 로드한 규칙으로 이동·구조를 판정하며 위치와 예약은 씬 수명 동안만 유지한다. [명세](../DataSpecs/BunkerRules.md)
