# 파밍 복도 튜토리얼 방송 · 2026-11-19

- 변경한 데이터는 `data.shelter.dialogue`다. 편집 정본 [ShelterDialogue.csv](../../Assets/Settings/Data/Csv/ShelterDialogue.csv)에 주최측 방송 14행을 추가했다. 각 행은 안내 문구, 순서, 강조할 화면을 지정한다. 기존 행과 스키마는 그대로다. [필드 및 소비자](../DataSpecs/ShelterDialogue.md)
- `data.farming.rules`의 [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset)에서 시작 인물 ID를 읽어 FarmingRunTransfer.BeginRun에 전달한다. [파밍 규칙](../DataSpecs/FarmingRules.md)
- DataRegistry의 예약과 프로젝트 파일에서 원문·별칭·소비자를 검색했다. 기존 대사 표가 역할과 상황에 따른 문장을 이미 관리하므로 같은 표와 공용 DB를 재사용했다. Events의 라디오 선택지는 독립된 이벤트다.
- Data Manager가 14행의 필드 추가만 검출했다. CSV는 UTF-8 BOM을 유지했다. [DB 적용](20261119-134023-3509953-data-applied.md) · [Addressables 콘텐츠 빌드](20261119-134101-8384560-data-built.md)
- Play Mode에서 타이틀 버튼을 눌러 씬에 진입하고 모든 행을 CSV와 대조했다. 설정 팝업 중에는 같은 문장이 유지됐고, 마지막 클릭 후 Farming의 Preparing 상태와 기본 인물 person_01을 확인했다. [화면 명세와 검수](../UI/Tutorial.md)
