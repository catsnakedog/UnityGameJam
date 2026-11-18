# 파밍 회차별 무작위 배치 · 검증 완료

UnityGameJam, 2026-11-18~19 KST. [변경 계획](20261118-farming-spawns-01-plan.md)에 따라 Farming 씬의 고정 개체 29개를 회차 시작 시 생성하는 20개체로 전환했다. 복도 벙커의 투입점에서 바깥 방문까지 거리는 325호 6, 324호 15, 323호 24 월드 단위다. 각 방의 책상 6개가 상판 자리 12개와 책상 사이 통로 자리 5개를 제공한다.

| 데이터 ID | 편집 정본 및 조회 | 변경 |
|---|---|---|
| `data.farming.rules` | [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset), [필드 명세](../DataSpecs/FarmingRules.md). GUID `22168f09c24cbfb49aeb55b136dd0298`, 주소 `data/farming/rules`, Farming Local 그룹, farming 라벨 | `RoomSpawns`에 벙커 거리순 인원 3·2·1명과 레전드 가중치 1·2·3을 추가했다. 가중치는 첫 밸런스 값이다. |
| `data.farming.pickups` | [FarmingData 시트·연결 명세](../DataSpecs/FarmingData.md), `FarmingDataLoader.Database.Records` | 기존 18행의 ID·종류·운반 비용을 재사용한다. 물자 12종 중 통조림만 각 방에 한 개씩 생성한다. |
| `data.shelter.items` | [ShelterItems 시트·연결 명세](../DataSpecs/ShelterItems.md), `GameJam.Data.Generated.ShelterItems.Table.Get(id).rank` | 기존 등급을 읽어 레전드 물자마다 방을 독립 추첨한다. 같은 방에 두 레전드가 나올 수 있다. |

등록 목록의 예약, Assets·ProjectSettings·Docs의 기존 규칙 및 소비자를 확인했다. 기존 FarmingRules 한 곳에서 방별 규칙을 편집한다. Farming Session은 ID별 프리팹 18개를 참조하며 수량·등급 목록은 만들지 않았다. 새 CSV·SO·로더·저장 키는 없다.

## 실제 검증

- 원본 씬을 다시 열어 고정 스폰 0개, 고유 프리팹 참조 18개, 방마다 상판 12자리와 저장된 방별 규칙을 확인했다. 같은 파밍 소스와 씬을 사용한 별도 Unity 6000.3.28f1 Editor에서 Windows Addressables 콘텐츠를 빌드하고 Use Existing Build로 플레이했다. 빌드 입력 16개와 출력 7개를 대조해 원본 프로젝트의 로컬 콘텐츠 캐시에도 반영했다.
- 실제 배치 함수를 시드 0~2999로 실행했다. 모든 회차에서 사람 6명·통조림 3개·다른 물자 11개였고, 인원은 3·2·1명, 통조림은 각 방에 한 개였다. 한 회차에서 자리 중복이 없었고 통조림은 각 방 상판 12자리를 모두 사용했다. 레전드 6,000개 중 325호 1,017개, 324호 1,987개, 323호 2,996개였다.
- [사이드뷰 Play 검사 107개](../Farming/Verification-SideView.txt)를 통과했다. 책상·통로 위치, 방 가시성, 문 왕복, 운반 용량, 벙커 투입, 30초 종료 및 재입실 시 미재생성을 확인했다.
- 세션의 실제 Restart 후 이전 회차와 위치 배정이 달라졌고, 인벤토리가 비었으며 20개체 모두 획득 가능한 상태였다. 세 방을 다시 순회해 각 개체의 바닥 좌표에서 20개 모두 해당 개체가 상호작용 대상으로 선택되는지 확인했다. 검증 범위는 Editor Play와 Windows Addressables 콘텐츠다.
