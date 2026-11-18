# 확정 아이템 적용 전 대조

- 편집 정본은 기존 [벙커 물자](../DataSpecs/ShelterItems.md)와 [파밍 대상](../DataSpecs/FarmingData.md) 시트다. 벙커 CSV는 11행 6열, 원격은 12행 8열이다. 추가 행은 카페인 알약이고 장비 보정 두 열과 통조림의 이벤트 ID가 갱신됐다.
- [노션 이벤트](https://app.notion.com/p/41caca30f5dc808a9c2bcda8f0c01844)와 Events 시트의 12개 사건에는 ev_caffeine이 없다. 사용자가 알약의 파밍 획득과 기존 두 음료의 보관 전용 동작을 확인했다. 이벤트의 문구나 보상을 새로 만들지 않는다.
- 기존 FarmingData는 사람 6종과 물자 11종이다. 폐기된 water, food, medkit, radio, toolbox, blanket은 활성 시트에서 빠졌지만 저장된 Farming 씬에 각 두 개가 있고 FarmingDatabase가 은퇴 목록으로 제공한다. 기존 배치와 인벤토리 아트 참조를 전환한 후 해당 프리팹과 호환 행을 제거한다. 파밍 투입 기록의 수명은 현재 플레이 회차다.
- DataRegistry의 예정 항목, Assets·ProjectSettings·Docs의 ID, 장비 수치 소비처와 프리팹 GUID를 대조했다. data.shelter.items, data.farming.pickups 및 data.bunker.rules를 재사용한다. 장착자의 전투·개발 능력은 캐릭터 기본값과 장착 중인 물자의 보정값으로 계산한다.
