# 벙커 생존자 애니메이션

[공유 Drive의 player 폴더](https://drive.google.com/drive/folders/1IfsNUxatQhkW-Fn5cK1Jo6TcwFAyZ5SC)에 캐릭터별 원본을 보관한다. Unity에 반영할 때는 바뀐 파일을 같은 이름으로 `Assets/Art/Shelter/Characters/no01_tags`~`no06_tags`에 복사한다. Unity가 임포트를 마치면 편집 모드에서 **Tools → Shelter → Refresh Survivor Animations**를 실행한다. 메뉴는 공용 Survivor 프리팹의 클립 참조를 갱신한다. Drive 업로드와 Unity 반입은 별도 단계다.

| 파일명 (.aseprite) | 재생 조건 |
| --- | --- |
| idle_1 / idle_2 | 일반 / 피곤한 대기, 반복 |
| walk_1 / walk_2 | 실제 수평 이동 중 일반 / 피곤한 걷기, 반복 |
| sit | 개발 가능한 방에 자리 잡을 때. typing이 있으면 앉기 한 주기 후 전환하고, sit만 있으면 반복 |
| typing | 개발 작업 중 반복 |
| sleep | 수면 방에 도착해 머무는 동안 반복 |
| faint | 멘탈 0에서 기절한 뒤 수면실 인계와 각성까지 유지한다. 기절 원화가 없는 환자를 운반할 때는 기본 그림을 가로로 들어 올린다. |
| ranged_attack | 보초의 실제 원거리 공격 판정마다 한 번 |
| no01~no06 | 해당 캐릭터의 기본 정지 그림. 없으면 idle_1의 첫 프레임 사용 |

파일 하나에 한 동작을 넣는다. 현재 원본은 128×128 캔버스에서 가로 중앙, 위에서 96픽셀 지점에 발을 맞춘다. 같은 기준으로 그리면 동작 전환 시 발 위치를 맞출 수 있다. Unity는 보이는 레이어를 프레임별로 합치고 Aseprite에서 정한 프레임 시간을 사용한다.

피곤 기준은 [BunkerRules](DataSpecs/BunkerRules.md)의 멘탈 50 이하이며, 기절 상태가 우선한다. 비어 있는 피곤한 걷기는 일반 걷기, 작업·수면 등 비어 있는 동작은 대기 그림으로 표시한다. 파밍 조작자는 1번의 대기·걷기 클립을 사용한다. 각 방에서 대기하는 구조 대상은 자기 ID의 대기 클립을 사용하며, 5·6번은 원본에 있는 기본 그림을 표시한다.
