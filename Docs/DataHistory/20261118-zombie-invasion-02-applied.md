# 좀비 침입 적용 — 20261118-zombie-invasion-02

- 프로젝트 UnityGameJam, 2026-11-18 Asia/Seoul, applied. 이전 [계획](20261118-zombie-invasion-01-planned.md).
- data.zombies: ZombieData, Assets/Settings/Shelter/ZombieData.asset, GUID `6845f853ae90e0b479d0f51cb5d53023`, 주소 `data/zombies`, 기존 Game Data Local, 라벨 없음. 편집 정본 SO 하나, [전체 스키마·값](../DataSpecs/Zombies.md). 소비 ZombieInvasionController → ZombieAgent/ZombieCombat. 프리팹 Assets/Prefabs/Shelter/Zombie.prefab GUID `18da28686bc1349439f96c4fa971a718`은 외형/참조만 소유.
- data.bunker.rules: 기존 경로/GUID/주소 유지. mentalMaximum 미존재→100, faintDurationTicks 미존재→40, wakeMental 미존재→30. totalTicks120/secondsPerTick1/randomEventEveryTicks10/invasionEveryTicks30/workEveryTicks1/percentPerAbility0.1/planningUnlockPercent50 보존. CharacterIdentity/BunkerSimulation 공용 소비.
- data.characters, data.events: 기존 시트→CSV→GameDatabase/data/catalog(62a272592b807b3469b072879c349759) 정본·생성 경로·값 유지. 능력과 멘탈/다음 습격 HP의 소비 연결만 확장. 공용 GameData 캐시·핸들 소유 재사용.
- 최초 생성 시 EditorJsonUtility 초기화가 빈 데이터를 남겨 Validate 실패. 게임 시작 전 JsonUtility 초기화로 같은 GUID 에셋을 복구하고 재검증. 설치 도구는 신규 생성 전 Validate하도록 수정. 다른 데이터는 이 실패에서 변경하지 않음.
- 적용 직전 최신 DataRegistry의 파밍 맵/예제 정리 작업도 확인: 이번 담당과 의미 중복 없음. 새 CSV/저장 키/로더 없음.
- 검증: Unity 컴파일 성공, 실제 SO 읽기/4종 구간/주사위 승패/동점/중복 ID 거부 Edit 검사 6개 통과. Addressables 콘텐츠 Default 프로필 빌드 성공, Use Existing Build 지정. 실제 Play 검증은 다음 이력에 기록 예정. 실행 파일 빌드하지 않음.
