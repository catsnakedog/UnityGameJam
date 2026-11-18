# 파밍 준비 단계 연결

2026-11-19 KST · UnityGameJam · 코드 적용 및 컴파일

[도입 계획](20261119-farming-preparation-plan.md)에 따라 기존 `data.farming.rules`에 양의 유한한 초 단위 필드 `preparationDuration`을 추가했다. 편집 위치는 `Assets/Settings/Farming/FarmingRules.asset`이며 주소는 `data/farming/rules`이다. 설정 전체와 수명은 [FarmingRules 명세](../DataSpecs/FarmingRules.md)에 기록했다.

시작 입력을 받으면 FarmingSession이 준비 시간을 별도로 세고, 완료된 프레임의 남은 경과 시간부터 구조 시간을 차감한다. FarmingPlayer와 FarmingMap2D는 준비 중 이동과 문 출입을 처리한다. 획득과 벙커 투입은 구조 단계에서만 가능하다. FarmingHud는 기존 텍스트와 게이지를 선택된 단계의 시간에 연결한다.

런타임과 Editor 프로젝트를 컴파일해 오류 0개를 확인했다. 생성된 프로젝트 목록에 없던 동시 작업의 WorkProgressFeedback.cs만 임시 MSBuild 입력으로 포함했다. 파밍 변경 파일의 `git diff --check`도 통과했다. Editor용 Side View 검증에 실제 카운트다운, 양방향 문 이동, 사람·물자 획득 차단, 구조 시간 보존 검사를 추가했다. 이 기록의 확인 범위는 컴파일과 정적 검사까지다.
