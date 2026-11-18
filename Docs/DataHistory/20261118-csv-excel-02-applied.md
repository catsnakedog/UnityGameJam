# CSV Excel 기본 열기 호환 · applied (일부 파일 대기)

- Record: 20261118-csv-excel-02-applied / 2026-11-18 Asia/Seoul.
- Previous: [20261118-csv-excel-01-planned](20261118-csv-excel-01-planned.md).
- Request: Excel에서 별도 인코딩 선택 없이 CSV를 쉽게 열기.
- Code: SpreadsheetImporter가 다운로드 텍스트 선두 BOM을 정규화하고 저장 시 `UTF8Encoding(true)`를 사용하도록 수정했다. 기존 CSV 읽기/파싱 규칙은 유지한다.
- Authoring: AGENTS, DataRegistry, DataManager 및 ExampleItems/FarmingData/GameDevEndings/DataToolSettings 명세에 UTF-8 BOM 저장 규칙을 기록했다.

| 데이터 ID | 파일 | 이 기록 시점 결과 |
|---|---|---|
| data.farming.pickups | Assets/Settings/Farming/FarmingData.csv | 선두 BOM 추가 완료, 기존 211바이트 본문 완전 동일 |
| data.game-dev.endings | Assets/Settings/Data/Csv/GameDevEndings.csv | 선두 BOM 추가 완료, 기존 470바이트 본문 완전 동일 |
| data.example-items | Assets/Settings/Data/Csv/ExampleItems.csv | Excel의 쓰기 잠금 때문에 아직 BOM 없음. 사용자가 닫기를 기다림 |
| editor.data-tool | SpreadsheetImporter 출력 인코딩 | 향후 시트 가져오기 결과는 BOM 포함 UTF-8. 설정 스키마 추가 없음 |

게임 값·스키마·행·ID·줄바꿈·GUID·Addressables 주소·소비자는 변경하지 않았다. 원본 CSV의 BOM 외 본문은 임시 백업과 바이트 및 SHA256으로 대조했다. C#의 UTF-8 파일 읽기가 BOM을 제외하므로 데이터 내용과 소스 해시는 유지된다. 인코딩 수정 때문에 DB/콘텐츠를 다시 생성할 필요는 없다.

검증: 변환된 두 CSV의 BOM 하나, 엄격한 UTF-8 해석, 본문/해시/CSV 셀 동일성 검사 통과. 기존 Unity `QuotedCsvPreservesUnicodeCommaEscapedQuoteAndNewline` 테스트 통과(0.0144428초). 안내/명세의 로컬 링크·필드·GUID 검사 통과. 이번 작업에서 Excel 창을 강제 종료하거나 열린 문서의 미저장 내용을 저장하지 않았다. 잠긴 예제 파일의 완료 결과는 후속 기록에 추가한다.
