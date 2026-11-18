# CSV Excel 기본 열기 호환 · planned

- Record: 20261118-csv-excel-01-planned / 2026-11-18 Asia/Seoul.
- Request: 별도 가져오기 설정 없이 CSV를 Excel에서 쉽게 열기.
- Related: 데이터 목록과 기존 ExampleItems/FarmingData/GameDevEndings/DataToolSettings 명세.
- Scope: 등록된 CSV의 UTF-8 본문을 그대로 유지하고 선두 BOM(EF BB BF)만 추가. 스프레드시트 가져오기 저장도 UTF-8 BOM으로 통일. 중복 BOM 없이 입력 선두 BOM을 정규화.
- Targets: data.example-items (`Assets/Settings/Data/Csv/ExampleItems.csv`), data.farming.pickups (`Assets/Settings/Farming/FarmingData.csv`), data.game-dev.endings (`Assets/Settings/Data/Csv/GameDevEndings.csv`), editor.data-tool (출력 인코딩 규칙; 설정 필드 추가 없음).
- Baseline: ExampleItems는 정상 UTF-8 한글 본문이고 BOM 없음. Excel이 파일 쓰기를 잠근 상태여서 사용자에게 닫기를 요청. 사용자의 열린 파일/미저장 변경을 강제 종료하지 않음.
- Values/schema: 필드, 행, ID, 수치, 줄바꿈, GUID, 주소, 소비자는 변경하지 않음. GameData/Farming 임포터는 이미 BOM 입력을 지원함.
- Existing ownership: GameDevEndings는 game-dev-20261118 작업이 작성 중. 내용은 수정하지 않고, 변환 시 파일을 독점 쓰기로 열어 그 시점의 실제 바이트에 선두 BOM만 추가함.
- Planned checks: 현재 본문 바이트 보존, 각 CSV의 UTF-8 BOM 1개, 인코딩 왕복·기존 CSV 파싱, 공용 CSV 파서 BOM 회귀 검사, Unity 코드 컴파일.

이 기록은 계획이며 잠긴 파일의 변환 완료를 의미하지 않는다.
