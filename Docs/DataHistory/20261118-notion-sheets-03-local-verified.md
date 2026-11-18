# 노션 콘텐츠·스프레드시트 로컬 검증 — 원격 연결 대기

- 작업: Codex notion-sheets-20261118. 이전 계획 [01](20261118-notion-sheets-01-planned.md), [02](20261118-notion-sheets-02-content-plan.md).
- 전체 완료가 아니다. Drive 업로드/삭제, Google Sheets URL/탭 등록, 노션 원격 체크, 미연결 규칙의 데이터화·게임 연결은 남아 있다. 최신 [대조 보고서](../NotionDevelopmentCheck.md)에 원문과 상태를 구분했다.

## 변경 데이터와 정본

| ID | 정본 | 변경/명세 |
|---|---|---|
| data.characters | Assets/Settings/Data/Csv/Characters.csv | 노션 이름·능력·초기 멘탈, designId/role/combat/description 추가. [명세](../DataSpecs/Characters.md) |
| data.shelter.items | Assets/Settings/Data/Csv/ShelterItems.csv | useKind/mentalRestore/sourceEvent 추가. 소비 UI 미연결. [명세](../DataSpecs/ShelterItems.md) |
| data.events | Assets/Settings/Data/Csv/Events.csv | 12행 내용/효과 유지, 시트 변환 및 새 물자 스키마와 회귀 검사. [명세](../DataSpecs/Events.md) |
| data.game-dev.endings | Assets/Settings/Data/Csv/GameDevEndings.csv | 임시안 유지, 시트 변환. [명세](../DataSpecs/GameDevEndings.md) |
| data.example-items | Assets/Settings/Data/Csv/ExampleItems.csv | 예제 역할 명시, 값 유지, 시트 변환. [명세](../DataSpecs/ExampleItems.md) |
| data.farming.pickups | Assets/Settings/Farming/FarmingData.csv | 값/경로 유지. 전용 임포터의 3행 시트 형식 지원. [명세](../DataSpecs/FarmingData.md) |
| system.game-database | 위 공용 CSV 5개에서 생성 | 기존 DB/접근자 갱신, GUID/주소 유지. [명세](../DataSpecs/GameDatabase.md) |
| editor.data-tool | ProjectSettings/GameDataTool.asset | URL 미등록/기존 설정 유지. 3행 헤더 지원과 미연결 상태 문서화. [명세](../DataSpecs/DataToolSettings.md) |

캐릭터 person_01~06은 기존 파밍/씬 참조를 보존하고 designId로 노션 ID에 대응한다. 필드 추가는 기존 테이블 안에서 이루어졌다. 신규 게임 데이터 정본 없음. 데이터 목록을 적용 전후 다시 읽었고 동일 의미의 planned 예약 없음. 파밍은 공용 CSV/DB에 복제하거나 중복 등록하지 않았다. BunkerRules 임시값과 오디오 경로를 변경하지 않았다.

## 실제 검증

- Unity 컴파일 오류 0, 경고 0.
- 공용 DataToolTests NUnit 36/36 통과: 기존 typed-header/Idler, 잘못된 CSV/ID/타입, 새 표식 없는 3행 시트, 빈 설명 행, 일반 헤더 오인식 방지 포함.
- 공용 CSV 5개를 제공 양식 CSV로 변환 후 Unity의 CsvTableReader로 다시 읽어 타입·열·행 ID·모든 값(배열/숫자/bool 포함) 동일 확인.
- FarmingData 시트의 12행을 기존 전용 파서로 읽어 ID/종류/무게 동일 확인. 중복 ID·무게 범위 오류·잘못된 종류 거부 확인.
- CharacterLinkChecks.RunEdit 및 EventPopupChecks.RunEdit 통과.
- 기존 Addressables 콘텐츠 빌드 완료. 실제 Play 로드에서 캐릭터 6명의 구조 ID/노션 별칭/이름/네 능력/초기 멘탈 60 확인. 물자 3종의 회복량 10 및 획득 이벤트 참조 확인(총 9개 검사).
- EventPopupChecks.RunPlay 67개 통과. 24개 Yes/No 분기의 정확한 효과, 조건, 선택 중복 방지, 결과 표시, 정지/재개, 재시작 포함. 기존 다음 습격 보정 API의 회귀 검증만 했으며 좀비 기능을 추가한 것은 아니다.
- 최신 CSV 6개·38행의 xlsx 생성. artifact-tool 렌더를 실제 확인했고, 별도의 Python ZIP/XML 읽기로 저장된 xlsx의 스키마/전체 셀값/타입/3행 틀 고정을 검증했다. 변환 시작/검증 시 CSV SHA256 동일. 이벤트 따옴표/한글/효과 배열 보존.
- Player 실행 파일 빌드 없음. Google Sheets 서버 변환·실제 원격 재다운로드·OAuth·자동 동기화는 미검증/미실행. 미구현 게임 기능은 대조 보고서에 명시.

## 다음 연결 단계

Drive 플러그인 또는 사용자가 명시적으로 선택한 브라우저 방식의 인증 후, 검증된 xlsx를 지정 폴더의 네이티브 Sheets로 가져온다. 원격 기존 테스트 CSV는 실제 파일을 다시 식별하고 정리한다. `SpreadConfig파일(삭제X)`와 양식 문서를 보존한다. 업로드 성공 후 실제 URL/탭을 기록하고 CSV→시트 준비 상태에서 시트→CSV→기존 DB 흐름으로 정본 정책을 전환한다. Farming은 계속 별도 전용 임포터를 사용한다. 노션 체크는 검증된 항목만 반영한다.
