# DevGageUI

사용자 확정: 화면 우측 직사각형 패널, 기획·프로그래밍·그래픽 세 분야의 0~100% 진행도. 아래 치수·문구는 기존 벙커 화면에 맞춘 수정 가능한 기본안이다.

- 씬: `Assets/Scenes/ShelterPrototype.unity`; 프리팹: `Assets/Prefabs/UI/DevGageUI.prefab`.
- 코드: `Assets/Scripts/GameDev/DevGageUI.cs`, `GameDevManager.cs`; 제작: `Assets/Editor/GameDev/GameDevSetup.cs`.
- 1920×1080 논리 영역, 기존 FixedAspectSetup의 16:9/letterbox 규칙. 우측 상단 (32, 160) 여백, 320×430 패널. 3개의 수평 게이지와 우측 숫자. 시작 0%, 최대 100%, 소수점 1자리까지.
- 정적 계층: Canvas → Viewport 1920x1080 → Development panel → Title / Subtitle / Planning / Programming / Graphics / Status. 각 분야는 Label / Percentage / Track / Fill. 전체 진행도를 점수로 표시하지 않는다.
- 상태: 로딩(데이터 준비), 개발(값 갱신), 점수 대기(개발 종료), 엔딩(총점·CSV 제목/본문), 실패(데이터 확인). 엔딩 본문은 길이에 맞춰 줄바꿈, 과도한 본문은 별도 결과 UI 확장 대상.
- 입력: 읽기 전용 HUD. 패널 영역은 월드 클릭을 막고 나머지는 통과. 신규 생산 속도·작업 배정·개발 버튼은 이 요청에서 규칙이 미정이다. 다른 게임 기능에서 Manager API를 호출한다.
- 바인딩: 직렬화된 Manager/게이지/TMP 참조. OnEnable 구독, OnDisable 해제, 재바인딩 시 이전 구독 해제. 점수 대기/엔딩 후 진행도 변경 차단.
- 사용자 범위 확정: 진행도 증가/개발 종료 조건은 후속 작업. 100% 게이지 표시만으로 점수 대기나 엔딩으로 전환하지 않는다. 외부의 명시적 `FinishDevelopment()` 호출 이후에만 점수 처리 단계로 이동한다.
- 기존 이미지 재사용: `Assets/Art/UI/Farming/panel.png` (64×64, Single, 9-slice 6), `gauge.png` (64×16, Single, 수평 Filled). PPU 100, Bilinear, Uncompressed, mipmap off. 흰색 Tint로 원본 표현 유지. Track에도 panel Sprite 사용.
- 폰트: 기존 `Assets/Fonts/FarmingKorean.asset` (한글 TMP). 텍스트는 이미지로 굽지 않는다.
- 이미지 교체: 정확한 기존 경로 PNG 바이트만 덮어쓰고 .meta/Single 설정을 보존. 공유 이미지라 Farming HUD에도 반영된다. 이번 작업은 새 이미지/복제본을 만들지 않는다.
- 검사: 0/부분/100%·세 분야 독립, 데이터 오류·엔딩 표시, 1920×1080 및 4:3/ultrawide 실제 캡처, 재바인딩/씬 재로드/재실행, Sprite 식별자 보존. 실행 결과는 후속 검증 문서에 기록.

## 검증 결과 (2026-11-18)

- 저장된 프리팹 GUID `6e70d210995c7134dad415b09132670e`. 제작 도구 재실행 후 씬 Manager/UI 각 1개, 직렬화 Manager 참조·모든 Sprite/TMP 참조·Missing Script 0 확인.
- 빌드된 Addressables 데이터로 실제 Play, 0/부분/100% 및 엔딩 표시 확인. 재바인딩·비활성 후 활성·씬 재진입에서 값 동기화와 초기화 확인.
- 1920×1080, 1440×1080, 2560×1080 실제 Game View 캡처/시각 검수. 검은 여백과 HUD 영역 정렬, 한글/퍼센트/엔딩 본문 표시 확인.
- 실제 EventSystem Raycast: 패널이 월드 클릭을 차단하며 패널 밖 월드 영역은 통과.
- 공유 gauge.png의 한 픽셀을 바꿔 재임포트 후 프리팹의 세 Fill 참조가 유지됨을 검사하고 원본 바이트 복구. GUID `b7ef226803a28ab4ab4bff472a5b6c67`, Sprite local ID `21300000`. panel.png GUID `3a153a4aac619734595107192bd960bc`.
- [부분 진행 화면](../GameDev/Preview-progress.png), [엔딩 화면](../GameDev/Preview-ending.png), [4:3](../GameDev/Preview-4x3.png), [울트라와이드](../GameDev/Preview-wide.png), [실행 검사](../GameDev/Verification-play.txt).
