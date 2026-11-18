# 벙커 시계와 머리 위 멘탈 표시 연결

기존 `data.bunker.rules`의 `totalTicks`와 `mentalMaximum`을 벙커 HUD와 생존자 프리팹의 표시 상한으로 사용한다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이며 시계와 멘탈 게이지가 각각 현재 틱·현재 멘탈에서 비율을 계산한다. [벙커 규칙](../DataSpecs/BunkerRules.md)

`data.characters`의 구조 캐릭터 이름과 초기 멘탈은 기존 Characters 시트 → `Assets/Settings/Data/Csv/Characters.csv` → GameDatabase 경로로 가져온다. 초기화 후 각 생존자에게 유지되는 `CharacterIdentity.CurrentMental`이 머리 위 바와 정보 카드에 함께 반영된다. [캐릭터 명세](../DataSpecs/Characters.md)

등록 목록의 planned 항목과 Assets, ProjectSettings, Docs에서 clock·CurrentTick·totalTicks·mentalMaximum·게이지·멘탈 소비자를 조사했다. 벙커 틱과 멘탈 상한은 기존 한 개 SO가 소유하며, 개발 게이지도 기존 GameDevManager 값을 사용한다. 새 밸런스 필드·CSV·설정·저장 키 없이 UI 소비자를 연결했다.

Unity 6000.3.28f1의 Roslyn과 프로젝트의 Editor 대상 참조로 런타임 및 Editor C# 어셈블리를 각각 컴파일했다. 두 컴파일의 진단은 0건이었다. 메인 Editor의 프리팹 반영 및 Play 검증은 통합 단계에서 진행한다. [화면 명세와 검사 방법](../UI/DevGageUI.md)
