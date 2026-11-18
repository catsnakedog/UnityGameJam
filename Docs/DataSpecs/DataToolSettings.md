# 데이터 도구 설정 명세

## 플레이 데이터 소스

`editor.data-tool` v3은 기존 설정 파일에 `useLiveSheets`를 추가한다. 단위 없는 bool이며 기존 파일에서 생략하면 기본값은 false다. true는 기존 spreadConfigUrl로 실행 때마다 활성 시트를 조회하고, false는 내부 CSV 기반 Addressables를 사용한다. 설정 정본은 `ProjectSettings/GameDataTool.asset` 하나다. 빌드에는 선택 결과만 포함한다. 원격 연결 목록이나 콘텐츠를 설정 파일에 저장하지 않는다.

공용·파밍·좀비 데이터는 모두 검증한 뒤 해당 게임 실행에만 제공한다. 실패 시 초기화 오류를 전달한다. 활성 행의 파일명, ID, gid는 기존 계약을 따른다. 기존 Config의 종류 탭 별칭 `ZombieData`는 전투용 종류에도 연결하며 이 형식에서 규칙 탭이 생략되면 내부 규칙을 사용한다. 현재 공용 CSV에도 같은 이름의 스냅샷이 있어 양쪽 조회에 같은 다운로드를 사용한다. 중복 조사와 이전 상태는 [계획](../DataHistory/20261118-live-sheets-01-planned.md)에 기록했다.

## 좀비 분기 확장 (2026-11-18)

담당 Codex zombie-sheets-20261118. 기존 `editor.data-tool`의 스키마/Config URL/CSV 폴더는 유지한다. `Zombies`와 `ZombieRules`는 파밍처럼 공용 CSV/DB에 넣지 않고 기존 ZombieData SO로 변환한다. 두 연결은 정확한 이름으로 함께 활성화하거나 함께 제외한다. 단독 활성화/대소문자 변경은 오류다. 고급 공용 미리보기는 세 전용 테이블을 제외한다. 전체 가져오기 시 모든 시트 검증 후 적용하고 오류/취소/동시 수정은 기존 값 보존 정책을 따른다. 원격 등록 및 실제 전체 가져오기 완료. 기존 5개 연결을 보존하고 Zombies/ZombieRules 2행을 추가하여 활성 7개 탭이다. [계획](../DataHistory/20261118-zombie-sheets-01-planned.md), [명세](Zombies.md), [규칙](ZombieRules.md).

## SpreadConfig 연결 계약 v2 (2026-11-18)

- 데이터 ID `editor.data-tool`, 담당 Codex spread-config-20261118. 신규 게임 데이터 ID/DB/로더 없음.
- 연결 목록 정본: [SpreadConfig의 SpreadConfig 탭](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615). 빈 시트1(gid=0) 또는 이전 xlsx가 아니다.
- 로컬 설정 정본: 기존 `ProjectSettings/GameDataTool.asset`. `csvFolder` 유지, `spreadConfigUrl:string` 추가(기본 빈 값, 사용 전 필수, HTTPS Google Sheets 문서/탭 링크). 구 `sheets`, `farmingSheetUrl`, `spreadsheetEnabled`는 제거하며 로컬 목록으로 폴백하지 않는다.
- Config는 1행 필드명/2행부터 연결 목록이다. 게임 콘텐츠 시트의 3행 메타데이터 형식과 구분한다. 이전 직접 URL 목록 편집 방식은 폐기했다.

| Config 필드 | 타입·단위 | 필수·빈 값·기본·제약 |
|---|---|---|
| sheetName | string, 무차원 | 필수, 출력 파일명. .csv 확장자는 선택. 경로 구분자 금지, C# 식별자 규칙, 대소문자 무시 중복 금지. 기존 파일명 대소문자 유지 |
| id | string, 무차원 | 활성 행 필수. Google spreadsheet 문서 ID(영문/숫자/밑줄/하이픈). gid와 URL 구성 |
| gid | string, 무차원 | 활성 행 필수. 0 이상의 숫자 문자열. 대상 탭 ID |
| enabled | bool, 무차원 | 필수. true/false 또는 1/0. 비활성 행은 다운로드에서 제외하며 기존 CSV/DB를 삭제하지 않음 |
| dataName / spreadsheetName / tabName / updatedAt | string, 메타데이터 | 기존 Config 생성 도구의 정보 열. Unity 파일명/경로/타입 결정에 사용하지 않음 |

- 조회: Editor 전용 SpreadConfigReader.Parse / DownloadAsync → SpreadsheetSource 목록 → 기존 SpreadsheetImporter / ProjectSpreadsheetSync. Config 자체는 게임 DB나 Generated 클래스에 넣지 않는다. 다운로드 목록은 호출/창 수명의 메모리 값이다.
- 가져오기마다 Config를 다시 다운로드한다. Config 오류·접근 실패·중복 파일명·빈 활성 목록은 실패로 표시하고 기존 데이터 유지. 문자열을 C#이나 경로로 실행하지 않는다. 비활성 행의 id/gid는 비워 둘 수 있다.
- FarmingData는 동일 Config 행으로 연결하되 기존 전용 CSV/DB/FarmingRecord로 분기한다. 비활성/미등록이면 기존 파밍 CSV를 의미 검증에 사용하고 쓰지 않는다. 공용 시트도 목록에서 빠졌다고 기존 CSV를 삭제하지 않는다. 신규 공용 연결은 기존 파서/코드 생성 사용(명세 등록은 AGENTS 규칙).
- 고급 공용 시트 미리보기는 Config의 공용 항목만 대상으로 하며 파밍 제외를 UI에 명시한다. 전체 가져오기는 공용/파밍을 함께 검증·반영한다. 기존 삭제/스키마 변경 검토와 적용 중 복구 정책 유지.
- 이전: 실제 Config 5행 및 각 URL/gid가 기존 연결과 일치함을 확인한 뒤 Unity Editor API로 새 URL을 저장하고 구 직렬화 필드를 폐기한다. 외부 Config 내용과 게임 수치는 이 작업에서 수정하지 않는다.
- 중복 조사: registry planned 및 Assets/ProjectSettings/Docs의 SpreadConfig/SpreadsheetSource/farmingSheetUrl 소비 확인. 기존 parser/downloader/DB 재사용. 게임 데이터별 ID/정본/명세는 변경 없음.
- 검증 완료: 실제 Config/5시트 다운로드·적용 및 CSV 전체 본문/BOM 대조, UI 연결 확인과 전체 가져오기 버튼, 자동 테스트 57개(기존 36개+Config 21개), 잘못된 Config/취소/중복 실행 시 5 CSV·2 DB·생성 코드 보존. [실제 검증 이력](../DataHistory/20261118-spread-config-02-verified.md).

## 로컬 설정과 수명

- 데이터 ID: `editor.data-tool`; 상태: active; 담당: Codex live-sheets-20261118; 문서 스키마 v3.
- 목적: 공용 CSV 출력 폴더와 SpreadConfig 연결 설정. 게임 데이터값이나 사용자 게임 옵션을 저장하지 않는다.
- 정본: [ProjectSettings/GameDataTool.asset](../../ProjectSettings/GameDataTool.asset).
- 타입: `GameJam.Data.Editor.DataToolSettings` (`ScriptableSingleton`). `SpreadsheetSource`는 다운로드한 연결 목록의 메모리 DTO다.
- Editor 전용 ProjectSettings 파일이다. Player 빌드 시 Library에서 생성한 StreamingAssets/GameDataSource.txt에 실행용 URL을 포함한다. 꺼짐은 빈 파일로 표시한다. Editor Play는 현재 설정을 직접 읽는다. 이 생성 파일에는 에셋 GUID/Addressables 주소가 없다.

| 필드 | 타입 | 의미 | 현재 값 / 빈 값 정책 | 실제 제약 |
|---|---|---|---|---|
| csvFolder | string | 공용 입력 폴더 | `Assets/Settings/Data/Csv` | 이 프로젝트 Assets의 하위 폴더여야 함; 가져오기 시 존재해야 함 |
| useLiveSheets | bool | 실행 시 시트 직접 읽기 | 기본 false, 구 설정에서 생략 가능 | true이면 유효한 spreadConfigUrl 필수 |
| spreadConfigUrl | string | 연결 목록을 담은 Config 문서/탭 URL | 위 SpreadConfig 탭 URL. 새 설정 기본값 빈 값, 사용 전 필수 | HTTPS docs.google.com; 대상 탭 gid 포함; 링크 열람 또는 CSV 게시 필요 |

모든 필드는 단위 없음. 소비자는 DataManagerWindow, DataImporter, SpreadConfigReader, SpreadsheetImporter, ProjectSpreadsheetSync. 창에서 수정할 때 `SaveSettings()`로 저장한다. 별도 설정 에셋이나 씬 컴포넌트에 같은 csvFolder/연결 목록을 복제하지 않는다. DataManagerWindow의 전체 가져오기 버튼은 프로젝트 Editor 어셈블리가 등록한 ProjectSpreadsheetSync.ImportAsync를 호출한다.

## 테이블 관계와 정책

각 활성 tableName은 [DataRegistry](../DataRegistry.md)에 등록한 테이블 명세와 대응해야 한다. **이 관계 검사는 현재 AI 작업 절차이며 자동 명세 검증 기능은 없다.** 시트의 헤더/키/필드 의미도 해당 로컬 테이블 계약을 따라야 한다.

`시트 → 다운로드 미리보기 → 적용 시 해당 로컬 CSV 갱신 → DB 생성`의 단방향이다. 기존 테이블이면 같은 파일을 갱신하고 새 파일명으로 복제하지 않는다. 시트 연결 제거/비활성화는 CSV/DB 행을 자동 삭제하지 않는다. 원격 시트 수정/비공개 OAuth는 제공하지 않으며 자격 증명을 여기에 저장하지 않는다.

다운로드 텍스트의 선두 BOM을 제거해 미리보기의 본문을 통일하고, CSV 파일을 쓸 때 UTF-8 BOM 하나를 추가한다. Excel 기본 열기에서 한글 인식을 돕기 위한 고정 출력 규칙이며 별도 설정 필드는 없다. 로컬 읽기는 BOM을 제외하므로 미리보기와 실제 파일의 내용 해시를 동일하게 유지한다.

소스 폴더 변경은 새로운 데이터 영역 생성이 아니다. 기존 정본을 옮기려면 GUID/원본 경로/목록과 연결을 함께 검토하고 옛 경로를 병행 정본으로 남기지 않는다.

## 중복 조사와 검증

2026-11-18 게임 개발 엔딩 추가 시 Excel이 연 CSV의 읽기를 지원하도록 공용 임포터에 `FileShare.ReadWrite`를 적용했다. 읽기 전용 접근이며 CSV 내용/설정은 변경하지 않는다. 적용 직전 전체 소스 hash 재검증은 유지한다. 근거: [임포트 수정 계획](../DataHistory/20261118-game-dev-02-import-blocked.md). 쓰기 핸들 공유 읽기를 포함한 NUnit 33개와 실제 열린 ExampleItems를 포함한 가져오기·빌드가 통과했다. [완료 이력](../DataHistory/20261118-game-dev-03-verified.md).

이전 직접 연결 방식의 URL 변환 테스트 기록은 [도구 검증 이력](../DataHistory/20261118-data-tool-02-verified.md)에 있고 실제 사용자 시트 5개의 다운로드·타입/ID/값 대조·적용은 [이전 검증](../DataHistory/20261118-notion-sheets-06-remote-verified.md)에 기록했다. 좀비 시트 게시 당시에는 SpreadConfig에 공용 4개·파밍 1개·좀비 2개가 있었다. 파밍의 전용 CSV 경로와 DB는 유지한다.

2026-11-18 예제 정리: ExampleItems 연결을 제거했다. 공용 4개 및 별도 파밍 1개 시트의 최신 내보내기와 내부 CSV가 3행 메타데이터/모든 셀에서 일치함을 확인했다. [검증](../DataHistory/20261118-example-cleanup-02-verified.md).

좀비 시트 추가 검증: 기존 Config 5행 내용 보존, 정확한 문서 ID/gid 2행 추가, 인증 없는 CSV 전체 셀 대조, 실제 전체 가져오기 7개, 동일 SO/GUID 및 공용 4테이블 유지 확인. [최종 검증](../DataHistory/20261118-zombie-sheets-03-remote-verified.md)
