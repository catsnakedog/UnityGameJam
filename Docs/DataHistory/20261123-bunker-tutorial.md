# 벙커 도착 방송 적용과 검증

2026-11-23 KST · UnityGameJam · data.shelter.dialogue · verified

기존 주최측 방송 14행의 ID를 유지했다. 첫 인사 세 문장에 파밍 조작 네 문장을 추가하고, 기존 벙커 설명과 종료 인사를 벙커에 배치했다. 벙커 도착 인사, 프로그래밍실·그래픽실, 기절자 구조, 돌발 상황을 더해 벙커 방송은 16문장이다.

정본은 [ShelterDialogue.csv](../../Assets/Settings/Data/Csv/ShelterDialogue.csv)다. 네 열의 타입과 필드 계약은 [대사 명세](../DataSpecs/ShelterDialogue.md)에 있다. 공용 GameDatabase.asset의 GUID는 `62a272592b807b3469b072879c349759`이며, Game Data Local 그룹의 `data/catalog`를 두 씬의 TutorialBroadcast가 GameData로 읽는다. 생존자·좀비 대사와 다른 데이터의 중복 검사는 [계획](20261123-bunker-tutorial-plan.md), 적용 전후 행 값은 [Data Manager 적용 기록](20261123-144545-3907726-data-applied.md), Default 프로필 콘텐츠 빌드 결과는 [빌드 기록](20261123-144610-7686249-data-built.md)에 있다.

Unity Play Mode에서 타이틀 튜토리얼 버튼, 복도 7문장, 파밍 결과에서 벙커로 이동, 벙커 16문장을 차례로 통과했다. 실제 UI 레이캐스트로 문장을 진행하고 CSV 문구 및 순서를 비교했다. 양쪽에서 방송 클립의 서로 다른 11프레임이 UI Image에 표시됐고, 옵션 창에서는 스피커와 대사가 멈췄다. 벙커 방송 중 틱이 0으로 유지되고 지휘 입력이 잠겼다. 실제 가방 열림, 마지막 클릭 후 카메라·가방 복원, 입력 재개, 틱 증가, 일반 회차 및 벙커 직접 실행 시 방송 생략을 확인했다. 1920×1080과 1440×1080에서 화면을 캡처했다. 4:3 화면에서 강조 중인 실제 시계·범례 및 개발률 패널이 천장 위에 보이는 것도 확인했다. 음파 이미지는 시험 교체와 원본 복구 후 프리팹 참조 및 GUID가 유지됐다. [플레이 화면](../Images/BunkerTutorial.jpg).
