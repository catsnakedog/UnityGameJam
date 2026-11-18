# 플레이 데이터 소스 선택 계획

UnityGameJam · 2026-11-18 23:25 KST · Codex · planned

Data Manager에서 선택한 소스로 게임을 시작하도록 기존 `editor.data-tool`을 확장한다. 선택 필드가 없는 기존 프로젝트는 내부 CSV로 생성한 데이터를 사용한다. 시트 모드에서는 기존 SpreadConfig의 활성 연결을 읽고 한 실행 동안 사용할 데이터를 메모리에서 검증한다. 수동 가져오기와 로컬 CSV/DB의 저장은 별도 동작이다.

- 정본과 조회 계약은 [도구 설정](../DataSpecs/DataToolSettings.md), [공용 DB](../DataSpecs/GameDatabase.md), [파밍](../DataSpecs/FarmingData.md), [좀비](../DataSpecs/Zombies.md)에 있다. 기존 데이터 ID, 행, 수치, 주소는 유지한다.
- Assets, ProjectSettings, Docs와 목록의 planned 항목에서 실행 시 소스 선택 필드나 동등한 소비 경로를 찾지 못했다. SpreadConfig 파서, 다운로드, CSV 타입/ID 검사 및 전용 SO를 재사용한다.
- 현재 원격 Config 조회 결과는 활성 6행이다. 좀비 종류가 `ZombieData`로 연결돼 있고 `ZombieRules` 행은 없다. 공용 폴더에도 기존 `ZombieData.csv`가 있으나 실제 전투 소비자는 전용 SO다. 이 별칭은 같은 다운로드를 재사용하고 미연결 규칙은 내부 데이터를 사용한다. 과거 7탭 형식도 지원한다. 기존 복제 파일의 삭제나 외부 시트 편집은 수행하지 않는다.
- 검증 예정 범위는 소스 선택, 실행 간 초기화, 활성 시트 소비, 연결/검증 실패, 내부 파일 보존이다. [기존 원격 연결 이력](20261118-zombie-sheets-03-remote-verified.md)
