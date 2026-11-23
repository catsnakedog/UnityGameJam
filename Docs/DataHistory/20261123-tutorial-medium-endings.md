# 튜토리얼의 보통 난이도와 최종 엔딩 문구

튜토리얼에서 파밍으로 넘어갈 때 `TutorialBriefing.BeginFarming`이 `BunkerDifficulty.Medium`으로 회차를 시작한다. 벙커는 회차에 저장된 난이도와 기존 `data.bunker.rules` 프리셋을 읽는다. [벙커 규칙 명세](../DataSpecs/BunkerRules.md) · [규칙 정본](../../Assets/Settings/Shelter/BunkerRules.asset)

`data.game-dev.endings`의 기존 세 행에서 제목과 본문을 수정했다. 시트를 CSV로 다시 내려받아 전체 행과 열을 대조하고, UTF-8 BOM을 포함한 프로젝트 스냅샷에 반영했다. [엔딩 명세](../DataSpecs/GameDevEndings.md) · [편집 정본](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846) · [로컬 CSV](../../Assets/Settings/Data/Csv/GameDevEndings.csv)

| 최소 점수 | 기존 ID | 제목 | 본문 |
|---:|---|---|---|
| 0 | `ending_unfinished` | 실패 | 좀비들이 게임에 만족하지 못하고 돌아갔습니다... |
| 0.3 | `ending_released` | 평범 | 벙커에서 만든 게임을 세상에 선보였습니다. |
| 0.5 | `ending_breakthrough` | 갓겜 | 벙커에서 만든 게임이 모든 좀비들을 만족시켰습니다! |

데이터 목록의 예약 항목, 기존 CSV·DB와 소비 코드를 조사했다. 결과 팝업은 엔딩 카탈로그의 제목과 본문을 읽고 컷씬과 표정은 같은 행의 ID로 결정된다. 기존 주소와 ID를 재사용했다.

공용 임포터가 재생성한 DB를 열어 0, 0.29999, 0.3, 0.49999, 0.5점에서 결과 ID·제목·본문을 대조했다. 팝업이 사용하는 한글 폰트에 세 문구의 표시 문자 40개가 모두 들어 있었다. 컴파일된 튜토리얼 진입 함수가 보통 난이도와 튜토리얼 플래그를 전달하는지 확인했고, 해당 프리셋의 0.5초/틱, 사건 간격 48틱, 첫 습격 40틱, 습격 간격 96틱을 검증했다. [임포터 적용 이력](20261123-223129-5747018-data-applied.md)

Windows x64와 WebGL에서 각각 빌드 전 검사를 통과했다. Windows 실행 파일을 구동하고 WebGL은 로컬 HTTP 서버에서 타이틀 화면 표시를 확인했다. 두 배포 ZIP의 전체 파일을 빌드 폴더와 대조했고, 웹 서버에서 받은 실행 모듈과 게임 데이터 번들의 해시도 일치했다.
