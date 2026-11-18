# 좀비 순차 진입 계획

- 2026-11-18 Asia/Seoul / planned / Codex zombie-queue-20261118.
- 사용자 후속 요청: 여러 좀비가 순서대로 들어오며 합쳐지면 안 됨.
- 변경 데이터 ID data.zombies, 정본 Assets/Settings/Shelter/ZombieData.asset, GUID 6845f853ae90e0b479d0f51cb5d53023, 주소 data/zombies, Game Data Local 유지. 신규 minimumSpacing: float, world units, 유한 >0, 미존재 → 임시 2.4 (현재 좀비 신장 2.15보다 큰 간격). 나머지 값 보존.
- 소비 ZombieInvasionController 생성 대기열/이동 제한, ZombieAgent. ShelterWalker에는 요청된 선분 이동을 제한하는 선택적 콜백만 추가하며 사람 이동은 기존 경로 그대로 유지.
- DataRegistry planned와 Assets/ProjectSettings/Docs의 spacing/separation/간격/대기열 조사: 기존 좀비간 간격 정본 없음. encounterDistance=0.65는 사람과의 강제 전투 반경이라 의미가 달라 재사용하지 않음. 새 CSV/SO/로더 없음.
- 검증 예정: 동시 생성 독립 HP·순서, 대기 중 판정 없음, 사다리/통로 간격, 선두 전투 중 후속 정지, 선두 처치 후 진행, 점거 중 비중첩/복구, 재시작 정리, 기존 전투 회귀.
