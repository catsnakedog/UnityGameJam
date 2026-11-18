# 파밍과 벙커 변경 통합 검증 · 2026-11-19

Unity 6000.3.28f1 메인 프로젝트에서 프리팹과 규칙을 저장하고 Default 프로필의 Windows Addressables 콘텐츠를 재빌드했다. 기존 번들을 사용하는 Editor Play로 파밍에서 벙커까지 확인했다. [콘텐츠 빌드 기록](20261119-012857-7905477-data-built.md)

| 데이터 ID | 편집 정본·소비처 |
|---|---|
| `data.bunker.rules` | [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset) → BunkerSimulation.Rules. 수면실 +5/틱, 전투방 −2/틱, 구조 후 각성 50점. 기존 전체 960틱·이벤트 40틱·습격 80틱·허기 10틱 설정으로 확인했다. [명세](../DataSpecs/BunkerRules.md) |
| `data.farming.rules` | [FarmingRules.asset](../../Assets/Settings/Farming/FarmingRules.asset) → FarmingDataLoader.Rules. 시작 운반 person_01과 325·324·323호 인원 2·2·1. [명세](../DataSpecs/FarmingRules.md) |
| `data.shelter.items` | 기존 ShelterItems 시트 → 공용 DB → ShelterItemCatalog.Definition. 가방과 실제 사용에서 같은 분류·회복량·장비 보정을 읽는다. [정본과 명세](../DataSpecs/ShelterItems.md) |
| `data.farming.pickups`, `data.characters` | 기존 FarmingData 시트의 사람 ID·운반 비용과 Characters 시트의 인물 정의·초기 멘탈을 재사용했다. [파밍 명세](../DataSpecs/FarmingData.md) · [인물 명세](../DataSpecs/Characters.md) |

등록 목록과 Assets·ProjectSettings·Docs의 기존 소비자를 대조했다. 멘탈 소모·회복과 시작 운반 설정은 기존 SO 두 곳에 반영했다. 가방의 멘탈 소비 가능 목록은 ShelterItems의 분류로 통합했다. 새 데이터 ID나 CSV·로더 추가는 0건이다.

## 실행 결과

- 파밍 Play 108개 검사 통과. 시작 인물의 일반 운반 비용 2/4칸, 방별 인원, 캐릭터별 원화, 플레이어 대기·걷기, 내려놓기·재획득·투입, 실제 30초 종료와 미투입자 제외를 확인했다. 추가로 실제 재시작 후 배치와 인벤토리를 확인하고 시작 인물만 투입해 벙커에 1명으로 진입했다. 조작자 외형에는 전용 걷기 클립이 없어 1번의 클립을 적용했다. 5·6번은 벙커와 같이 기본 정지 그림을 사용한다. [파밍 검사](../Farming/Verification-SideView.txt) · [시작 화면](../../outputs/Farming/starting-carry.png)
- 벙커 Edit 11개, Play 46개 검사 통과. 41틱 대기와 외부 멘탈 회복 후에도 인계 전 기절 상태를 유지했다. 다른 층의 구조자가 클릭 명령으로 환자에게 접근해 2인 엘리베이터를 타고 수면실에 인계했다. 멘탈 0에서 9틱 후 45, 10틱 후 50에서 행동을 재개했다. 운반 자세와 구조자 기절 시 환자 인계 해제도 확인했다. [벙커 검사](../Bunker-play.txt)
- 카메라 Play 69개 검사 통과. WASD 네 방향, 화살표·마우스·줌·추적과 팝업 차단, 실제 좀비 생성 시 시점·줌 유지를 확인했다. 좀비 전투 Play 50개 및 대기열 Play 18개 검사도 통과했다. [카메라 검사](../ShelterCamera-checks.txt) · [전투 검사](../Zombies/Verification-play.txt)
- 가방 Play 29개 검사 통과. 실제 슬롯 드래그와 캐릭터 카드 레이캐스트를 통해 물자 12종을 검사했다. 파밍 투입분·이벤트 재고의 멘탈 물자 4종, 상한에서 보존·상한 직전 단일 소비, 장비 7종의 개인 능력 보정과 해제, 통조림의 허기·회분 처리를 확인했다.
- HUD 편집 검사 8개와 Play 검사 12개 통과. 시계는 0·480·960틱에서 00:00·12:00·24:00, 원형 채움은 0·50·100%였다. 일시정지·재시작, 세 개발 게이지의 개별 값, 멘탈 100·60·1·0일 때 이름과 머리 위 채움량을 확인했다. 1920×1080 화면에서 시각 숫자와 링의 중앙 정렬을 확인했다. [벙커 화면](../../outputs/SurvivorAnimation/shelter-hud-and-mental.png)

검증 후 Editor는 Farming 씬 편집 모드다. 검증 범위는 로컬 Addressables와 Editor Play까지다.
