# 파밍 복도 / 2D 맵 변경 계획

- 작업: Codex farming-map2d-20261118. 기존 `data.farming.pickups`, `data.farming.rules`를 재사용한다. 신규 CSV/DB/로더 없음.
- 기존 FarmingData 시트 → CSV → DB 연결, 모든 ID와 무게, Characters 참조, 60초와 4칸을 유지한다.
- Rules 정본 `Assets/Settings/Farming/FarmingRules.asset`에 `roomRevealDuration`(float, 초, 필수 양수, 제안 0.5)을 추가한다. 같은 FadeDuration은 벙커 투입 대상만 담당하므로 별개 연출 대상인 방 노출 시간을 명시한다.
- 기존 `mapHalfExtents=(11,8)`는 단일 방 경계에서 기본 호실 설계 반크기(논리 격자 단위)로 이전한다. 씬 생성 시 소비하며 실제 플레이 이동/투하 경계는 씬 Floor Tilemap/Collider2D가 소유한다. 기존 3D X/Z 경계 클램프는 제거한다.
- 이동/상호작용/투하 거리 값은 그대로 유지하고 XY 월드 단위로 소비한다. 새 2:1 투영의 방 크기와 동선은 플레이로 검증한다.
- 배치/타일/방 영역/스프라이트 참조는 기존 Farming 씬과 Prefab을 편집 정본으로 사용한다. 별도 맵 CSV/SO를 복제하지 않는다.
- 중복 조사: DataRegistry active/planned, Assets/Scripts, Assets/Editor, ProjectSettings, Docs에서 FarmingMap, roomReveal, mapHalfExtents 검색. 기존 호실 노출 데이터 없음. 좀비/벙커/시트 작업 예약과 범위 충돌 없음.
- 검증 예정: 모든 타일 64×64/PPU64/Point, 실제 2D Grid/Tilemap, 복도 시작, 3개 문 통행/벽 차단, 입퇴실 페이드, 내부 비노출/상호작용 차단, 운반/던지기/페이드/시간 종료/재시작 및 기존 벙커 전달.
