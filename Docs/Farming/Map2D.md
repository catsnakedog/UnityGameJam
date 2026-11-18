# 파밍 2D 사이드뷰 맵

2026-11-18 사용자 결정으로 아이소메트릭 맵을 사이드뷰로 전환했다.

## 씬과 조작

- 씬: `Assets/Scenes/Farming.unity`. Editor에서 복도·문·호실·책상을 편집할 수 있다. Farming Session의 Pickup Prefabs가 각 생성 대상의 외형을 연결한다.
- 복도에서 시작한다. A/D로 좌우 이동하고 325·324·323호 문 앞에서 E를 누르면 해당 방으로 이동한다. 방의 왼쪽 출구 문에서 E를 누르면 대응하는 복도 문으로 돌아온다.
- 문에 닿는 것만으로는 이동하지 않는다. E 한 번에 한 번만 전환되며 운반 상태·남은 시간·방의 파밍 상태는 유지한다.
- 새 회차마다 벙커 가까운 325호부터 324호, 323호 순으로 사람 3·2·1명을 배정한다. 사람은 이웃한 책상 사이의 통로에 서고 물자는 책상 프리팹의 빈 상판 위치에 놓인다. 통조림은 방마다 한 개, 다른 물자는 각각 한 개다. 레전드 두 종은 각각 방 가중치 1·2·3으로 독립 추첨한다.
- 복도 왼쪽 BUNKER에서 E로 운반 중인 대상을 모두 투입한다. 4칸 제한, 사람 2~4칸, 물자 1~2칸, 포물선 던지기·투입 페이드·60초 종료·벙커 씬 연결은 기존 시스템을 사용한다.
- E 우선순위: 운반 중 벙커 투입 → 가까운 문 → 가까운 파밍 대상 → 마지막 운반 대상 내려놓기. 문 근처에서는 HUD에 목적지가 표시된다.

## 공간과 이동

`Side View Map`의 FarmingMap2D가 corridor/rooms/doors 참조를 가진다. 각 FarmingRoom2D의 `Walk left`, `Walk right`가 수평 이동선의 끝점이며 `Camera height`가 카메라 높이를 정한다. footprint는 해당 공간의 월드 영역이다. 문은 FarmingDoor2D의 Area, Target, Arrival을 직렬화해 연결한다.

동일 씬의 분리된 공간으로 위치를 옮기므로 입퇴실 때 게임 상태를 다시 생성하지 않는다. 출입은 문을 통해서만 하며 다른 공간의 대상을 상호작용 대상으로 잡지 않는다. 카메라는 현재 공간의 끝에서 멈추고 좌우로 따라간다. 기존 16:9 FixedAspectCamera와 HUD를 사용한다.

캐릭터는 가구 앞의 수평 이동선을 걷는다. 배경 가구에는 통행을 막는 충돌체를 두지 않는다. 매 이동에서 X를 이동선 안으로 제한하고 Y를 바닥에 고정하므로 큰 프레임 간격이나 잘못된 초기 위치에도 벽 밖으로 나가거나 가구에 끼지 않는다. W/S 및 점프는 현재 조작에 포함하지 않는다.

## 아트·소팅·교체

- 현재 월드 이미지: `Assets/Art/Farming/SideView/`. 정면 벽·문·가구와 측면 캐릭터를 같은 픽셀 스타일로 제작했다. 모든 이미지 Sprite Single, PPU64, Point, mipmap off, 무압축, Full Rect.
- 실제 임포트한 30개 이미지의 GUID/local ID/크기/PPU는 [SideViewArtManifest.json](SideViewArtManifest.json)에 기록한다.
- 벽: 192×256, 바닥: 128×64. Rectangle SpriteRenderer의 Tiled 모드이며 다이아몬드 투영/Isometric Tilemap은 사용하지 않는다.
- 문/문 강조: 96×192, 테이블: 192×96, 의자: 64×96, 창: 192×128, 벙커 문: 192×192, 포커스: 128×32.
- 서 있는 인물/플레이어: 64×96, 누운 인물: 128×64, 물자: 64×64. 일반 오브젝트 Pivot은 하단 중앙, 누운 인물은 중앙이다. 벽/바닥은 좌하단.
- 배경 벽 -100, 바닥 -90, 창 -50, 문 0, 의자 5, 테이블 SortingGroup 10, 인물/월드 물자 100, 플레이어 200. 테이블 위 물건은 테이블 그룹 내부 20/21, 운반물은 플레이어 그룹 내부 10부터다. Z는 모두 0이며 Y-Z 아이소메트릭 깊이 계산을 하지 않는다.
- PNG 바이트만 같은 경로에 덮어쓰고 `.meta`는 유지한다. 새 이미지 규격은 위 크기/PPU/Pivot을 유지한다. 인물 프리팹의 standingSprite/lyingSprite, Player의 idleSprite/carrySprite 참조는 저장되어 있다.
- HUD 이미지는 기존 `Assets/Art/UI/Farming/`와 `FarmingHud.prefab`을 재사용한다. 안내 문구만 사이드뷰 조작으로 갱신했다.

## 데이터·제작 도구

`data.farming.pickups`의 기존 FarmingData 시트/CSV/DB와 `data.farming.rules`의 기존 FarmingRules.asset을 그대로 사용한다. 새 맵 CSV/SO/로더는 만들지 않았다. [규칙 명세](../DataSpecs/FarmingRules.md), [개체 명세](../DataSpecs/FarmingData.md), [전환 계획](../DataHistory/20261118-farming-sideview-01-planned.md).

`Tools > Farming > Rebuild Side View Scene`은 파밍 맵/해당 프리팹/아트를 다시 작성하는 제작 도구다. 평상시 배치 편집은 저장된 씬에서 수행한다. 기존 FarmingIsoSceneBuilder.Build 호출도 현재 사이드뷰 제작으로 연결된다. 이전 맵 스냅샷은 Git 이력과 `Temp/FarmingBeforeSideView`에 보존되며 게임에서는 사용하지 않는다.

검증: `Tools > Farming > Verify Side View Play`를 새 파밍 Ready 상태에서 실행한다. 결과는 `Docs/Farming/Verification-SideView.txt`에 기록한다. 이전 Verification-2D/Movement는 아이소메트릭 당시의 기록이다.

현재 무작위 배치의 [Play 검사·재시작·추첨 결과](../DataHistory/20261119-farming-spawns-02-verified.md). 초기 사이드뷰의 [조작·아트 검증](../DataHistory/20261118-farming-sideview-02-verified.md), [복도 화면](../../outputs/Farming/sideview-corridor.png), [325호 화면](../../outputs/Farming/sideview-room.png).
