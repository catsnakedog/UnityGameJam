# 좀비 접근 거리와 보초 사격 간격 변경 계획

UnityGameJam · 2026-11-18 23:38 KST · Codex · 사용자 요청

기존 [접근·침입 기록](20261118-zombie-approach-verified.md)과 현재 정본에서 접근 15틱, 보행 속도 초당 2유닛, 틱당 1초, 공용 전투 간격 5틱을 확인했다. 방 입구에서 선두 좀비까지의 거리는 30유닛이었다.

기존 `data.bunker.rules`의 `invasionApproachTicks`를 6으로 조정하고, 같은 에셋에 보초의 원거리 사격 주기 `guardVolleyEveryTicks`를 2로 등록한다. 두 필드는 필수 양의 정수이며 단위는 벙커 틱이다. 생성 후 2·4·6틱의 사격, 마지막 사격과 침입 순서, 이후 근접전 5틱, 다중 좀비의 대기열을 검증한다. [정본 및 조회 계약](../DataSpecs/BunkerRules.md)

Registry의 등록·예약과 Assets, ProjectSettings, Docs에서 접근 거리, 원거리·근접 공격, 다음 사격 틱을 검색했다. 기존 `data.zombies.combatEveryTicks`가 두 공격에 쓰였으며 보초 사격만 조정할 필드는 없었다. 보행 속도와 근접전 주기는 기존 [ZombieRules 시트 계약](../DataSpecs/ZombieRules.md)을 계속 사용한다.
