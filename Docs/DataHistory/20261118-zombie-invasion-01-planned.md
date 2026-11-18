# 좀비 침입 계획 — 20261118-zombie-invasion-01

- 프로젝트 UnityGameJam, 2026-11-18 Asia/Seoul, planned, 담당 Codex zombie-invasion-20261118.
- 요청: 노션 주사위 전투, 전원 멘탈 0 또는 n×3회 후 생존 시 침입, 실제 경로 강제 전투, 방 점거와 해방.
- 신규 data.zombies: Assets/Settings/Shelter/ZombieData.asset, ZombieData, data/zombies, Game Data Local, GUID 미생성. 값과 모든 소비자/수명은 [명세](../DataSpecs/Zombies.md).
- 확장 data.bunker.rules: 기존 Assets/Settings/Shelter/BunkerRules.asset, GUID 01a9f70ed623c5341bec7f05cc07781d, data/bunker-rules / Game Data Local 보존. 추가 mentalMaximum=100, faintDurationTicks=40, wakeMental=30. 이전 필드 없음; 기존 모든 값 유지. CharacterIdentity/BunkerSimulation 소비.
- 재사용 data.characters: 기존 시트→Characters.csv→GameDatabase/data/catalog. Combat/CurrentMental 소비 추가, 정본 값·ID 변경 없음.
- 재사용 data.events: 기존 시트→Events.csv→같은 GameDatabase. NEXT_WAVE_HP 소비를 실제 좀비 생성에 연결, 이벤트 수치 변경 없음.
- 데이터 목록·Assets/ProjectSettings/Docs 중복 검색: 기존 좀비 정본/예약 없음. 주기·캐릭터 능력·멘탈 초기값·HP 보정·경로 시스템은 재사용. 계획 시 컴파일/런타임/빌드 미검증.
