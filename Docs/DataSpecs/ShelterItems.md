# 벙커 물자 정의

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.shelter.items`. 편집 정본: [ShelterItems](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376).
- 로컬 스냅샷: `Assets/Settings/Data/Csv/ShelterItems.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager에서 전체 시트 가져오기 · CSV / 클래스 갱신을 누른다. 고급 변경은 미리보기 후 적용한다. 연결 목록은 SpreadConfig의 sheetName/id/gid/enabled에서 관리하며 Unity에는 Config URL만 저장한다. [연결 명세](DataToolSettings.md).
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기로 3행의 모든 셀/헤더를 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

## 현재 계약 v2 — 노션 소비 정의 (2026-11-18)

기존 CSV/DB/ID/수명을 유지하며 다음 필드를 확장한다. 아래 v1의 "소비 효과를 정의하지 않음"은 이번 계약으로 대체된다.

| 필드 | 타입 | 필수/빈 값/기본값/단위/제약 |
|---|---|---|
| useKind | string | 필수, 기본값 없음. 현재 닫힌 소비 분류 mental |
| mentalRestore | int | 필수, 기본값 없음, 0 이상의 회복 포인트. 노션 3종 모두 10 |
| sourceEvent | string | 선택, 빈 값=획득 이벤트 미정, 기본값 빈 문자열. 값이 있으면 Events.id 참조 |

기존 id/displayName 제약은 유지한다. ShelterItemCatalog.Definition(id)에서 전체 정의를 조회하며 Name/Contains API는 유지한다. sourceEvent 관계는 콘텐츠 임포트 검사에서 확인한다. 소비 드래그 UI와 기절 해제는 미구현이며 데이터 추가만으로 완료 처리하지 않는다. AED는 노션 ID/획득처 미정과 기절 해제 문구 충돌을 대조 보고서에 남기고 임의 행을 만들지 않는다. 이벤트가 참조하는 기존 세 물자와 FarmingData의 운반 비용은 같은 데이터가 아니다.

출처: [노션 개발 요구사항](https://app.notion.com/p/41caca30f5dc80239e4dd03a4d451a46). [계획 이력](../DataHistory/20261118-notion-sheets-02-content-plan.md), [실제 로드·관계·이벤트 회귀 검증](../DataHistory/20261118-notion-sheets-03-local-verified.md). 편집 원본은 상단 Google Sheets, 기존 CSV는 내려받은 스냅샷으로 유지.

- ID `data.shelter.items`, active / Codex event-effects-20261118, v1, 2026-11-18.
- 소유: 벙커 소비 물자 ID와 표시명. 정본 `Assets/Settings/Data/Csv/ShelterItems.csv`. 기존 공용 GameDatabase/Generated DataTables 및 GameData, data/catalog 사용. 별도 로더/주소 없음. CSV GUID `0e9e62a0321a0c845b15da3e342d2b28`.
- id:string: 필수, 영문 소문자 식별자, 테이블 내 고유, 기본값 없음. displayName:string: 필수 일반 텍스트, 공백만 불가, 기본값 없음. 단위 없음. ShelterItemCatalog에서 검증.
- 행: choco=초콜렛, monster=몬스터, bacchus=박카스. 사용자 명시 물자. 무게/가격/소비 효과를 여기서 정의하지 않음.
- 현재 수량은 ShelterSupplies의 런타임 상태(개, 정수 >=0). 시작/개발 재시작 시 0개. 편집 정본에 런타임 재고를 쓰지 않는다. Changed/GetCount/SetCounts/Add로 소비, 씬 수명. 영구 저장 없음.
- Events의 ITEM 대상 및 requiredItem은 이 ID를 참조. 삭제 시 Events 전체 참조 검증 필수. id 변경 시 모든 참조를 함께 이전하고 기존 ID 재사용 금지.
- 중복 조사: DataRegistry의 planned 포함, Assets/ProjectSettings/Docs의 choco/monster/bacchus/inventory/Item 검색. FarmingData는 water/food/medkit/radio/toolbox/blanket의 운반 비용 정본이며 동일 물자 아님. 기존 FarmingInventory는 운반 중 슬롯 상태로 벙커 재고와 다름. ExampleItems는 도구 예제. 자동 food→choco 매핑 및 FarmingData 복제 없음.
- 갱신: 기존 Data Manager/Events Import 메뉴. 임포트 전 참조 검증, UTF-8 BOM. GameData 앱 캐시/해제 소유, UI에서 Release하지 않음.
- 검증: 공용 임포트·콘텐츠 빌드·실제 Play 증감·재고 부족·참조 검증·재시작 초기화 완료. [이력](../DataHistory/20261118-event-effects-02-verified.md).
