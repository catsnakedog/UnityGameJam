# 파밍 사이드뷰 전환 계획

사용자 결정(2026-11-18): 아이소메트릭을 폐기하고 2D 사이드뷰로 통일. 복도에서 시작하고 문에 상호작용하면 호실로 이동한다.

- 담당: Codex farming-sideview-20261118.
- 재사용 데이터: `data.farming.pickups` (기존 FarmingData 시트 → CSV → DB), `data.farming.rules` (기존 FarmingRules.asset). ID/GUID/주소/값/스키마/로더 변경 없음.
- 소비 변경: moveSpeed는 수평 이동 속도, interactionRadius는 같은 공간 내 근접 탐색 및 문 상호작용 반경에 사용. mapHalfExtents.x는 기존 기본 호실 너비 설계에 사용. roomRevealDuration은 입실 시 해당 호실 시각 요소 노출에 계속 사용한다.
- 배치 정본: 기존 Farming.unity의 복도/호실 경계, 이동선, 연결 문, 도착점. 새 맵 CSV/SO/로더 없음. 캐릭터/물자의 상태와 운반 목록은 세션 런타임 상태.
- 구조: A/D 수평 이동, E 문 이동/들기/내려놓기/벙커 투입. 문 이동에서 시간과 운반 상태 유지. 같은 씬의 분리된 공간으로 전환하므로 재입실 시 획득 상태 유지.
- 중복 조사: DataRegistry의 planned 항목 및 FarmingRules/FarmingData 명세, Assets/Scripts/Farming·Assets/Editor/Farming·ProjectSettings·Docs의 기존 맵/규칙 소비 확인. 벙커와 다른 작업의 데이터는 재사용 경계만 확인하며 수정하지 않는다.
- 검증 예정: 사이드뷰 배치/수평 이동/각 문 왕복/다른 공간 상호작용 차단/운반 유지/테이블 앞 통행/4칸/투입 페이드/60초 종료/재시작/데이터 불변.
