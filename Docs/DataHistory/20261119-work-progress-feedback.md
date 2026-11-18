# 벙커 작업 완료 증가량 표시

- `data.bunker.rules`의 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이다. 기존 작업 주기·효율·잠금 조건에 따라 발생한 분야별 진행 증가량에 머리 위 표시를 연결했다. [규칙](../DataSpecs/BunkerRules.md)
- `data.characters`의 캐릭터 정의 위치는 `Assets/Settings/Data/Csv/Characters.csv`이다. 기존 장비 적용 후 능력과 회차의 실제 증가량으로 개인 몫을 계산한다. [캐릭터](../DataSpecs/Characters.md)
- `Assets`, `ProjectSettings`, `Docs`와 목록의 예정 항목을 조사했다. 화면 상단 개발 게이지는 누적치이고 머리 위 기존 게이지는 멘탈이다. 신규 작업 표시는 재사용되는 캐릭터 프리팹의 직렬화 UI이며 별도 CSV·SO·로더·저장 키를 사용하지 않는다.
- 세 분야, 잠금 상태, 복수 기여자, 상한 직전의 부분 증가에 대한 검사를 `BunkerChecks.RunPlay()`에 포함했다. 프리팹 참조 검사는 `WorkProgressFeedbackSetup.Validate()`로 실행한다. [배치와 표시 규칙](../UI/WorkProgressFeedback.md)
