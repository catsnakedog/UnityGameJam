# 벙커 작업자의 멘탈 소모 계획

2026-11-18 21:19 KST · UnityGameJam · Codex work-mental. 사용자가 개발 작업 중 일정한 멘탈 감소와 SO 조정을 요청했다. [기존 벙커 규칙](../DataSpecs/BunkerRules.md)과 [이전 확장](20261118-ending-popup-02-verified.md)을 확인했다.

- 기존 `data.bunker.rules`의 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이다. GUID `01a9f70ed623c5341bec7f05cc07781d`, 주소 `data/bunker-rules`, 그룹 `Game Data Local`. 시뮬레이션이 GameData 캐시를 통해 로드한다.
- Editor에서 확인한 현재 값은 총 120틱, 1초/틱, 사건 10틱, 침공 30틱, 작업 1틱, 능력당 0.1%p, 기획 해금 기준 50% 초과, 멘탈 상한 100, 기절 40틱, 기상 멘탈 30, 평가 목표 96과 기준 24틱이다.
- `Assets`, `ProjectSettings`, `Docs` 및 목록의 예약 항목에서 개발 효율·작업 주기·작업 멘탈 소모·기절을 조사했다. 개발 효율과 주기는 이 SO가 소유한다. 작업 소모량 필드는 없었다. `data.characters`의 능력/초기 멘탈, 전투·이벤트의 멘탈 효과는 기존 경로로 읽는다.
- 기존 SO에 `mentalCostPerWork`를 추가한다. 기준은 작업자 1명·작업 1회당 정수 포인트이며 임시 초깃값은 1이다. 이번 요청이 이전 공간 문서의 작업방 멘탈 변화 0을 대체한다.
- 세 개발 분야의 유효한 작업을 집계할 때 같은 작업자의 현재 멘탈에 한 번 적용한다. 이동·잠금·점거·완료된 분야와 일시정지, 여러 작업자, 탈진과 재시작을 검증한다.
