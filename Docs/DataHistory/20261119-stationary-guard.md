# 보초 제자리 방어와 좀비 접근 거리

기존 `data.bunker.rules.invasionApproachTicks`를 1에서 6으로 변경했다. 편집 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이다. 현재 0.5초/틱과 초당 2월드 단위에서 선두의 철문까지 이동 거리는 6월드 단위다. 보초는 철문 앞 캔 투척과 돌파 후 근접전 모두 자기 위치에서 공격하며 좀비 쪽에서 거리를 좁힌다.

DataRegistry의 예약과 Assets·ProjectSettings·Docs의 spawnOffset, invasionApproachTicks, encounterDistance 및 소비자를 확인했다. 새 데이터 필드 없이 접근 시간과 기존 좀비 시트의 접촉 반경을 재사용했다. 생성 거리·도착 전 무피격·공격자의 제자리 유지·접촉 후 근접전에 대한 기존 검사 코드를 갱신했다. 플레이 검사는 생략했다. [벙커 규칙](../DataSpecs/BunkerRules.md) · [좀비 명세](../DataSpecs/Zombies.md)
