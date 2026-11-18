# character-link 적용 및 검증

- 프로젝트 UnityGameJam / 2026-11-18 Asia/Seoul / Codex. 선행: 20261118-character-link-01-planned.md.
- data.characters 정본: Assets/Settings/Data/Csv/Characters.csv (UTF-8 BOM), person_01~06. programming=1/art=2/planning=7/mental=10은 사용자 예시를 적용한 임시값. 이름 MIRA/JIN/NOA/PERSON 04~06. [명세](../DataSpecs/Characters.md).
- system.game-database 기존 생성물 Assets/Settings/Data/GameDatabase.asset, GameJam.Data.GameDatabase, GUID 62a272592b807b3469b072879c349759, 주소 data/catalog, 그룹 Game Data Local, 라벨 없음. 공용 DataImporter로 Characters 추가, 기존 엔딩/ExampleItems 유지. DataTables.cs 재생성. 별도 SO/로더/주소 복제 없음.
- data.farming.pickups: Assets/Settings/Farming/FarmingData.csv, Person ID 관계만 추가하고 본문/무게/스키마 유지. 명세 FarmingData.md 갱신. data.farming.rules 값 변경 없음.
- 연결: 공용 CSV→DB→GameData→CharacterCatalog, FarmingSession 시작 시 1:1 Person 검증→투입 ID 스냅샷→ShelterRoster→기존 Survivor 프리팹→CharacterIdentity. 기존 예제 3명은 비활성 보존, ENTRY에 직렬화된 spawn point 6개 설치. 멘탈은 인스턴스 상태로 원본 불변.
- Unity 6000.3.28f1 컴파일 completed/failed=false/errors=[] 확인. 공용 Addressables 콘텐츠 빌드 성공.
- [Edit 5개](../CharacterLink-edit.txt): BOM, 음수, 물자 ID, 고아/누락 Person, 중복 ID 거부.
- [Play 40개](../CharacterLink-play.txt): 실제 Farming→Shelter 씬 전환에서 2/0/6명 명단, 중복 투입/물자/미투입 대상 제외, CSV 능력·멘탈 일치, 멘탈 변경 원본 불변, 생성 캐릭터 이동 명령 경로 수락, 벙커 재로드 인원 유지, 새 파밍 초기화, 직접 벙커 실행 0명.
- 테스트는 세션 API로 투입/시간 종료를 구동했다. 이번 작업에서 60초 실제 키보드 운반 플레이나 목적지까지 걷는 전체 시간은 재검증하지 않았다. Player 실행 파일 빌드/영구 저장/능력별 개발속도/멘탈 증감 게임 규칙은 수행하지 않음.
- 적용 직전 목록 재확인 및 의미 중복 검색: 기존 캐릭터 능력/멘탈 정본 또는 타 작업 예약 없음. 파밍 무게·용량, GameDev 진행도와 의미 구분. 파밍 데이터의 공용 폴더 복제 없음.
