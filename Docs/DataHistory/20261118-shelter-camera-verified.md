# 벙커 부분 시야·자유 이동 카메라 — 2026-11-18

- 작업: `Codex shelter-camera-20261118`.
- 요청: 전체 맵 시야를 줄이고 롤식 자유 이동, 1~6 캐릭터 중심 이동, 같은 번호 연속 입력 추적 고정, Y 해제, 1920×1080 유지 및 임시 시야 조정 지원.
- 사용 데이터 ID: `data.characters`. 기존 Characters 시트 → `Assets/Settings/Data/Csv/Characters.csv` → 공용 DB → `ShelterRoster.Characters`의 `person_01~06`을 참조한다. 시트·CSV·SO·수치·스키마·로더·Addressables 변경 없음.
- [현재 캐릭터 명세](../DataSpecs/Characters.md)와 [목록](../DataRegistry.md)에 카메라 소비 연결을 추가했다. 캐릭터 목록/이름을 별도 데이터로 복제하지 않았다.
- 카메라 화면/입력 설정 정본: 기존 `Assets/Scenes/ShelterPrototype.unity`의 Main Camera/ShelterCamera. 렌더링 설정 범위이며 게임 밸런스 DB를 새로 만들지 않았다. [필드 계약·조작](../ShelterCamera.md).
- 기존 전체 맵 맞춤 `minimumHalfHeight=13.5`, `minimumHalfWidth=20`을 단일 `viewHalfHeight=5.5`로 대체했다. 실제 세로 범위 27 → 11유닛. 화면비는 기존 FixedAspectCamera의 16:9를 유지한다. 구 필드는 씬 저장에서 제거했고 새 중복 카메라 컴포넌트는 없다.
- 시작 위치는 (2.5, 10.4, -10) → 거실 중심 (0, 9.4, -10). 이동 경계는 기존 활성 ShelterArea에서 계산하여 별도 맵 경계 데이터가 없다.
- 중복 검사: Assets/Scripts, Assets/Settings, Assets/Scenes, ProjectSettings, Docs에서 camera/카메라/minimumHalf/edgePan/doubleTap/캐릭터 ID·소비자를 검색했다. 기존 FixedAspectCamera, ShelterCamera, ShelterRoster, CharacterIdentity를 재사용한다. 적용 직전 DataRegistry를 재확인했으며 카메라 데이터의 planned 충돌은 없었다. 파밍 카메라는 변경하지 않았다.

## 실제 검증

- 연결된 Unity 6000.3.28f1 Editor에서 컴파일·Play 수행. `scriptCompilationFailed=false` 확인.
- [Play 검사 57개 통과](../ShelterCamera-checks.txt). 가상 Input System Keyboard/Mouse로 실제 Update 입력 분기 검증: 상단 숫자 1~6, 숫자패드 1~6, 동일 번호 빠른/느린 입력, 다른 번호, Y 해제, 방향키, 마우스 가장자리, 추적 이동, 대각선 속도, 팝업/텍스트 입력 차단, 비활성 대상, 빈 슬롯, 씬 재진입.
- 역순 명단 및 person_06/person_02만 구조한 실제 희소 명단에서 고정 ID 연결 확인. 직접 벙커 실행 시 없는 캐릭터를 생성하지 않는다.
- 1920×1080, 1280×1024, 2560×1080의 계산된 렌더 영역에서 16:9 및 가장자리/레터박스 입력 검사. 창 크기를 실제 OS에서 바꾸는 수동 검사는 하지 않았다.
- 1920×1080 [캡처](../../outputs/ShelterCamera/partial-view.png) 확인: 캐릭터 중심의 거실·주변 일부만 보이고 전체 층/방이 동시에 표시되지 않는다. 기존 HUD가 유지된다.
- 최초 검사 중 시야 크기 변경의 부동소수점 완전 일치 단언만 실패하여 0.001 오차 허용 비교로 고쳤다. 카메라 실제값 4.4 반영을 확인했고 전체 검사를 재실행해 57개 통과했다.
- 컴파일 도메인 리로드 중 Unity CLI 연결이 잠시 끊겼으나 Editor 복구 후 Play 검사와 최종 컴파일 상태를 확인했다.
- Play 검증용 캐릭터는 영구 저장하지 않았으며 Play 종료했다. Player 빌드와 수동 조작감 평가는 수행하지 않았다. 임시 시야·속도·더블 입력 간격은 Inspector에서 조정할 수 있다.
