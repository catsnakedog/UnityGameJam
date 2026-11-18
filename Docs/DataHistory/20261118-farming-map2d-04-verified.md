# 파밍 2D 복도 · 방향 · 소팅 최종 검증

작업: Codex farming-map2d-20261118. Unity 6000.3.28f1 연결 Editor에서 검증. 이전 [방향 정정](20261118-farming-map2d-03-orientation.md), [적용](20261118-farming-map2d-02-applied.md).

## 구현과 편집 정본

- `Assets/Scenes/Farming.unity`: 왼쪽 아래→오른쪽 위 복도, 그 왼쪽 위 325/324/323호. 기존 파밍 콘텐츠는 325호. 저장된 2D Grid/Tilemap/Collider2D/SpriteRenderer/Prefab으로 편집한다.
- `data.farming.pickups`: [FarmingData 명세](../DataSpecs/FarmingData.md)의 기존 Google Sheets 정본 → `Assets/Settings/Farming/FarmingData.csv`(UTF-8 BOM) → 기존 FarmingDatabase. ID/타입/무게와 6명/12개 인스턴스 보존.
- `data.farming.rules`: `Assets/Settings/Farming/FarmingRules.asset`, GUID `22168f09c24cbfb49aeb55b136dd0298`, 주소 `data/farming/rules`, Farming Local. [현재 명세](../DataSpecs/FarmingRules.md). roomRevealDuration=0.5초 추가 외 기존 수치 보존. 로더/DB/CSV/설정 복제 없음.
- `mapHalfExtents`는 제작 시 기본 호실 크기로 소비하고, 이동/투하는 실제 Tilemap/Collider2D 경계를 사용한다. 제작 도구에서 유효하지 않은 값을 자동으로 기본화하지 않는다.
- 적용 직전 최신 DataRegistry의 시트/좀비/엔딩 작업을 다시 확인했다. 이번 방향/소팅 작업은 해당 데이터에 쓰지 않으며 파밍 데이터를 공용 DB/CSV에 중복 등록하지 않았다.

## 소팅 수정

- 기존 서로 다른 숫자 순서의 Sprite 그룹과 Tilemap을 혼용하던 방식을 제거. 월드 그룹/벽은 Order 0과 카메라 Custom Axis (0,1,-1), 발/바닥 pivot으로 통일했다. Unity가 축을 단위 벡터로 반환하는 것도 확인했다.
- 벽 Tilemap은 Individual 모드. 벽 단과 투사체의 화면상 높이를 Z로 상쇄해 바닥 깊이로 비교한다. 실제 충돌과 맵은 계속 2D XY다.
- 테이블 위 물건은 테이블 SortingGroup의 자식, 운반 대상은 플레이어 SortingGroup의 자식으로 정렬. 던지면 독립 월드 그룹으로 돌아간다.
- 선택 링과 무게 표시는 테이블 위 물건의 높이를 따른다. 데이터 로딩 전 LateUpdate가 원래 라벨 높이를 바꾸던 문제를 수정했다.

## 실행한 검증

- C# 컴파일 통과. CSV/Rules Edit 검사 13개 통과.
- 로컬 Addressables 빌드 후 기존 번들 모드에서 Rules의 추가 필드가 0.5로 로딩되는 것을 확인했다. 실행 파일 빌드는 요청 범위에 없어 수행하지 않았다.
- 2026-11-18 17:33:06 KST: [Play 검사 69개 통과](../Farming/Verification-2D.txt). 방향, 7종 타일 규격, 2D 구성, 네이티브 깊이 정렬 설정, 테이블/운반 그룹, 모든 문 출입·페이드·재가림, 벽·테이블 충돌, 숨겨진 방 상호작용 차단, WASD/E, 4칸 용량, 투하/재습득, 벙커 투입·페이드·중복 방지, 실제 60초 경과·종료 입력 차단·미투입분 제외, SO 원본 보존 포함.
- `Restart()` 후 실제 씬 재로딩: Ready, Remaining=60, PeopleSaved=0, ItemsSaved=0, Used=0, 개체 18개 모두 Available, 복도 시작, 모든 방 Reveal=0 확인.
- 이후 표시 위치 수정은 별도 집중 검사: 18개 개체의 라벨 높이를 저장된 Prefab 기준과 대조, 전부 통과. 사람 라벨 Y=1.55, 테이블 위 물건 라벨 Y=1.44, 물건/선택 링 높이=0.64 확인. 해당 수치는 배치 검증 스냅샷이며 별도 밸런스 정본이 아니다.
- 실제 카메라 캡처로 닫힌 복도/열린 325호/전체 배치를 확인. [복도](../../outputs/Farming/corridor-corrected.png), [최종 실내](../../outputs/Farming/room-final.png), [전체 배치](../../outputs/Farming/map-overview.png).
- 캡처용 카메라/HUD 변경은 런타임에만 적용하고 재시작으로 복원했다. 마지막 상태는 파밍 시작 대기(Ready)이며 E로 시작할 수 있다.
