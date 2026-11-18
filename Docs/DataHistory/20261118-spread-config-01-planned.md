# SpreadConfig 연결 계획

- 작업 ID: Codex spread-config-20261118 / 2026-11-18 KST.
- 요청: 사용자가 수정한 SpreadConfig의 파일명과 URL을 사용하도록 Unity 가져오기 기능 추가.
- 변경 대상 ID: 기존 editor.data-tool. 연결 목록은 사용자 SpreadConfig로 이전하며 로컬 ProjectSettings에는 Config URL과 CSV 폴더만 저장한다. 기존 4개 URL 목록 및 farmingSheetUrl의 별도 편집 정본 폐기.
- 실제 확인한 Config: https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615 . 1행 dataName,sheetName,id,gid,enabled,spreadsheetName,tabName,updatedAt. 활성 5행(GameDevEndings/ShelterItems/FarmingData/Events/Characters), 기존 URL/탭과 동일.
- 전체 버튼은 매번 Config → 활성 데이터 시트 → 검증 → UTF-8 BOM CSV/기존 클래스 및 DB 갱신. FarmingData 분기는 유지한다. 비활성/누락 연결은 기존 로컬 데이터를 삭제하지 않는다.
- 이전 값: ProjectSettings/GameDataTool.asset의 공용 sheets 4개, farmingSheetUrl 1개, spreadsheetEnabled=true. 이후: spreadConfigUrl 및 csvFolder. Config 원격 내용/게임 콘텐츠 값 변경 없음.
- 공용 Editor 코드: Assets/Scripts/Data/Editor 재사용. 프로젝트별 의미 검증/파밍 분기는 Assets/Editor/ProjectSpreadsheetSync.cs 유지. 테스트는 기존 Assets/Scripts/Data/Tests/Editor.
- 유지 생성물: Assets/Settings/Data/GameDatabase.asset (data/catalog, GUID 62a272592b807b3469b072879c349759), Assets/Settings/Farming/FarmingDatabase.asset (data/farming/database, GUID 117bb726d48c1a0439f32806bea7df2d), 기존 DataTables.cs/FarmingRecord 및 5개 CSV 경로.
- 현재 명세: ../DataSpecs/DataToolSettings.md. 개별 데이터 명세/정본은 registry 참조. 중복 조사 및 입력 계약은 명세에 기록했다.
- 검증 전 계획이며 적용 성공을 의미하지 않는다. 후속 verified/failed 이력에 결과를 추가한다.
