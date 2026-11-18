# 화면 비율 — 1920×1080 고정

사용자 확정(2026-11-18): 기준 해상도는 1920×1080, 게임 표시 비율은 16:9. 다른 창/모니터 비율에서는 중앙에 화면 전체를 비례 축소·확대하고 남는 영역을 검게 표시한다.

| 출력 크기 | 게임 영역 | 검은 여백 |
| --- | --- | --- |
| 1920×1080 | 1920×1080 | 없음 |
| 1280×720 | 1280×720 | 없음 |
| 1440×1080 | 1440×810 | 위·아래 각 135px |
| 2560×1080 | 1920×1080 | 좌·우 각 320px |
| 1920×1200 | 1920×1080 | 위·아래 각 60px |

## 공용 글꼴

게임 화면의 공용 글꼴은 Neo둥근모다. 캐릭터와 방의 월드 이름표는 `Assets/Fonts/NeoDunggeunmo.ttf`를 직접 사용한다. 말풍선, 파밍 HUD, 사건·엔딩 팝업은 같은 TTF로 만든 동적 TextMeshPro 에셋 `Assets/Fonts/FarmingKorean.asset`을 사용한다. 이 에셋은 새 TMP 텍스트의 프로젝트 기본값이기도 하다. 원본과 함께 배포하는 이용 조건은 `Assets/Fonts/NeoDunggeunmo-OFL.txt`에 있다. [공식 배포본](https://github.com/neodgm/neodgm/releases/tag/v1.601)

## 적용 위치

- `Assets/Scripts/Common/FixedAspectCamera.cs`: 화면 크기에 맞춰 카메라 Viewport Rect를 중앙 16:9로 유지하고 검은 여백을 그린다. 렌더 텍스처용 카메라는 제외한다.
- `ShelterCamera`, `FarmingCamera`: 고정 비율 컴포넌트를 요구하며 창 비율에 따른 추가 줌아웃을 하지 않는다.
- `Assets/Editor/FixedAspectSetup.cs`: `Tools > Display > Apply 1920x1080 Letterbox`로 기존 씬의 MainCamera와 파밍 HUD 프리팹에 적용한다. Desktop/Web 기본 크기를 1920×1080으로 설정한다.
- `FarmingSceneBuilder`: HUD 생성 시 CanvasScaler는 1920×1080 + Expand, 내부 `Viewport 1920x1080`은 FitInParent 16:9 + RectMask2D로 구성한다. 기존 HUD 자식/바인딩을 유지한다. UI 앵커는 이 내부 영역을 기준으로 한다.
- 새 화면용 카메라에는 `FixedAspectCamera`를 부착한다. 새 Overlay Canvas는 편집 시 `FixedAspectSetup.ConfigureCanvas`로 설정한 뒤 저장한다.

## 검증 (2026-11-18)

- Unity 6000.3.28f1에서 스크립트 컴파일 통과.
- `ShelterPrototype` Play Mode의 실제 Game View를 1920×1080 → 1440×1080 → 2560×1080으로 변경하여 캡처·시각 검수. 화면 구도와 orthographicSize=9.1125 유지, 상하·좌우 검은 여백 확인.
- 1280×720, 1920×1200, 1080×1920을 포함한 6가지 크기의 계산 결과: 중앙 정렬, 16:9 비율 확인.
- 임시 Canvas로 4:3 및 울트라와이드에서 카메라와 HUD 영역 일치 확인. 논리 영역은 두 경우 모두 1920×1080. 설정을 두 번 적용해도 자식 중복/참조 손실 없음.
- 검수 캡처: `Temp/FixedAspect/shelter-1920x1080.png`, `shelter-1440x1080.png`, `shelter-2560x1080.png` (Unity 임시 폴더이므로 영구 보관 대상 아님).
- 검수 당시 파밍 씬/HUD 프리팹은 아직 생성되지 않아 생성기 설정과 공용 Canvas 동작을 검증했다. 파밍 완성 화면의 별도 시각 검수는 해당 씬 생성 후 수행한다.
- Player 실행 파일 빌드는 수행하지 않았다.
