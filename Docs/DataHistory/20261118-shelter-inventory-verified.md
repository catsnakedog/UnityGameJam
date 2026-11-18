# 벙커 인벤토리 연동 검증

2026-11-18 · Unity 6000.3.28f1 · ShelterPrototype / Farming 실제 Editor Play.

## 사용한 데이터

| ID | 역할 | 정본과 명세 |
|---|---|---|
| `data.farming.pickups` | 투입한 개체의 ID·종류·운반 비용. 투입 결과에서 수량 집계 | Google Sheets FarmingData → `Assets/Settings/Farming/FarmingData.csv`. [명세](../DataSpecs/FarmingData.md) |
| `data.shelter.items` | 이벤트 재고의 ID·이름·멘탈 회복량. 현재 수량은 씬의 ShelterSupplies | Google Sheets ShelterItems → `Assets/Settings/Data/Csv/ShelterItems.csv`. [명세](../DataSpecs/ShelterItems.md) |

목록의 planned와 Assets·ProjectSettings·Docs에서 물자 ID, 인벤토리, 운반 상태, 이벤트 소비자와 저장 경로를 확인했다. 수정 범위는 기존 파밍 인계와 벙커 조회 소비자다. 새 물자 DB·수량 저장소·저장 키·시트·로더는 추가하지 않았다.

## Play 결과

- 벙커 직접 시작 시 가방은 닫혀 있었다. 가방 버튼을 열었을 때 빈 16칸과 빈 상태 문구를 확인했다. 파밍 씬에는 가방이 없었다.
- 파밍 씬의 실제 개체로 `Deposit`을 호출하고 타이머 종료 후 `EnterShelter`로 전환했다. 물 2, 의료품 1, 라디오 2가 벙커의 다섯 칸에 표시됐다. 같은 물 개체에 대한 중복 투입 호출, 들고만 있던 식량, 별도의 구조 명단으로 전달된 사람은 물자 수량에 포함되지 않았다.
- 가방을 연 상태에서 `ev_door` 보상으로 초콜렛이 한 칸 늘고 `ev_party` 소비로 다시 사라졌다. 툴팁은 실제 보유 수량과 기존 회복량·운반 비용을 표시했다.
- Play 중 재고를 31개로 설정해 16칸과 15칸으로 나뉘는지, 둘째 페이지에서 9개로 줄이면 첫 페이지로 돌아오는지 확인했다. 가방 닫기·다시 열기, 개발 재시작 후 이벤트 물자 초기화와 파밍 투입분 유지도 확인했다.
- 1920×1080, 1440×1080에서 포인터와 실제 Game View로 버튼·아이템 그림·호버·화면 여백을 확인했다. 가방 PNG 한 픽셀을 변경해 재임포트한 후 원본을 복원하고 Sprite GUID와 local ID, 씬 버튼 참조가 유지됨을 확인했다. [가방 화면](../UI/ShelterInventory.md)
