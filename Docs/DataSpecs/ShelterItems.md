# 벙커 물자 정의

`data.shelter.items`는 벙커에서 표시하고 사용하거나 장착할 물자를 정의한다. 편집 정본은 [ShelterItems 시트](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376)다. 설명·타입·필드명 3행 뒤에 물자 12행이 있다.

| 필드 | 타입 | 의미 · 제약 |
|---|---|---|
| id | string | 필수, 소문자로 시작하고 소문자·숫자·밑줄 사용. 테이블 내 고유, 기본값 없음 |
| displayName | string | 필수 표시명. 빈 값과 공백만 있는 이름은 거부 |
| useKind | string | 필수. mental은 멘탈 물자, hunger는 통조림, attack과 buff는 장비 |
| mentalRestore | int | 필수, 0 이상. useKind가 mental인 물자를 한 번 사용할 때 회복하는 멘탈 포인트 |
| sourceEvent | string | 선택, 빈 값은 미정. 게시된 Events.id 또는 같은 ID의 파밍 물자가 있는 경우 예정된 사건 ID를 허용 |
| rank | string | 등급 표기. 현재 normal, rare, legend. FarmingSpawnLayout이 같은 파밍 ID의 rank를 조회하며 legend에 [거리순 방 가중치](FarmingRules.md)를 적용한다. |
| plusAttack | int | 필수, 0 이상. 장착자의 전투에 더하는 포인트. 소비 물자는 0 |
| plusSkill | int | 필수, 0 이상. 장착자의 기획·프로그래밍·아트 각각에 더하는 포인트. 소비 물자는 0 |

수치에 빈 값 또는 암묵적 기본값은 없다. 가방에서 초콜렛·몬스터·박카스를 사용하면 멘탈 10, 카페인 알약을 사용하면 20을 회복한다. 상한까지 필요한 만큼 회복하며 이미 가득 차면 물자를 보존한다. 이벤트는 남은 수량을 참조한다. 통조림의 허기 회복량과 회분은 벙커 규칙에서 읽는다. 카페인 알약은 파밍으로 얻으며 `ev_caffeine`은 이벤트 기획용 참조다.

## 조회와 수명

- [CSV 스냅샷](../../Assets/Settings/Data/Csv/ShelterItems.csv)은 UTF-8 BOM이며 GUID는 `0e9e62a0321a0c845b15da3e342d2b28`이다. 기존 Data Manager가 GameDatabase와 DataTables를 생성한다. 런타임은 `data/catalog`의 `GameData.GetTable("ShelterItems")` 및 `ShelterItemCatalog.Definition(id)`를 사용한다. [가져오기](../DataManager.md)
- 기존 이벤트 효과와 requiredItem은 동일한 ID를 참조한다. 원격 시트와 로컬 스냅샷 갱신 시 Events와 [FarmingData](FarmingData.md)를 대조한다. 항목 삭제나 ID 변경 시 모든 이벤트·파밍·가방·장착 참조를 먼저 전환한다. 표시명만 변경하면 ID를 보존한다.
- `ShelterSupplies`가 파밍 투입 및 이벤트 보상의 회차 재고를 관리한다. 인물 한 명이 장비 하나를 장착하며 교체·해제 시 기존 개체가 가방에 돌아간다. 캐릭터 카드, 개발 틱, 전투 판정은 기본 능력과 현재 장비 보정을 합산한다. 장비 해제 시 기본 능력을 사용한다.
- 사용 횟수·현재 재고·장착 상태는 런타임 값이다. 재시작하면 파밍 투입분에서 복구한다. 파밍 무게와 벙커 사용 효과를 각각 기존 시트 한 곳에서 편집한다. 중복 조사와 검증 범위는 [확정 목록 이력](../DataHistory/20261118-item-roster-02-verified.md)에 기록한다.
