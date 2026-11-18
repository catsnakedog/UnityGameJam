# 벙커 HUD와 생존자 멘탈 표시

1920×1080 게임 영역 상단에 24시간 시계와 기획·프로그래밍·그래픽 진행도를 표시한다. 시계와 각 분야는 개별 그림이며, 개발 상태는 이름과 퍼센트로도 구분한다. 기존 16:9 레터박스 Canvas의 좌상단을 좌표 원점으로 삼는다. 치수는 프리팹에서 조절할 수 있다.

| 요소 | 배치와 크기 | 표시와 입력 |
|---|---|---|
| Shelter clock | 좌측 24, 상단 20, 100×100 | 24눈금 다이얼 위의 호박색 링이 12시부터 시계 방향으로 찬다. 중앙에 시:분을 표기하며 월드 클릭을 통과시킨다. |
| Planning / Programming / Graphics | 좌측 486, 740, 994. 상단 24. 카드 각각 240×68, 간격 14 | 윗줄에 분야와 퍼센트, 아래에 왼쪽부터 채워지는 독립 게이지. 카드 위에서 월드 클릭을 차단한다. 틈새는 월드 클릭을 통과시킨다. |
| Status | 진행도 카드 아래 | 데이터 준비·실패·집계·결과 상태를 표시한다. 개발 중에는 숨긴다. 이벤트·엔딩 팝업은 기존 순서로 위에 뜬다. |
| Mental gauge | Survivor 루트의 로컬 높이 2.22, 폭 1.04, 높이 0.14 월드 단위 | 생존자별 현재 멘탈 비율. 이름은 높이 2.34에 한 줄로 표시하며 탈진하면 이름 뒤에 ‘탈진’을 표시한다. 좌우 이동·엘리베이터 탑승 시 캐릭터를 따라간다. 선택 카드에서는 정확한 수치를 계속 볼 수 있다. |

## 데이터와 전환

- 시계는 벙커 시뮬레이션의 현재 틱을 로드된 전체 틱으로 나눈다. 0틱에 00:00, 절반에 12:00, 마지막 틱에 24:00을 표시한다. 현재 메인 프로젝트의 960틱이면 40틱에 한 시간이다. 일시 정지 동안 시각을 유지하고 재시작하면 첫 틱에서 다시 시작한다. 초침은 없다. [벙커 규칙](../DataSpecs/BunkerRules.md)
- 개발 게이지는 기존 `GameDevManager.Progress`와 변경 이벤트를 사용한다. 각 분야 0~100%이며 소수점 첫째 자리까지 버림 표기한다.
- 머리 위 멘탈 게이지는 생존자마다 `CharacterIdentity.CurrentMental`과 같은 인물에게 이미 연결된 `Rules.MentalMaximum`을 사용한다. 규칙을 받으면 비율을 채우고, 이후 일·아이템·피격·회복·재시작으로 현재값이 바뀔 때 즉시 갱신한다. [캐릭터 명세](../DataSpecs/Characters.md)

## 프리팹과 이미지 교체

- 개발 HUD와 시계는 `Assets/Prefabs/UI/DevGageUI.prefab`, 멘탈 바는 `Assets/Prefabs/Shelter/Survivor.prefab`에서 편집한다. 선택 카드 `Assets/Prefabs/UI/ShelterCharacterCard.prefab`는 시계 아래에 뜬다.
- `Assets/Art/UI/ShelterClockDial.png`와 `ShelterClockElapsed.png`는 112×112 투명 PNG다. 같은 중심·지름에 맞춰 원본 경로의 파일을 교체한다. Single Sprite, 100 PPU, Bilinear, 압축·mipmap 끔과 기존 .meta를 유지한다. 동적 링은 uGUI Image의 Filled / Radial360을 사용한다.
- 분야 카드·트랙은 기존 `Assets/Art/UI/Farming/panel.png` (64×64, 9-slice), 채움은 `gauge.png` (64×16), 문자에는 기존 한글 폰트를 사용한다. 공유 원화를 교체하면 파밍과 캐릭터 카드에도 반영된다.
- 기존 프로젝트에서 편집 모드의 `GameDevSetup.UpdateShelterHud()`로 프리팹에 반영한다. `GameDevChecks.CheckShelterHud()`는 저장된 진행도 세 칸의 가로 배치, 링과 시간 문자열 연결, 생존자 프리팹의 월드 게이지 참조를 검사한다.

## 이전 우측 패널 검증 기록 (2026-11-18)

- 저장된 프리팹 GUID `6e70d210995c7134dad415b09132670e`. 제작 도구 재실행 후 씬 Manager/UI 각 1개, 직렬화 Manager 참조·모든 Sprite/TMP 참조·Missing Script 0 확인.
- 빌드된 Addressables 데이터로 실제 Play, 0/부분/100% 및 엔딩 표시 확인. 재바인딩·비활성 후 활성·씬 재진입에서 값 동기화와 초기화 확인.
- 1920×1080, 1440×1080, 2560×1080 실제 Game View 캡처/시각 검수. 검은 여백과 HUD 영역 정렬, 한글/퍼센트/엔딩 본문 표시 확인.
- 실제 EventSystem Raycast: 패널이 월드 클릭을 차단하며 패널 밖 월드 영역은 통과.
- 공유 gauge.png의 한 픽셀을 바꿔 재임포트 후 프리팹의 세 Fill 참조가 유지됨을 검사하고 원본 바이트 복구. GUID `b7ef226803a28ab4ab4bff472a5b6c67`, Sprite local ID `21300000`. panel.png GUID `3a153a4aac619734595107192bd960bc`.
- [부분 진행 화면](../GameDev/Preview-progress.png), [엔딩 화면](../GameDev/Preview-ending.png), [4:3](../GameDev/Preview-4x3.png), [울트라와이드](../GameDev/Preview-wide.png), [실행 검사](../GameDev/Verification-play.txt).
