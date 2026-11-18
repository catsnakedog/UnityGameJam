# 파밍 제한시간 반영 전 확인

사용자가 `data.farming.rules.duration`을 60초에서 30초로 편집했다. Unity Inspector의 정본과 디스크의 `Assets/Settings/Farming/FarmingRules.asset` 모두 30초였다. Farming 씬을 새로 Play한 Ready 상태에서 `FarmingSession.Rules.Duration`과 `Remaining`은 모두 60초였다. Play Mode Script는 Use Existing Build이며 로컬 번들이 SO 편집보다 먼저 빌드됐다.

기존 주소 `data/farming/rules`와 Farming Local 그룹을 유지해 콘텐츠를 갱신하고, HUD 제목과 검증기에서 발견한 고정 60초 참조를 정리한다. [정본과 필드 명세](../DataSpecs/FarmingRules.md)
