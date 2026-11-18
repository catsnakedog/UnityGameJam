# Excel 기본 인코딩 설정 및 실제 열기 검증

- Record: 20261118-csv-excel-03-default-setting / 2026-11-18 Asia/Seoul.
- Previous: [CSV 인코딩 적용 기록](20261118-csv-excel-02-applied.md).
- Request: Excel 기본값 설정 후 진행.
- Scope: 현재 Windows 사용자 계정의 로컬 Excel 설정. 게임 데이터 값·필드·정본·생성물에는 변경 없음.

`HKCU\Software\Microsoft\Office\16.0\Excel\Options`의 `DefaultCPG`를 `REG_DWORD 65001`(UTF-8)로 설정하고 다시 읽어 값과 타입을 확인했다. 이전에는 이 값이 없었으며, 상태 백업은 `.utmp/excel-default-encoding-before-20261118-144954.json`이다. 원복은 추가한 `DefaultCPG` 값 하나를 제거하면 된다. 이 레지스트리 설정은 프로젝트를 받는 다른 PC에 자동 적용되지 않는다.

설치된 Excel 파일 버전은 `16.0.20527.20196`이다. 기존 사용자 Excel과 프로세스가 다른 새 자동화 인스턴스를 만든 후, 프로젝트 데이터가 아닌 임시 CSV/텍스트의 한글 셀을 원문과 비교했다. 검증 후 생성한 인스턴스만 종료했다.

| 열기 방법 | UTF-8 BOM | 한글 결과 |
|---|---|---|
| `Workbooks.Open`, Origin 생략 | 없음 | 불일치 |
| `Workbooks.Open`, Origin 생략 | 있음 | 원문 일치 |
| `Workbooks.OpenText`, Origin 생략 | 없음 | 불일치 |

따라서 레지스트리 설정 저장과 CSV 일반 열기 문제 해결을 구분한다. 이번 검사에서 기본값 변경만으로는 해결되지 않았다. GUI 텍스트 가져오기 마법사의 기본 선택 값이나 탐색기 더블클릭 자체는 별도 검사하지 않았다. 프로젝트의 UTF-8 BOM 저장 정책을 유지한다. [Microsoft 공식 안내](https://support.microsoft.com/en-us/excel/opening-csv-utf-8-files-correctly-in-excel)도 일반 열기를 위해 UTF-8 BOM을 안내한다.

`data.example-items`의 [정본 CSV](../../Assets/Settings/Data/Csv/ExampleItems.csv)는 다시 쓰기 잠금을 확인했으나 여전히 사용 중이어서 BOM 적용을 보류했다. [ExampleItems 명세](../DataSpecs/ExampleItems.md)의 데이터 내용·정본 위치는 그대로이며 대체 CSV를 생성하지 않았다. 사용자 Excel 창을 종료하거나 열린 파일을 저장하지 않았다. 나머지 CSV의 이전 BOM 적용 결과는 선행 기록을 따른다.
