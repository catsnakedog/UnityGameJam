# game-dev-20261118 / import blocked and corrective plan

- 작성: 2026-11-18, Asia/Seoul. 선행 [계획](20261118-game-dev-01-planned.md).
- 엔딩 임포트가 기존 ExampleItems.csv의 sharing violation으로 시작 단계에서 실패했다. DB/접근자 적용 전 실패이며 데이터 변경 없음. UI 프리팹과 씬은 별도로 저장 완료.
- 실제 조사: Excel 프로세스가 열려 있고, 해당 CSV에 FileShare.Read로 접근하면 실패, FileShare.ReadWrite 읽기는 성공했다. 사용자 Excel을 닫지 않는다.
- 수정 계획: 공용 DataImporter의 읽기 스트림만 FileShare.ReadWrite로 변경. UTF-8/BOM 처리와 적용 직전 소스 fingerprint 재검증은 유지. 정본 복제/로더 추가 없음.
- 영향 ID: `editor.data-tool` (임포터 동작), `system.game-database` (생성 경로), `data.game-dev.endings` (신규 소비자). 기존 csvFolder/저장 설정/ExampleItems 값은 변경하지 않는다.
- 검증 계획: 쓰기 핸들을 연 CSV를 읽는 회귀 검사, 엔딩 추가 후 기존 ExampleItems 행 보존과 DB hash 확인.
