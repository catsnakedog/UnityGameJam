# ExampleItems 폐기 명세

- 데이터 ID: `data.example-items`; 상태: **deprecated (삭제 완료)**; 2026-11-18.
- 목적: CSV 타입/조회 시연. 실제 게임 소비자 없음. 사용자 요청으로 example_wood/example_stone 2행과 테이블 전체를 폐기했다.
- 현재 편집 정본: 없음. 이전 Google Sheets는 휴지통 이동, `Assets/Settings/Data/Csv/ExampleItems.csv`와 meta 삭제, DataToolSettings 연결 삭제, 기존 생성 DB 및 타입 접근자에서 제거.
- 실제 캐릭터/사건/물자/파밍/엔딩 데이터로 자동 변환하지 않았다. 새 ID/DB/로더 없음. 런타임 저장 ID로 사용하지 않아 저장 이전 없음.
- GameDevChecks는 더 이상 이 예제의 존재를 요구하지 않는다. 현재 CSV의 모든 테이블 이름/행 수와 생성 DB를 대조한다.
- 재도입 금지: 도구 예제를 게임 데이터 폴더/공유 시트에 다시 생성하지 않는다. 파서 테스트의 메모리 내 fixture는 게임 데이터가 아니다.
- 이전 필드/값은 변경 불가능한 [원래 검증 이력](../DataHistory/20261118-notion-sheets-06-remote-verified.md)과 당시 생성 이력에만 보존한다.
- [제거 계획](../DataHistory/20261118-example-cleanup-01-plan.md), [제거 및 양식 검증](../DataHistory/20261118-example-cleanup-02-verified.md).
