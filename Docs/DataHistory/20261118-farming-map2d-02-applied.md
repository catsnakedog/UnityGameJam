# 파밍 복도 2D 전환 적용

- 작업: Codex farming-map2d-20261118. 이전: [변경 계획](20261118-farming-map2d-01-planned.md).
- `data.farming.rules`: 기존 `Assets/Settings/Farming/FarmingRules.asset`, GUID `22168f09c24cbfb49aeb55b136dd0298`, Addressables `data/farming/rules` / Farming Local 유지. roomRevealDuration=0.5초 추가, Validate 유한 양수 검사. 별도 설정 에셋/로더 없음.
- mapHalfExtents (11,8) 값은 유지하며 기존 Player X/Z 클램프 소비를 제거. 기본 호실 제작 크기를 정하는 Editor 소비로 이전했다. 런타임 경계는 저장된 Floor Tilemap과 벽/테이블 Collider2D다.
- duration=60, capacity=4, moveSpeed=5.8, interactionRadius=2, dropDistance=1.6, throwDuration=0.45, fadeDuration=0.5, throwArc=1.5, warningSeconds=10 유지. 거리 값은 2D XY 월드에서 소비.
- `data.farming.pickups`: 기존 시트 → UTF-8 BOM CSV → FarmingDatabase 유지. 사람 6개/물자 6개 레코드, 씬 인스턴스 사람 6/물자 12. 모든 ID/무게 보존. Characters 행과 FarmingRunTransfer의 기존 사람 ID 연결 유지.
- 씬은 `Assets/Scenes/Farming.unity` 동일 GUID. Prefab도 기존 파일에 Editor API로 저장. 기존 3D 씬/프리팹 스냅샷을 Temp/FarmingBefore2D에 보관했다. 런타임에 생성하는 맵이 아니라 편집 가능한 저장된 Grid/Tilemap/Prefab이다.
- 타일 7종은 PNG 64×64, PPU64, Point, mipmap OFF, 압축 None, pivot (0.5,0.25)로 대조했다. 참조 원본과 파생 에셋 경로는 [Map2D](../Farming/Map2D.md)에 기록.
- 적용 전 최신 DataRegistry의 planned 좀비 작업 및 다른 시트/벙커 데이터와 비교. 파밍 ID/수치의 신규 복제본 없음. 공용 Data Manager에 파밍 CSV를 중복 등록하지 않았다.
- 실제 검증: C# 컴파일 통과, CSV/기존 규칙 13개 Edit 검사 통과. 로컬 Addressables 콘텐츠를 빌드하고 기존 번들 모드 Play에서 roomRevealDuration=0.5, Ready, 3개 방 로딩을 확인했다. 60초 종단 검사는 공유 Editor의 다른 작업과 겹쳐 중단되었으며, 최종 검증은 후속 불변 이력에 별도 기록한다.
