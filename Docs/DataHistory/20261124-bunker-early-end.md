# 벙커 조기 종료와 최종 평가

벙커는 마지막 생존자가 탈진하거나 기획·프로그래밍·그래픽이 모두 완성되면 그 시점까지의 작업을 제출한다. 같은 작업으로 두 조건을 충족하면 완성을 먼저 표시한다. 마감 전에도 같은 최종 진척에 같은 엔딩이 나오도록 기존 마감 점수 기준을 사용한다.

| 사용한 데이터 ID | 정본 | 명세 |
|---|---|---|
| `data.bunker.rules` | [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset) | [벙커 규칙과 평가 기준](../DataSpecs/BunkerRules.md) |
| `data.game-dev.endings` | [GameDevEndings 시트](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846) | [엔딩 명세](../DataSpecs/GameDevEndings.md) |

데이터 목록과 기존 소비 경로를 대조했다. 탈진은 참여 캐릭터의 `CharacterIdentity.IsFainted`, 완성은 `GameDevProgress.IsComplete`를 읽는다. 점수는 기존 계산기와 엔딩 카탈로그가 결정한다. 회차의 종료 사유는 벙커 시뮬레이션이 소유한다. [중앙 안내와 컷씬 전환](../UI/GameDevEndingPopup.md)

Unity Play Mode에서 실제 벙커 씬을 세 번 새로 열어 전원 탈진, 세 분야 완성, 두 조건의 동시 충족을 검증했다. 각 회차에서 생존자 한 명과 미완성 게이지 하나가 남으면 계속 진행했다. 종료 조건을 충족한 뒤에는 추가 틱 없이 한 번만 제출했고, 중앙 안내가 검은 화면에 표시된 뒤 엔딩 씬에서 사라졌다.

실제 작업 틱에 마지막 게이지를 채우고 여러 틱 분량의 시간을 한 번에 진행시켜도 해당 작업 틱에서 멈췄다. 1920×1080과 1280×800으로 렌더링한 두 문구의 흰 글자 영역 중심은 모두 화면 중앙에서 가로·세로 3픽셀 이내였다.
