# 파밍 2D 아이소메트릭 복도

## 편집 위치

- 씬: `Assets/Scenes/Farming.unity`. `Isometric 2D Grid`의 Floor와 방별 Wall/Ceiling Tilemap을 편집한다.
- 복도는 화면 왼쪽 아래에서 오른쪽 위로 올라간다. 호실은 복도의 왼쪽 위에 325 / 324 / 323호 순서로 붙는다. 기존 6명·12개 물자·6개 테이블은 325호에 배치하며 나머지 호실은 같은 형식의 집기를 가진 확장 공간이다.
- 시작: 복도 첫 문 앞. 복도 시작점의 민트색 BUNKER가 투입 구역이다.
- 이동/파밍 규칙은 기존 Session/Inventory/Addressables를 사용한다. 결과에서 기존 벙커 씬으로 넘어가는 연결을 유지한다.

## 타일 교체 계약

참조: [Unity 2D IsoTilemaps](https://github.com/UnityTechnologies/2D_IsoTilemaps), commit `31e3ea7aaa3968236166f4c58089e2487efba92b`. README의 아트 크레딧: [castpixel](https://twitter.com/castpixel). Basic의 base01/cube01 형태를 재사용하고 프로젝트 팔레트로 색상만 조정했다.

| 항목 | 공통 규격 |
|---|---|
| 바닥·벽·입구·가림 타일 | PNG RGBA, 64×64 px |
| Sprite | Single / Full Rect / PPU 64 |
| 필터 | Point, mipmap OFF, 압축 None |
| Pivot | (0.5, 0.25): 하단 32px의 다이아몬드 중심이 바닥 기준점 |
| Grid | Isometric, cellSize (1, 0.5, 1), XY 평면 |
| Tilemap anchor | (0.5, 0.5, 0) |
| 투영 | x=(u+v)/2, y=(v-u)/4; 정수 u/v는 셀 경계. 논리 셀 (u,v)는 Grid 셀 (v,-u-1)에 저장 |
| 벽 높이 | 같은 64px cube 타일을 위로 쌓아 표현 |
| 인물·집기 | 동일 PPU64/Point/팔레트. 캔버스는 실루엣 크기에 맞추되 발/바닥 pivot 유지 |

아트: `Assets/Art/Farming/Isometric`; Tile 에셋: 그 아래 `Tiles`. 원본 PNG 참조는 `Reference`에 보관한다. 인물·물체도 SpriteRenderer이며 런타임 3D 메시를 사용하지 않는다. 새 타일은 위 규격으로 기존 Tile.sprite를 교체한다. 새 맵 DB나 CSV를 만들 필요가 없다.

## 문과 시야

- Collider2D가 벽과 테이블의 통행을 막고, 전면 벽의 3셀 문만 방과 복도를 연결한다.
- `FarmingMap2D`는 실제 플레이어 바닥 위치로 현재 방을 판정한다. `FarmingRoom2D`는 선택된 방의 천장/벽을 1→0, 집기/사람/물자를 0→1로 페이드한다. 복귀 시 반대로 전환한다.
- 닫힌 방의 물건은 상호작용 대상에서 제외한다. 들고 나온 개체는 방 가림과 독립적으로 표시하며 내려놓은 위치에 맞춰 다시 판정한다.
- 월드 Sprite/SortingGroup과 벽 타일은 같은 Order=0을 사용한다. 카메라 Custom Axis=(0,1,-1)은 Y-Z로 바닥 깊이를 비교한다. Z는 화면상 추가 높이를 상쇄하는 렌더링 메타데이터이며 충돌은 계속 XY Collider2D다. 숫자로 계산한 개체별 sortingOrder와 타일별 네이티브 정렬을 혼용하지 않는다.
- 벽은 Individual Tilemap 모드로 캐릭터·집기 사이에 끼어 정렬된다. 각 벽 단의 Y 높이만큼 Z를 보정하고, 동일 바닥점의 단들에는 1/1024 단위의 작은 순서 보정을 사용한다. 스프라이트는 Pivot 기준이며 SortingGroup 위치는 발/바닥점이다.
- 테이블 위 물건은 테이블 SortingGroup의 자식(내부 Order 10/11), 운반 개체는 플레이어 SortingGroup의 자식으로 렌더한다. 던질 때는 독립 월드 그룹으로 복귀하며 손 높이와 포물선 높이를 함께 상쇄한다. 바닥·벙커 마커는 지면 순서, 닫힌 방의 불투명 천장은 가림 전용 순서를 사용한다.
- 카메라는 회전 없는 정투영 XY 카메라로 플레이어를 따라간다. 공용 16:9 FixedAspectCamera/HUD 설정을 유지한다.

## 데이터와 수명

새 정본 없음. `data.farming.pickups`는 기존 시트/CSV/DB, `data.farming.rules`는 기존 SO다. [FarmingRules 명세](../DataSpecs/FarmingRules.md)에서 방 페이드와 기본 호실 크기를 관리한다. 씬 Tilemap/Collider2D는 배치의 정본이며 런타임 경계를 별도 상수로 복제하지 않는다.

`Tools > Farming > Rebuild 2D Corridor Scene`은 생성된 씬/Prefab/아트를 재작성하는 제작 도구다. 평소 편집은 저장된 씬/타일에서 수행한다. 최초 3D 씬/Prefab 스냅샷은 프로젝트 밖이 아닌 Unity 미임포트 경로 `Temp/FarmingBefore2D`에 보관했으며 배포 의존성이 아니다.

검증: [최종 검증 기록](../DataHistory/20261118-farming-map2d-04-verified.md), [69개 Play 검사](Verification-2D.txt). [전체 배치](../../outputs/Farming/map-overview.png), [325호 실내](../../outputs/Farming/room-final.png).

소팅 근거: [Unity Tilemap Renderer Modes](https://docs.unity.cn/Manual/Tilemap-Isometric-RenderModes.html), [Unity 2D Sorting](https://docs.unity.cn/Manual/2DSorting.html). 이 프로젝트는 높이 1을 Z 1로 표현하므로 표준 Isometric Z as Y 예제의 -0.26 대신 -1을 사용한다.
