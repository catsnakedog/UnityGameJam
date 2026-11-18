# 확정 아이템 12종 반영 결과

2026-11-18 KST. [적용 전 대조](20261118-item-roster-01-plan.md)에서 확인한 차이를 반영했다. 사용자는 카페인 알약의 파밍 운반 비용을 1칸으로 정하고 몬스터와 박카스는 보관 전용으로 유지하도록 했다.

| 관련 데이터 | 정본 및 결과 |
|---|---|
| `data.shelter.items` | [ShelterItems 시트](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376), [명세](../DataSpecs/ShelterItems.md). 12행·8열을 기존 data/catalog 및 생성 접근자에 반영했다. caffeine은 rare, mental, 회복량 20. 장비 7종은 plusAttack·plusSkill을 사용하고 통조림의 예정 출처는 ev_canfood다. |
| `data.farming.pickups` | [FarmingData 시트](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264), [명세](../DataSpecs/FarmingData.md). caffeine, Item, 1을 정본에 추가했다. 기존 전용 DB data/farming/database에 사람 6종과 물자 12종을 반영했다. |
| `data.bunker.rules` | [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset), [명세](../DataSpecs/BunkerRules.md). 기존 허용 목록에 caffeine을 추가했다. choco와 caffeine은 드래그 사용, monster와 bacchus는 보관 전용이다. |
| `data.events` · `data.characters` | [이벤트 명세](../DataSpecs/Events.md), [캐릭터 명세](../DataSpecs/Characters.md). 기존 12개 사건과 캐릭터 기본값을 재사용한다. 출처 이벤트가 아직 없는 물자는 파밍에 동일 ID가 있을 때 검사를 통과한다. 카페인 알약은 현재 파밍으로 획득하며 ev_caffeine 이벤트는 미게시다. |

## 배치와 중복 검사

구 물자 water, food, medkit, radio, toolbox, blanket을 쓰던 325호 테이블의 12개 자리를 기존 배치 도구의 시트 순서대로 교체했다. 나머지 호실 배치와 호실당 통조림 하나를 유지했다. 여섯 프리팹 및 전용 아트 18개의 GUID가 씬·가방·다른 에셋에서 더 쓰이지 않는 것을 확인한 뒤 제거했다. 은퇴 행의 무게 사본도 폐기했다. 파밍 도구와 벙커에서 같은 ID를 사용하며 신규 CSV, 데이터베이스, 로더, Addressables 주소를 추가하지 않았다.

## 검증

- 세 원격 시트를 다시 내려받아 벙커 12행·8열, 파밍 18행·3열, 이벤트 12행·7열의 모든 헤더와 셀이 로컬 UTF-8 BOM CSV와 같은지 비교했다. 기존 두 임포터로 아이템만 적용했다.
- Unity의 가져오기 검증에서 미게시 ev_caffeine은 파밍 행으로 해석된다. 파밍 행도 없는 경우를 만들어 거부를 확인했다. 은퇴 ID 6개는 DB 조회에서 모두 거부됐다. 기존 이벤트 Edit 검사 12개를 통과했다.
- [Addressables 콘텐츠 빌드](20261118-230137-1235105-data-built.md) 후 Unity 6000.3.28f1의 Use Existing Build로 플레이했다. 저장된 파밍 씬의 물자 23개가 확정 12종을 모두 포함했고, 각 물자의 Sprite와 알약 1칸을 확인했다. 12종을 투입하고 생존자 6명과 벙커로 전환했다.
- 벙커에서 알약은 멘탈 40→60으로 회복하고 재고가 1→0이 됐다. 몬스터·박카스는 사용을 거부하고 각각 한 개를 유지했다. 초콜렛은 10을 회복했다. 가방에서 알약 아이콘과 +20 툴팁을 확인했다.
- 장비 7종을 차례로 장착해 네 능력, 교체 시 이전 개체 반환, 해제 시 기본값 복원을 확인했다. 게이밍 노트북 장착 후 실제 기획 틱에 +2, 소방 도끼 장착 후 보초 전투 주사위 합에 +3이 반영됐다. 재시작 후 최초 투입한 알약 한 개가 복원됐다.
