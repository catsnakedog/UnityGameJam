# 데이터 관리 도구

## 게임 실행 시 데이터 선택

**Tools → Data Manager → 실행 시 스프레드시트 실시간 연동**에서 선택한다. 기본값은 꺼짐이다.

| 선택 | 게임 실행 동작 |
|---|---|
| 켜기 | 게임을 시작하면 SpreadConfig와 활성 시트를 다시 읽는다. 공용 데이터, 파밍, 좀비를 검증한 뒤 이번 실행의 게임 데이터로 사용한다. 씬 전환 중에는 같은 값을 사용하며 다음 실행 때 시트 편집이 반영된다. |
| 끄기 | Google Sheets 접속 없이 내부 CSV에서 생성한 기존 Addressables DB로 시작한다. 내부 CSV를 갱신할 때는 기존 수동 가져오기 절차를 사용한다. |

Editor Play는 현재 토글을 읽고, Player는 빌드 시 선택한 모드로 실행한다. 시트 읽기는 로컬 CSV·DB·접근자를 쓰지 않는다. 연결 또는 값 검증이 실패하면 초기화 오류에 원인을 표시한다. 현재 Config에는 활성 6행이 있고 좀비 종류의 연결 이름은 `ZombieData`다. 연결되지 않은 `ZombieRules`는 내부 규칙을 사용한다. [설정 명세](DataSpecs/DataToolSettings.md)

## 좀비 시트 연결 (2026-11-18 완료)

[Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193)(종류 4행)와 [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)(공통 규칙 1행)를 게시하고 SpreadConfig에 연결했다. 기존 5개 연결을 유지하여 게시 당시 총 7개 탭이었다. [종류 명세](DataSpecs/Zombies.md), [규칙 명세](DataSpecs/ZombieRules.md).

두 행을 정확한 파일명으로 함께 활성화하고 **전체 시트 가져오기**를 누른다. `Assets/Settings/Shelter/Zombies.csv`와 `ZombieRules.csv`를 갱신하고 같은 `ZombieData.asset`에 생성한다. 공용 GameDatabase로 복사하지 않는다. 생성 SO Inspector는 읽기 전용이다. 공용 고급 미리보기는 파밍·좀비를 제외하므로 전용 SO 데이터에는 전체 가져오기를 사용한다. 잘못된 값·ID 삭제·한쪽 시트만 활성화는 적용 전에 차단하고, 적용 실패 시 CSV/SO를 복구한다. 콘텐츠 빌드 전에도 좀비 CSV/SO 일치를 확인한다.

기획자용 [게임 데이터 시트와 CSV 양식](https://docs.google.com/document/d/1wl9JY-Iiv316ucy_pDvmogp1xr20tYZP/edit)에 기존 5개 시트의 30개 필드·타입·입력 제약·효과 문법·Unity 반영 절차를 정리했습니다. 콘텐츠 편집 정본은 각 Google Sheets이며 이 문서는 양식 설명입니다. [문서 반영 이력](DataHistory/20261118-data-reference-document.md).

AI가 데이터를 추가·수정할 때는 먼저 [데이터 목록과 중복 방지 규칙](DataRegistry.md)에서 기존 정본을 확인하고 연결된 명세를 읽습니다. 새 테이블은 [명세 양식](DataSpecs/_Template.md) 작성과 목록 등록을 먼저 마칩니다. 필드/ID/관계 변경은 같은 작업에서 명세와 이력을 갱신합니다. 프로젝트 진입 지침은 [AGENTS.md](../AGENTS.md)입니다.

Unity 메뉴 **Tools → Data Manager**에서 **전체 시트 가져오기 · CSV / 클래스 갱신**을 누릅니다. Google Sheets가 유일한 편집 원본입니다. Drive에 별도 CSV를 저장할 필요가 없습니다.

1. Google Sheets에서 내용과 수치를 편집합니다.
2. Unity의 전체 시트 가져오기 버튼을 누릅니다. SpreadConfig를 다시 읽고 현재 활성 시트를 내려받아 타입·ID·관계를 검증한 뒤 내부 CSV, 타입 접근 클래스, 기존 DB를 갱신합니다.
3. 배포용 데이터를 바꿀 때는 **Addressables 콘텐츠 빌드**를 누릅니다. 스키마 변경 시 Unity의 스크립트 컴파일 완료를 기다립니다.

버튼 실행 중 취소할 수 있으며 Play 중에는 가져오기를 막습니다. 파일명과 시트 연결은 [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615)에서 관리합니다. Unity 창에서 실행 시 연동 토글, **SpreadConfig URL (탭 포함)**과 CSV 폴더를 저장합니다. 공용 `DataTables.cs`는 자동 생성하고 파밍은 기존 `FarmingRecord` 클래스로 파싱합니다. 아래 로컬 CSV 검증과 미리보기는 고급 작업용입니다.

### SpreadConfig 작성

Config는 **1행 헤더 / 2행부터 연결 목록**이며 게임 시트의 3행 메타데이터 양식과 다릅니다.

| 필드 | 의미 |
|---|---|
| sheetName | 출력 파일명. 예: Events 또는 Events.csv. 경로 입력 금지, 대소문자 무시 중복 금지 |
| id | 원본 Google 스프레드시트 문서 ID |
| gid | 대상 탭 ID. 주소의 gid 값을 입력 |
| enabled | TRUE/1이면 가져오기, FALSE/0이면 제외. 제외해도 기존 로컬 데이터는 삭제하지 않음 |
| dataName / spreadsheetName / tabName / updatedAt | Config 생성 도구의 정보 열. Unity 파일명·저장 경로 결정에는 사용하지 않음 |

`id`와 `gid`로 `https://docs.google.com/spreadsheets/d/{id}/edit?gid={gid}`를 구성합니다. 현재 Config의 빈 시트1(gid=0)이 아니라 **SpreadConfig 탭(gid=961703615)** 주소를 연결했습니다. 새 연결은 Config에 추가하면 다음 버튼 실행부터 반영됩니다. 신규 데이터 명세 등록 및 실제 게임 소비자 연결은 별도로 필요합니다.

**시트 연결 설정 / 고급 미리보기 → SpreadConfig 연결 확인**으로 파일 변경 없이 현재 목록을 볼 수 있습니다. 목록은 읽기 전용이며 설정 복제본으로 저장하지 않습니다. **공용 시트 미리보기 (파밍·좀비 제외)**는 공용 테이블만 검토합니다. 파밍·좀비를 포함한 갱신은 전체 가져오기 버튼을 사용합니다. Config 오류 시 예전 로컬 URL 목록으로 폴백하지 않습니다.

CSV는 **UTF-8 BOM 포함**으로 저장해 Excel에서 파일을 더블클릭하여 열 수 있게 합니다. 시트 가져오기 결과도 같은 형식으로 저장합니다. Excel에서 편집 후에는 `CSV UTF-8(쉼표로 분리)` 형식으로 저장하세요. `.xlsx`는 현재 가져오기 대상이 아닙니다. 이미 열린 파일은 닫았다 다시 열어야 변경된 인코딩 표시가 적용됩니다.

Excel의 `DefaultCPG=65001` 설정만으로 일반 CSV 열기의 UTF-8 인식을 보장하지 않습니다. 현재 설치 버전에서 기본값 설정 후에도 BOM 없는 CSV는 깨졌고, BOM 포함 CSV는 정상으로 확인했습니다. [로컬 Excel 설정 및 검증 기록](DataHistory/20261118-csv-excel-03-default-setting.md)을 참고하세요.

1. `Assets/Settings/Data/Csv`에 CSV를 넣습니다. 다른 Assets 하위 폴더도 지정할 수 있습니다.
2. **CSV 검증 / 미리보기**로 타입, ID, 변경 전후 값과 테이블을 확인합니다.
3. **미리보기 적용 · DB / C# 갱신**으로 ScriptableObject DB와 타입 접근 코드를 생성합니다.
4. 배포 데이터가 필요하면 **Addressables 콘텐츠 빌드**를 실행합니다. Player 실행 파일 빌드와는 별도입니다.

도구 예제 `ExampleItems.csv`는 삭제했습니다. 게임 데이터로 다시 생성하지 않습니다. 기존 `Assets/Settings/Farming/FarmingData.csv`와 Farming 로더는 이 도구로 자동 이전하지 않습니다.

현재 데이터 계약: [Characters](DataSpecs/Characters.md), [Events](DataSpecs/Events.md), [ShelterItems](DataSpecs/ShelterItems.md), [GameDevEndings](DataSpecs/GameDevEndings.md), [DB 컨테이너](DataSpecs/GameDatabase.md), [도구 설정](DataSpecs/DataToolSettings.md). 이름이 같은 필드라도 단위/의미를 먼저 대조합니다. 의미 중복과 명세 존재 여부는 현재 AI가 문서를 기준으로 검사하며, 임포터가 자동 차단하지는 않습니다.

엔딩의 활성 0점 행·중복 점수 등 의미 검사는 `Tools > Game Dev > Import Ending CSV`에서 적용 전에 함께 수행합니다. 공용 창으로 적용했다면 `Tools > Game Dev > Validate Ending Rules and UI`로 검사하세요. Excel에서 열어 둔 CSV도 공유 읽기를 허용하면 가져올 수 있으며, Excel의 미저장 편집은 반영되지 않습니다.

## CSV 형식

현재 모든 게임 시트와 내부 CSV는 **1행 설명 / 2행 타입 / 3행 필드명 / 4행부터 데이터**로 통일했습니다. 열 순서와 셀 값을 그대로 유지하며 내부 파일은 UTF-8 BOM으로 저장합니다. 아래 단일 헤더/Idler 예시는 기존 파일 호환 규칙입니다.

파일 이름이 테이블 이름입니다. 하위 폴더를 포함해 이름이 중복되면 오류입니다. 파일 이름과 열 이름은 영문/숫자/밑줄을 사용하며 숫자로 시작할 수 없습니다. `~`, `.`, `#`로 시작하는 파일은 무시합니다.

간단한 형식:

```csv
id:string,displayName:string,price:int,weight:float,enabled:bool,tags:string[]
wood,목재,10,1.5,true,material|wood
stone,돌,20,2.5,true,material|stone
```

- 헤더는 `이름:타입`. 타입을 생략하면 `string`입니다. 자동 타입 추론은 하지 않습니다.
- 지원 타입: `string`, `int`, `long`, `float`, `double`, `bool` 및 각 타입의 `[]` 배열.
- 배열은 `|`로 구분합니다. 빈 배열은 빈 셀입니다. 배열 원소 안의 `|` 이스케이프는 지원하지 않습니다.
- 기본 키는 대소문자를 무시해 찾은 `id` 열입니다. 다른 열을 사용하려면 `listId!:int`처럼 `!`를 붙입니다. 키 타입은 string/int/long입니다.
- 숫자는 소수점 `.`을 쓰며 빈 숫자, 범위 초과, NaN/Infinity는 오류입니다. bool은 true/false 또는 1/0입니다.
- 키는 비어 있거나 앞뒤 공백이 있으면 안 됩니다. 숫자 키 `01`과 `1`은 같은 키입니다. ID와 조회 문자열은 대소문자를 구분합니다.
- UTF-8/BOM, 쉼표·줄바꿈이 들어간 따옴표 필드, `""` 이스케이프를 지원합니다.
- 완전히 빈 행과 첫 셀이 `#`로 시작하는 데이터 행은 무시합니다. 기본 헤더의 빈 열과 `#` 열은 무시합니다.
- 자동 생성 접근자의 내부 이름인 `Row`, `_dataRow`는 열 이름으로 사용할 수 없습니다.

Idler의 3줄 형식도 그대로 읽습니다. 첫 줄 첫 셀이 `#`로 시작하면 설명/타입/변수명 형식으로 판단합니다.

```csv
#변수 설명,고유 번호,표시 이름,가격
#데이터 타입,int,string,int
#변수명,id,displayName,price
,1,목재,10
,2,돌,20
```

설명·타입·변수명 중 하나가 `#`로 시작하거나 타입/변수명이 비어 있는 열은 제외합니다. CSV의 행 순서를 열거 순서로 유지합니다. Idler의 `ItemData_ItemDefaultData.csv`는 `ItemData_ItemDefaultData`라는 한 테이블이 됩니다. Idler의 바이너리/Resources 로더 의존성은 필요하지 않습니다.

## 코드에서 조회

게임 시작 흐름에서 한 번 비동기 초기화한 뒤 사용합니다. Unity 메인 스레드에서 호출하세요. 초기화 전 접근은 원인을 담은 예외를 던지며 임의 값으로 대체하지 않습니다.

```csharp
using GameJam.Data;

await GameData.InitializeAsync();

var characters = GameData.GetTable("Characters");
var person = characters.Get("person_01");
int planning = person.Get<int>("planning");
UnityEngine.Debug.Log(person.Get<string>("displayName"));
```

자동 생성된 타입 접근자:

```csharp
var person = GameJam.Data.Generated.Characters.Table.Get("person_01");
int planning = person.planning;
foreach (var character in GameJam.Data.Generated.Characters.Table.All)
    UnityEngine.Debug.Log(character.displayName);
```

DB는 `data/catalog` 주소로 한 번 로드됩니다. 동시에 초기화를 요청하면 같은 Task를 공유합니다. 테이블/ID/열 딕셔너리를 만들고 셀 타입을 한 번 변환하므로 반복 조회마다 CSV를 읽거나 리플렉션으로 변환하지 않습니다. 조회 결과와 배열은 읽기 전용이며 플레이 상태는 별도 객체에 보관합니다. 씬을 바꾸어도 캐시를 유지합니다.

로드 실패 시 `GameData.LastError`와 초기화 Task에 오류가 전달됩니다. 이후 InitializeAsync로 재시도할 수 있습니다. 애플리케이션 종료 시 자동 해제하고 Domain Reload를 끈 Play 재진입도 초기화합니다. 명시적 `GameData.Release()`는 모든 소비자가 사용을 마친 뒤 호출하세요. Release 이후에는 기존 행 참조를 재사용하지 말고 다시 초기화·조회합니다.

## Google Sheets 선택 연결

### 2026-11-18 제공 양식 지원

Drive 예시와 같은 **1행 설명, 2행 타입, 3행 변수명, 4행 데이터** 형식도 지원한다. 별도의 `#` 표식 열 없이 첫 열부터 id를 둘 수 있다. 기존 `이름:타입` 단일 헤더와 Idler 형식도 유지한다. 변수명과 타입이 모두 유효하고 id/명시 키가 있는 경우에만 새 양식으로 인식한다. 일반 id 단일 헤더는 우선한다. 다운로드한 Google Sheets CSV의 TRUE/FALSE도 기존 bool 파서로 읽는다.

`FarmingData`도 SpreadConfig의 같은 연결 목록에 포함된다. sheetName=FarmingData 행의 id/gid에서 다운로드하여 **기존 FarmingData.csv 경로**에 저장하고 기존 Farming 전용 임포터를 실행한다. 공용 CSV 폴더/DB에 중복 등록하지 않는다. 전용 임포터의 Person/Item 무게 제약을 유지한다.

좀비 시트를 게시한 시점에는 게임 데이터 Google Sheets 문서 6개, 총 7개 탭을 관리했다. 공용 4개와 파밍 1개, ZombieData 문서 안의 좀비 2개 탭을 SpreadConfig에 등록했다. 편집은 시트에서 하고 전체 시트 가져오기 버튼으로 내려받는다. FarmingData도 버튼 안에서 기존 전용 경로/임포터를 사용한다. 테스트 CSV 22개와 중간/테스트 xlsx 27개는 휴지통으로 옮겼다. `outputs/20261118-notion-data`의 xlsx는 이번 업로드용 산출물이며 편집 원본이 아니다. 자동 양방향 동기화가 아니다. [기획 대조 및 연결 상태](NotionDevelopmentCheck.md)를 확인한다.

현재 연결 목록은 SpreadConfig만 편집합니다. 구 Google Sheets 사용 토글·개별 URL 목록·별도 farmingSheetUrl은 제거했습니다. **공용 시트 미리보기 (파밍·좀비 제외)**는 다운로드·검증만 수행합니다. **미리보기 적용**을 눌러야 공용 CSV와 DB가 갱신됩니다. 비활성·누락 연결의 기존 로컬 테이블은 유지합니다.

- 링크로 열람 가능한 Google Sheets 또는 CSV로 웹 게시된 시트를 지원합니다.
- 비공개 시트 OAuth 로그인은 현재 제공하지 않습니다. 접근 불가·HTML 로그인 응답·네트워크 오류·잘못된 CSV는 실패로 표시합니다.
- 가져오기는 단방향이며 원격 시트를 수정하지 않습니다. 연결된 시트가 편집 정본이고 로컬 CSV는 실행용 DB 생성에 쓰는 스냅샷입니다. 다음 시트 가져오기를 적용하면 해당 로컬 CSV 편집은 시트 값으로 대체됩니다.
- 전체 다운로드와 검증이 끝나기 전에는 로컬 파일을 쓰지 않습니다. 취소와 실패 시 기존 파일을 유지하며, 적용 중 실패하면 CSV/DB/생성 코드의 이전 상태를 복원합니다.
- 설정은 `ProjectSettings/GameDataTool.asset`의 useLiveSheets/spreadConfigUrl/csvFolder에 저장합니다. 신규 프로젝트에서는 Config URL을 입력해야 합니다.
- Google의 시트 접근 정책 참고: [Google Sheets 데이터 접근](https://developers.google.com/chart/interactive/docs/spreadsheets).

## 변경 보호와 산출물

미리보기 이후 CSV 또는 DB가 바뀌면 적용을 거부하고 새 미리보기를 요구합니다. 누락된 테이블·행·열과 타입/키 변경은 기본적으로 적용을 차단합니다. 미리보기에서 확인한 뒤 **삭제 및 스키마 변경 허용**을 켜면 해당 변경을 적용합니다. 빈 폴더 전체 가져오기는 항상 거부합니다.

| 위치 | 용도 |
|---|---|
| `Assets/Settings/Data/Csv` | 시트에서 가져온 공용 CSV 스냅샷 |
| `Assets/Settings/Data/GameDatabase.asset` | 생성 DB, Inspector 읽기 전용 |
| `Assets/Scripts/Data/Generated/DataTables.cs` | 타입 접근자, 콘텐츠 값은 포함하지 않음 |
| `Assets/Scripts/Data/Editor` | 관리 창, 검증/가져오기/생성 도구 |
| `Assets/Scripts/Data/Tests/Editor` | EditMode 회귀 테스트 |
| `Docs/DataHistory` | 계획·적용·실패·콘텐츠 빌드의 추가형 기록과 전체 DB 스냅샷 |

기존 DB를 갱신할 때 GUID와 논리 주소를 보존합니다. 숫자나 표시 이름만 수정하면 접근자 소스가 바뀌지 않습니다. 새로운 열/테이블 또는 타입 변경 시에는 생성된 C#을 컴파일합니다. SO를 직접 편집해 CSV와 이중 관리하지 마세요.

## 선택형 이벤트 데이터

[Events v2](DataSpecs/Events.md) 및 [ShelterItems](DataSpecs/ShelterItems.md)는 공용 CSV 경로를 사용한다. `Tools > Events > Import Event CSV`는 효과 문법·아이템 외래키·발생 조건까지 검사한 뒤 공용 적용 절차를 호출한다. 일반 Data Manager 적용 이후에도 런타임 카탈로그가 의미 검사를 수행한다. 효과 배열은 `|`로 구분하며 쉼표가 포함된 CSV 셀은 큰따옴표로 감싼다. 적용 후 Addressables 콘텐츠를 빌드한다.
