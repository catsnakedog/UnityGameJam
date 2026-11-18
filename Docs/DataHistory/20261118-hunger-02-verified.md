# 스파게티 통조림·허기·아이템 사용

UnityGameJam · 2026-11-18 KST · Codex. 사용자가 통조림은 기존 식량과 별개라고 정정했다. [처음 조사](20261118-hunger-01-planned.md) 이후 원본 시트를 재조회해 이미 등록된 두 테이블의 공통 ID `canfood`를 찾았다. 신규 식품 ID와 별도 음식 DB를 만들지 않았다.

| 데이터 ID | 편집 정본과 변경 | 실제 소비 |
|---|---|---|
| data.farming.pickups | [FarmingData](../DataSpecs/FarmingData.md)에 연결된 Google 시트. 현재 17행, canfood 1행, 무게 1칸. 기존 FarmingDatabase.asset, GUID 117bb726d48c1a0439f32806bea7df2d, 주소 data/farming/database | FarmingSession → FarmingRunTransfer → ShelterSupplies. 기존 맵 세 호실에 캔 한 개씩 추가. 시트에서 은퇴한 여섯 ID는 기존 배치와 투입 결과 조회를 지원 |
| data.shelter.items | [ShelterItems](../DataSpecs/ShelterItems.md)에 연결된 Google 시트. 현재 11행. 공용 생성 DB의 기존 data/catalog 주소 | useKind로 허기·멘탈·장비를 구분. 장비 7종의 아이콘과 파밍 프리팹을 확인. 캔 회분, 착용 여부는 런타임 상태 |
| data.bunker.rules | [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset), GUID 01a9f70ed623c5341bec7f05cc07781d, Game Data Local의 data/bunker-rules. [필드 계약](../DataSpecs/BunkerRules.md) | BunkerSimulation, CharacterIdentity, ShelterSupplies. 기본 5틱당 −1칸, 한 입 +5칸, 새 캔 4회. 10칸에서 시작하고 과식 시 소비를 거부 |

사용 분류와 양은 각 정본에 한 곳만 있다. 파밍 캔·이벤트 캔과 초콜릿은 같은 정의로 처리한다. 이벤트 출처가 아직 등록되지 않은 장비와 통조림은 파밍으로 얻는다. 처음부터 존재하던 물·기존 식량은 허기를 채우지 않는다. 초콜릿은 현재 시트의 멘탈 회복량을 사용한다. 장비는 인물당 한 칸이며 교체·해제 시 이전 물자가 가방으로 돌아간다.

## 실제 검증

- 최신 로컬 파밍·물자 CSV의 기본키, 타입, 외래키와 BOM을 검사했다. 두 CSV를 기존 임포터로 반영하고 [Addressables 콘텐츠를 빌드](20261118-223444-1183885-data-built.md)했다. Unity 6000.3.28f1에서 Use Existing Build 모드로 실제 로드했다.
- 플레이 검사 27개 통과. 파밍 투입·씬 전환, 캐릭터 둘의 틱 경계와 하한, SO 복제본의 간격·감소·회복 변경, 포만 상태의 소비 거부, 같은 가방에 든 두 캔의 독립 회분, 네 번째 사용 후 제거, 기존 food·water 거부를 확인했다.
- 실제 게임 프리팹에 EventSystem 포인터 이벤트를 보내 UI 레이캐스트, 월드 생존자와 카드 드롭, 다른 UI로 드롭 취소, 교체·단일클릭·더블클릭, 파밍·이벤트 초콜릿, 이벤트 캔과 재고 차감, 개발 재시작 시 원래 재고·허기 복원을 확인했다. [플레이 화면](../../outputs/ShelterInventory/hunger-equipment.png)
- 최종 컴파일 오류 및 경고 0개. 검증 후 Editor는 Farming 씬의 편집 모드다. 플레이어 실행 파일 빌드와 장시간 밸런스 검사는 수행하지 않았다.
