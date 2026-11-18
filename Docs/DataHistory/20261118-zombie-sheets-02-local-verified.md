# 좀비 시트 로컬 연결 검증 — 20261118-zombie-sheets-02

- 프로젝트 UnityGameJam, 2026-11-18 Asia/Seoul, local-verified. [계획](20261118-zombie-sheets-01-planned.md).
- 사용/변경 ID: `data.zombies`, `editor.data-tool`. 새 게임 데이터 ID/런타임 로더/Addressables 주소 없음. 기존 5개 원격 시트는 변경하지 않았다.
- 기존 정본 SO `Assets/Settings/Shelter/ZombieData.asset`, GUID `6845f853ae90e0b479d0f51cb5d53023`, 주소 `data/zombies`, `Game Data Local`, 라벨 없음. 게시 성공 전 SO 유지. 신규 CSV는 현 SO를 그대로 내보낸 스냅샷이다.
- 스냅샷 `Assets/Settings/Shelter/Zombies.csv`/`ZombieRules.csv`, 설명/타입/필드명 3행, UTF-8 BOM. [Zombies](../DataSpecs/Zombies.md), [ZombieRules](../DataSpecs/ZombieRules.md). 게시용 xlsx `outputs/20261118-zombie-sheets/ZombieData.xlsx`, Zombies 4행 + ZombieRules 1행, artifact-tool로 값 전체 대조·렌더 확인.
- 노션 좀비 전투와 전역 상수 페이지를 로그인 브라우저로 직접 재확인. 종류/전투력/체력 및 COMBAT_TICK=5, RAID_COMBAT_TICKS=3 일치. 이동/접촉/간격은 현 SO의 임시값 보존. 새 밸런스 변경 없음.
- CsvTableReader 및 ZombieData.Validate 재사용, Editor 변환 어댑터만 추가. ProjectSpreadsheetSync가 두 시트를 함께 가져오고 기존 SO를 갱신한다. 전체 적용 실패 시 파밍과 좀비 CSV/SO를 복구한다. 고급 공용 미리보기에서 전용 테이블 제외. 콘텐츠 빌드 전 CSV/SO 일치 검사. 설치 도구의 하드코딩 seed 제거, 생성 SO Inspector 읽기 전용.
- Unity 컴파일 완료 및 실제 새 타입 실행 확인. CLI recompile 응답은 도메인 재로드 중 400이었으나 Editor 로그에 컴파일 성공/어셈블리 오류 0을 확인했고 이후 새 코드로 36개 [Edit 검사](../Zombies/Verification-sheets-edit.txt) 모두 통과.
- 검사 범위: 전체 필드 원값 보존·웨이브 경계·GUID·BOM·중복 ID·0 HP·전투력 범위·첫 웨이브 누락·중복 웨이브·빈 이름·ID 공백·스키마 변경·규칙 ID·0/NaN 간격·기존 ID 삭제·Config 양쪽 활성화·공용 테이블 제외·변경값 조회 API 반영·오류 후 SO 보존.
- Addressables 콘텐츠 빌드 성공. [Play 검사 17개](../Zombies/Verification-sheets-play.txt): 실제 씬 소비자 초기화, 공용 로더 캐시, CSV 전체 값 대조, 웨이브 1~12 ID/HP, 재시작, 원본 SO 보존 통과. Player 실행 파일은 빌드하지 않음.
- 중복 검사: 최신 Registry의 기존 `data.zombies` 작업 재사용, Assets/ProjectSettings/Docs의 좀비 필드 검색. 공용 GameDatabase에 좀비 테이블 없음, 파밍 전용 경로 유지. 기존 다른 작업 파일은 보존.
- 남은 단계: Google Drive 생성 도구 미노출로 스킬 규칙에 따라 브라우저 업로드 방식 선택 요청. 사용자가 선택한 뒤 게시, SpreadConfig 2행 등록, 실제 원격 내보내기/전체 가져오기 대조 후 정본 URL과 검증 이력을 확정한다. 원격 완료로 간주하지 않는다.
