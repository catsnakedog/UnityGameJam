# 사건 알림 참고 방향 확정

2026-11-18 · Codex event-popup-design-20261118 · 사용자 후속 답변 반영.

- 사용자 확정: “《60 Seconds!》 같은 사건 알림”.
- [UI 설계](../UI/EventLaptopPopup.md)의 참고 방향 미정을 해소했다. 자동 발생 시점·추첨·주기는 별도 미정으로 유지한다.
- 데이터 ID `data.events`, 정본 예정 `Assets/Settings/Data/Csv/Events.csv`, `id:int,title:string,content:string` 계약 유지. [명세](../DataSpecs/Events.md)와 목록에 확정 내용을 반영했다.
- 검증: 현재 목록의 기존 예약과 문서 참조 확인. 신규 데이터/정본/로더를 추가하지 않아 중복 없음. 이전 이력 보존.
- 문서만 변경. 코드·CSV·씬·프리팹 변경 및 Play·빌드 검증 없음.
