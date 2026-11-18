# 벙커 카메라

편집 위치: `Assets/Scenes/ShelterPrototype.unity` → **Main Camera → Shelter Camera**.
기존 `FixedAspectCamera`의 1920×1080(16:9) 화면비·레터박스를 유지한다. 시작 시야는 가로 약 39.1, 세로 22월드 유닛이다.

## 조작

- WASD 또는 방향키로 자유 이동한다. 마우스를 게임 화면 가장자리로 옮겨도 이동하며 대각선 속도는 동일하다.
- 게임 화면에서 휠을 위로 굴리면 확대하고 아래로 굴리면 축소한다. 캐릭터 추적 중에도 줌을 사용할 수 있다. UI 위와 레터박스에서는 휠 입력을 무시한다.
- 좀비가 등장해도 현재 시점과 줌을 유지한다. 수동으로 실외 접근 구간까지 카메라를 움직일 수 있다.
- `1~6`(숫자패드 포함) 한 번: 해당 캐릭터를 화면 중앙으로 이동하고 자유 시점으로 전환한다. 캐릭터 선택/이동 명령은 기존 마우스 조작을 유지한다.
- 같은 번호를 `Double Tap Seconds` 안에 두 번: 해당 캐릭터를 계속 따라간다. 임시 간격은 0.3초다.
- `Y`: 추적 고정을 풀고 현재 카메라 위치를 유지한다. 자유 시점에서 Y를 눌러도 고정되지 않는다.
- 다른 번호, 자유 이동, Y, 입력 차단/포커스 이탈은 연속 입력 판정을 끊는다. 없는 번호는 시점을 바꾸지 않는다.
- 번호는 기존 `data.characters.id`의 `person_01~06`이다. 구조 순서와 무관하며 구조하지 않은 캐릭터의 슬롯은 비워 둔다. 좀비/비활성 예제 캐릭터는 제외한다.
- 팝업의 `ShelterCommander.InputBlocked` 및 텍스트 입력 중에는 카메라 입력을 받지 않는다. 마우스가 UI 위/레터박스/화면 밖에 있으면 가장자리 이동하지 않는다. Editor에서는 Game 탭에 포커스가 있어야 한다.

## Inspector 설정 계약

아래 값은 씬 컴포넌트의 렌더링/입력 설정이다. 씬이 편집 정본이며 새 SO/CSV/저장 키/로더를 만들지 않는다. 코드의 초기값은 새 컴포넌트 생성용이고 기존 씬 값을 덮어쓰지 않는다. 문서 값은 최초 임시 설정의 기록이다.

| 필드 | 타입·단위 | 임시값 / 제약 | 의미 |
|---|---|---|---|
| View Half Height | float, 월드 유닛 | 11 / 최소·최대 사이 | 시작 시 보이는 세로 범위의 절반 |
| Min Half Height / Max Half Height | float, 월드 유닛 | 4 / 24 | 휠 줌의 한계. 최소값은 0.5 이상, 최대값은 최소값 이상 |
| Character Offset | Vector2, 월드 유닛 | (0, 1) | 캐릭터 발 위치에서 화면 중심까지의 오프셋 |
| Pan Speed | float, 월드 유닛/초 | 12 / 최소 0.1 | 자유 이동 속도, 비배속 시간 기준 |
| Edge Pan | bool | true | 마우스 가장자리 이동 사용 |
| Edge Size | float, 기준 화면 픽셀 | 24 / 1~100 | 1920×1080 기준 가장자리 감지 폭. 실제 viewport 크기에 비례 |
| Boundary Padding | float, 월드 유닛 | 1 / 0 이상 | 활성 방/복도의 외곽에서 카메라 중심을 허용하는 여유 |
| Double Tap Seconds | float, 초 | 0.3 / 최소 0.05 | 같은 번호 두 번 입력 시 추적 전환 시간 |
| Roster | ShelterRoster 참조 | 씬의 기존 명단 | `Characters`에서 기존 ID를 조회. 미지정 시 같은 씬에서 탐색 |
| Commander | ShelterCommander 참조 | 씬의 기존 선택 관리자 | 기존 팝업 입력 차단 재사용. 미지정 시 같은 씬에서 탐색 |

숫자 필드는 `OnValidate`에서 제한한다. 현재 추적 대상/직전 입력/맵 경계는 런타임 상태이며 저장하지 않는다. 맵 경계는 활성 `ShelterArea`에서 계산한다. 끝방 캐릭터도 중앙에 둘 수 있도록 카메라 **중심**을 제한하므로 가장자리에서는 맵 밖 배경이 보일 수 있다. 런타임 맵 구조 변경 후에는 `RefreshMapBounds()`를 호출한다.

편집 모드에서도 시작 시야를 미리 볼 수 있다. 시작 위치는 거실 중심이다. 추적 대상이 비활성화되거나 사라지면 자유 시점으로 돌아간다. 씬 재진입 시 시작 배율로 돌아간다.

## 변경·검증

시작 배율과 줌 범위는 벙커 씬의 Shelter Camera에서 조절한다.

중복 조사 범위: `Assets`, `ProjectSettings`, `Docs`의 camera/카메라/minimumHalf/edgePan/doubleTap 및 `data.characters` 소비자. 캐릭터 정본/ID는 재사용한다. [캐릭터 명세](DataSpecs/Characters.md), [검증 출력](ShelterCamera-checks.txt).

검증 실행: Play 모드에서 `await ShelterCameraChecks.RunPlay()` (Unity CLI `script eval`). 자동 검증은 테스트용 명단을 런타임에 구성하며 마지막에 시각 확인용 6명을 남긴다. Play 종료 시 사라진다. 실행 파일 빌드는 범위 밖이다.
