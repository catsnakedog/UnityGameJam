# 엔딩 팝업

- UI ID `game-dev.ending-popup`, v1, 2026-11-18. 사용자 요청 확정: GameDevEndings 기반 팝업.
- 씬 `Assets/Scenes/ShelterPrototype.unity`, 프리팹 `Assets/Prefabs/UI/GameDevEndingPopup.prefab`, 스크립트 `Assets/Scripts/GameDev/GameDevEndingPopup.cs`.
- 기준 1920×1080, 기존 [16:9 레터박스](Display.md) 유지. 기존 사건 노트북과 버튼 Sprite, FarmingKorean TMP 폰트 재사용. 아래 치수는 초기 구현 기본안.

| 요소 ID | 배치·크기 | 표시·입력 |
|---|---|---|
| backdrop | 전체 1920×1080, Canvas order 200 | 어두운 배경, 바깥 클릭 차단 |
| frame | 중앙 1120×760 | 기존 Events/laptop_frame.png |
| title | 프레임 좌상단 (130,139), 860×55 | displayName, TMP 34→26 자동 크기 |
| score | (130,100), 860×32 | 평가 점수, 소수 3자리 |
| body | (130,225), 860×220 | description, TMP 28, 긴 본문 세로 스크롤 |
| confirm | (810,485), 180×56 | 확인, 기존 버튼 SpriteSwap |
| hint | (130,501), 640×28 | ENTER / SPACE / ESC 닫기 |

좌표는 프레임 좌상단 기준, Unity y는 음수. 데이터의 제목·본문·분기 수치는 프리팹에 복제하지 않는다. 실제 프리뷰 문구는 편집용 일반 안내뿐이다.

## 상태·수명

- Playing 시작 시 숨김. GameDevManager가 Ended로 바뀌면 1회 표시. 늦게 활성화되어도 현재 결과 표시.
- 확인/Enter/Space/Escape로 닫고 HUD의 결과는 유지. 같은 결과는 Changed/재활성화로 다시 열지 않음. 재시작하면 이전 확인 상태를 지움.
- 표시 중 기존 BunkerSimulation pause owner 및 ShelterCommander input block 사용. Time.timeScale은 바꾸지 않는다. 닫기/비활성/씬 종료에 해제.
- 기존 사건 팝업은 개발 종료 상태 변경 때 기존 동작으로 닫힌다. 엔딩 팝업이 더 위에 표시된다.
- 패널, 텍스트, 버튼, 스크롤, manager/simulation/commander를 직렬화. 런타임 계층 생성 없음.

## 이미지 교체

`Assets/Art/UI/Events/laptop_frame.png`(1120×760), `button_normal.png`, `button_hover.png`, `button_pressed.png`는 기존 공용 이미지이며 사건 팝업도 함께 바뀐다. PNG만 같은 규격으로 교체하고 기존 .meta/Single Sprite/PPU 100/Bilinear를 보존한다. 신규 복제 아트 없음.

## 검증

완료: 실제 데이터 분기, 최종 틱 자동 종료, 입력 차단/해제, 연속 종료/닫기/재시작, 프리팹 중복 설치 방지, Sprite 교체/원본 복구, 1920×1080 및 실제1440×1080 Game View 레터박스 캡처. [검증 기록](../DataHistory/20261118-ending-popup-02-verified.md), [팝업 화면](../../outputs/ending-popup/ending-1920x1080.png), [4:3 화면](../../outputs/ending-popup/ending-1440x1080.png).
