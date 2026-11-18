# 2026-11-18 좀비 접근 및 실내전 계획

사용자가 15틱 접근, 5틱마다 보초별 공격, 실내 진입 즉시 교전, 전원 멘탈 −2, 배치된 보초 8틱마다 −1을 지정했다. 이전 판정 횟수 제한을 폐기한다.

DataRegistry의 planned 항목과 Assets, ProjectSettings, Docs의 접근 거리·보초 멘탈·침입 손실·시간·소비자를 대조했다. 기존 `data.bunker.rules`가 습격 시계와 인원 멘탈을, `data.zombies`가 공용 속도와 전투 주기를 소유한다. 기존 근접 피해, 작업자 멘탈, 사건 멘탈은 따로 적용된다.

정본은 기존 [벙커 규칙 SO](../../Assets/Settings/Shelter/BunkerRules.asset)와 [좀비 시트 명세](../DataSpecs/ZombieRules.md)다. SO의 기존 GUID와 주소를 유지하고 기존 시트의 attemptsPerDefender와 spawnOffset은 호환 열로 보존한다. 새 CSV·로더·저장 키를 만들지 않는다. 컴파일, 콘텐츠 빌드, 틱 경계·일시정지·다인전·대기열 검증 예정.
