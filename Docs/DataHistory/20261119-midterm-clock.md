# 중간 발표와 시계 이벤트

공개 노션의 개발 요구사항, 시간표, 중간평가 원문을 대조했다. 960틱 회차의 240·480·720틱에 일반 사건을 대체하고, 첫 좀비 습격은 20틱부터 기존 80틱 간격으로 발생한다. [시간표](https://app.notion.com/p/41caca30f5dc8065a8aae3eccbbd09c0) · [점수와 멘탈](https://app.notion.com/p/41caca30f5dc801c873be391bddeeade)

등록 목록, 기존 이벤트·엔딩 테이블, BunkerRules, 틱 시계와 HUD를 검색했다. 발표는 선택형 사건이나 최종 엔딩 행이 아니며, 기존 규칙 에셋과 진행도 계산을 사용한다. 등급표·첫 습격·발표 시각의 정본은 [기존 BunkerRules](../../Assets/Settings/Shelter/BunkerRules.asset) 한 곳이다. [데이터 명세](../DataSpecs/BunkerRules.md)

Unity 6000.3.28f1의 Windows Addressables로 960틱 전체를 실행했다. 일반 사건 20회, 습격 12회, 발표 3회, 24시 종료를 확인했다. 발표는 3인의 멘탈 상·하한과 등급 경계, 확인 버튼 및 Enter로 재개, 중복 적용 방지, 재시작 후 15개 표식 복구를 통과했다. [실행 기록](../Events/Verification-midterm-play.txt) · [시계와 방 배치](../GameDev/Preview-clock-events.png) · [첫 발표 화면](../Events/Preview-midterm.png)

같은 회차 규칙에서 3인 방 배치, 두 병상의 동시 예약, 명령 취소로 예약 반환, 마지막 병상에 기절자 인계와 구조자의 복귀를 확인했다. [벙커 검사](../Bunker-play.txt) · [엘리베이터 검사](../Elevators-checks-play.txt)

1920×1080과 1440×1080 Game View에서 시계, 범례, 진행 게이지와 열다섯 표식의 실제 배치를 확인했다. 최신 아트로 파밍의 이동·운반·애니메이션 검사도 통과했다. [파밍 검사](../Farming/Verification-SideView.txt) · [일반 사건 회귀 검사](../Events/Verification-effects-play.txt) · [좀비 습격 회귀 검사](../Zombies/Verification-play.txt)
