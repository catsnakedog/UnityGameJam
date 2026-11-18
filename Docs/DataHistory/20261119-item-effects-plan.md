# 벙커 아이템 효과 적용 계획 · 2026-11-19

아이템 능력 수정 요청에 따라 `data.shelter.items`의 12개 행을 가방 사용 경로와 대조했다. 초콜렛·몬스터·박카스는 모두 mental/+10, 카페인은 mental/+20이다. 현재 `ShelterSupplies.TryUse`가 `data.bunker.rules.usableMentalSupplyIds`의 choco와 caffeine만 허용해 음료 두 종의 효과가 차단된다. 장비 7종에는 전투 또는 개발 보정이 있고, 통조림은 벙커 규칙의 허기 회복과 회분을 사용한다.

가방에서 소비 분류와 효과는 기존 ShelterItems 정의로 결정한다. 별도 허용 목록은 사용·표시·초기 설치에서 함께 제거한다. 기존 ID·시트·CSV·DB·주소를 재사용한다. Assets, ProjectSettings, Docs와 데이터 목록의 예약을 대조했으며 신규 테이블이나 로더가 필요하지 않다. 검증 대상은 파밍 투입분과 이벤트 재고, 회복 상한에서의 소비, 장비 장착·해제, 캔의 남은 회분이다. [물자 명세](../DataSpecs/ShelterItems.md) · [기존 규칙](../DataSpecs/BunkerRules.md)
