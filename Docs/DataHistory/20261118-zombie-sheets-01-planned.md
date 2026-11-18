# 좀비 시트 연결 계획 — 20261118-zombie-sheets-01

- 프로젝트 UnityGameJam, 2026-11-18 Asia/Seoul, planned / Codex zombie-sheets-20261118.
- 요청: Spread에 빠진 좀비 데이터를 노션 기준 초기값으로 추가하고 기존 가져오기에 연결.
- 기존 `data.zombies`를 재사용한다. `Zombies`(4종 카탈로그)와 `ZombieRules`(공통 규칙 1행)를 Google Sheets 정본으로 전환할 예정이다. 신규 게임 데이터 ID/런타임 로더/Addressables 주소는 만들지 않는다.
- 예정 스냅샷 경로: `Assets/Settings/Shelter/Zombies.csv`, `Assets/Settings/Shelter/ZombieRules.csv`, UTF-8 BOM, 설명/타입/필드명 3행 양식. 두 테이블의 명세는 각각 [Zombies](../DataSpecs/Zombies.md), [ZombieRules](../DataSpecs/ZombieRules.md).
- 생성 대상은 기존 `Assets/Settings/Shelter/ZombieData.asset`, GUID `6845f853ae90e0b479d0f51cb5d53023`, 주소 `data/zombies`, 그룹 `Game Data Local`, 라벨 없음. 공용 GameDatabase에 중복 등록하지 않는다.
- `CsvTableReader`로 파싱/타입 검증하고 기존 `ZombieData.Validate`로 전체 의미 검증한다. 기존 SO 소비자/공용 GameData 수명을 유지하기 위해 Editor 변환 어댑터만 추가한다.
- 현재 SO 값과 노션 좀비 전투 원문을 대조: zombie_1/좀비/1/1/2, zombie_2/덩치 좀비/4/2/4, zombie_3/성난 좀비/7/3/5, zombie_4/거대 좀비/10/4/6 (ID/표시명/시작 웨이브/전투력/HP).
- 공통 값: diceSides=6, combatEveryTicks=5, attemptsPerDefender=3, damageToZombie=1, mentalDamage=2, mentalRecovery=5. 기존 이동 임시값 walkSpeed=2, climbSpeed=1.6, encounterDistance=0.65, spawnOffset=1.2, minimumSpacing=2.4 보존.
- 중복 조사: DataRegistry의 active/planned, Assets/ProjectSettings/Docs의 zombie/좀비/firstWave/combatEveryTicks/minimumSpacing 검색. 기존 ZombieData와 설치 코드의 최초 seed만 발견. 설치 seed는 CSV 생성 경로로 교체하며 SO를 읽기 전용 생성물로 표시한다. 캐릭터/벙커 규칙/이벤트 보정과 겹치는 필드는 추가하지 않는다.
- 전체 다운로드·검증 이후 적용, 실패/취소 시 기존 CSV/SO 유지. 두 좀비 시트는 함께 활성화하거나 함께 제외한다. 기존 5개 시트 연결은 유지한다.
- 원격 게시/실제 가져오기/컴파일/콘텐츠 빌드/Play 검증은 아직 미실행. 성공 전에는 기존 SO가 정본이다.
