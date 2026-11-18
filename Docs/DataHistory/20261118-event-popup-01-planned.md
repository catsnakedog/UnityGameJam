# 노트북 이벤트 표시 설계 예약

2026-11-18 · Codex event-popup-design-20261118 · 문서 변경만.

- 이전: 사건 콘텐츠 테이블/팝업 없음. 기존 BunkerSimulation에 RandomEventDue, 기존 BunkerRules에 주기 설정 존재.
- 이후: `data.events`를 planned로 예약. 정본 예정 `Assets/Settings/Data/Csv/Events.csv`, `id:int,title:string,content:string`.
- [현재 데이터 명세](../DataSpecs/Events.md), [UI 설계](../UI/EventLaptopPopup.md).
- 기존 GameData/DB/CSV 도구를 재사용하는 설계. 주기/캐릭터 능력/엔딩 데이터를 복제하지 않음. 효과 적용은 범위 제외.
- 실제 검사: Assets·ProjectSettings·Docs의 별칭/의미 검색, 현재 색인 및 주기·DB·엔딩 명세 대조, BunkerSimulation/BunkerTickClock/ShelterCommander/GameData/DataTable의 실제 코드 확인. 동일 의미 정본/예약 발견 없음.
- 미정: “60초” 의미, 자동 사건 선정, 실제 원고, 최종 아트. 주기 수치는 변경하지 않음.
- 실제 CSV/DB/소비 코드/씬/프리팹은 변경하지 않음. Play·빌드 검증 없음. 이전 이력 보존.
