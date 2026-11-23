# 960틱 최종 점수 점검 계획

2026-11-23 KST, UnityGameJam. 게임 종료 시 점수 측정 오류 제보로 계산식, 회차 길이, 등급 경계를 대조했다.

기존 [엔딩 명세](../DataSpecs/GameDevEndings.md)의 120틱 회차에서는 목표 진척 96, 기준 틱 24로 종료 시 분모가 480이었다. 회차가 960틱으로 늘어난 뒤 같은 기준 틱으로는 분모가 3840이 된다. 세 분야 합계 상한 300을 넣어도 0.078125점으로, 상위 두 엔딩에 필요한 0.3점과 0.5점에 도달할 수 없다.

[벙커 규칙](../DataSpecs/BunkerRules.md)의 기존 `data.bunker.rules.evaluationReferenceTicks`를 회차 길이에 비례해 24에서 192로 변경한다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`, GUID는 `01a9f70ed623c5341bec7f05cc07781d`, 기존 주소는 `data/bunker-rules`다. 현재 회차의 종료 점수, 경계 포함 여부, 마지막 틱의 제출 및 결과창 표시를 검증한다. 엔딩 시트와 중간 발표 기준은 변경 대상에서 제외한다.
