# 벙커 가방

벙커에서 우측 하단 가방 버튼을 누르면 아이콘 바로 위에 보유 물자가 열리고, 같은 버튼으로 닫는다. 한 물자 개체가 한 칸을 차지한다. 화면을 열고 있는 동안에도 벙커 시간은 흐른다.

| 요소 | 배치와 동작 |
|---|---|
| BagButton | 1920×1080 게임 영역의 우측 하단. 가방 그림으로 열기·닫기 |
| InventoryPanel | 우측 여백 32, 가방 버튼 위 여백 12, 폭 312. 기존 개발 진행 HUD 위, 이벤트·엔딩 팝업 아래 |
| Slots | 2열×2행. 4개를 넘으면 휠 또는 위·아래 버튼으로 한 줄씩 스크롤 |
| Tooltip | 아이템에 마우스를 올리면 가방 왼쪽의 같은 행에 이름과 효과를 표시한다. 장비는 전투력·개발력 증가량, 소모품은 회복량과 보유량 또는 남은 사용 횟수를 표시한다. 스크롤하면 현재 칸의 내용으로 바뀌고, 칸을 벗어나거나 드래그하거나 가방을 닫으면 숨긴다. |

게임의 기존 16:9 논리 영역과 한글 TMP 폰트를 사용한다. 처음 여는 빈 가방에는 빈 상태 문구를 표시한다.

## 데이터와 편집 위치

점유 슬롯을 생존자의 월드 그림이나 열린 캐릭터 카드에 끌면 그 인물에게 적용한다. 다른 UI·빈 공간에 놓으면 취소한다. 초콜렛·몬스터·박카스·카페인 알약 툴팁은 멘탈 회복량을 표시한다. 통조림에는 허기 회복량과 남은 회분이 표시된다. 이미 가득 찬 경우 물자를 유지한다. 장비 교체와 해제 결과는 가방에 즉시 반영된다.

- `FarmingSession.EnterShelter`에서 투입 완료한 Item을 중복 수량까지 전달한다. Person은 기존 구조 명단으로 간다. 파밍에서 확인한 `FarmingRecord`의 ID·운반 비용을 그대로 참조한다. [파밍 데이터](../DataSpecs/FarmingData.md)
- `ShelterSupplies`가 벙커 이벤트 재고와 파밍 투입 결과의 읽기 인터페이스를 제공한다. 이벤트 보상·소비·재시작과 같은 `Changed` 알림으로 열린 가방을 갱신한다. [벙커 물자](../DataSpecs/ShelterItems.md)
- 화면은 `Assets/Prefabs/UI/ShelterInventory.prefab`, 칸은 `Assets/Prefabs/UI/ShelterInventorySlot.prefab`에서 편집한다. 슬롯 클릭으로 물자를 변경하는 동작은 없다.
- 공용 패널·슬롯·버튼의 9-slice 이미지는 `Assets/Art/UI/Farming`에서 참조한다. 가방과 물자 그림은 `Assets/Art/UI/Inventory`의 48×48 투명 PNG이며 호버 모서리는 32×32, 8px 9-slice이다. 모두 Single Sprite, Point, 100 PPU, mipmap off다. 교체할 때 같은 경로와 크기로 PNG를 덮어쓰고 .meta를 유지한다.
- 프리팹의 Artwork에서 물자 ID와 같은 이름의 Sprite를 연결한다. 현재 12종의 한글 이름과 사용 효과는 ShelterItems에서 읽고, 보유 수량은 현재 회차의 재고에서 읽는다. 그림이 없는 항목도 슬롯과 툴팁에 이름을 표시한다.

## 확인한 화면

[1920×1080 가방과 카드](../Images/BunkerBagAndCard.png) · [1440×1080 레터박스](../../outputs/ShelterInventory/bunker-bag-4x3.png) · [재고 연동 검증](../DataHistory/20261118-shelter-inventory-verified.md)
