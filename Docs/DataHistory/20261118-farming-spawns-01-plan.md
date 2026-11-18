# 파밍 스폰 변경 계획 · 2026-11-18 (KST)

UnityGameJam에서 파밍을 시작할 때마다 기존 세 호실에 사람과 물자를 다시 배정해 달라는 요청을 반영한다. 이전 [물자 확정 기록](20261118-item-roster-02-verified.md) 이후 저장된 Farming 씬에는 6명과 중복 물자를 포함해 총 29개 개체가 있다. 방마다 책상 6개와 복도로 연결되는 문이 있다.

| 데이터 ID | 정본과 현재 값 | 변경 계획 |
|---|---|---|
| data.farming.rules | [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset), GUID `22168f09c24cbfb49aeb55b136dd0298`, Farming Local / farming / `data/farming/rules`. 기존 필드 값은 [명세](../DataSpecs/FarmingRules.md)에 기록 | 기존 SO에 거리순 인원 3·2·1과 레전드 상대 가중치 1·2·3을 추가한다. 가중치는 첫 조정값이다. |
| data.farming.pickups | [FarmingData 명세와 원본](../DataSpecs/FarmingData.md), Person 6행, Item 12행 | ID·종류·무게를 재사용한다. 통조림만 방당 하나씩 생성한다. |
| data.shelter.items | [ShelterItems 명세와 원본](../DataSpecs/ShelterItems.md), rank 중 legend 2행 | 동일 물자 ID의 등급을 조회한다. |

DataRegistry의 예약과 Assets·ProjectSettings·Docs에서 방별 가중치·인원수·등급·소비자를 조사했다. 방별 규칙은 기존 정본에 없으며 등급과 물자 목록은 이미 등록되어 있다. 프리팹은 ID별 외형, 책상은 위치만 제공한다. 씬 시작 후 ID별 개수, 통로와 책상 좌표, 방 배정 확률, 재입실과 재시작을 검증한다.
