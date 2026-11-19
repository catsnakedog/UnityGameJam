# 사운드 기본 설정과 사용자 저장값 명세

- 데이터 ID: `audio.manager-defaults`, `audio.user-settings`; 상태: active; 담당: 오디오.
- 문서 스키마 v1; 확인일: 2026-11-18. 서로 다른 의미의 기본값과 사용자 저장값을 함께 설명한다.
- 구현: [SoundManager.cs](../../Assets/Scripts/Audio/SoundManager.cs), SoundSettingsRelay. 개별 음원 설정은 [SoundCue](SoundCue.md).

## 공용 매니저 기본값

정본은 [SoundManager.prefab](../../Assets/Prefabs/Audio/Resources/SoundManager.prefab), GUID `d3d126cb33088ce48a322a28d981a189`, Resources 키 `SoundManager`. 현재 Addressables 등록 없음. 매니저가 자동 생성되어 씬 사이에서 유지된다. 같은 Resources 이름/설정 역할을 가진 프리팹을 추가하지 않는다. 런타임 클래스의 초기값은 코드 생성 경로의 값이므로 프리팹과 서로 다른 두 밸런스 프로필로 운용하지 않는다.

| 필드 | 타입 | 의미/단위 | 현재 값 | 제약 |
|---|---|---|---|---|
| initialVoices | int | 사전 생성 효과음 소스 / 개 | 8 | 1 이상, 최대 풀 범위 고려 |
| maxVoices | int | 효과음 풀 상한 / 개 | 32 | 1 이상 |
| stopEffectsOnSceneChange | bool | 활성 씬 전환 시 효과음 정리 | true | 음악 유지와 구분 |
| pauseOnFocusLoss | bool | 포커스 상실 일시정지 | true | 사용자 일시정지 상태와 별도 |
| masterVolume | float | 최초 Master gain / 무차원 | 1 | 0~1, 저장값 우선 |
| musicVolume | float | 최초 Music gain / 무차원 | 0.7 | 0~1, 저장값 우선 |
| sfxVolume | float | 최초 SFX gain / 무차원 | 1 | 0~1, 저장값 우선 |
| uiVolume | float | 최초 UI gain / 무차원 | 1 | 0~1, 저장값 우선 |
| ambienceVolume | float | 최초 Ambience gain / 무차원 | 0.8 | 0~1, 저장값 우선 |
| muted | bool | 최초 음소거 상태 | false | 저장값 우선 |
| musicGroup | AudioMixerGroup | Music 출력 경로 | null | 선택 참조 |
| sfxGroup | AudioMixerGroup | SFX 출력 경로 | null | 선택 참조 |
| uiGroup | AudioMixerGroup | UI 출력 경로 | null | 선택 참조 |
| ambienceGroup | AudioMixerGroup | Ambience 출력 경로 | null | 선택 참조 |

원본 프리팹 필드는 구조상 필수이며 mixer null은 허용한다. 범위 속성/런타임 처리는 SoundManager 코드가 담당한다. 게임 전체의 새 초기 설정을 만들려면 이 프리팹 계약을 확장하고 별도 AudioSettings.csv/씬별 복제 기본값을 만들지 않는다. 형식 이전이 필요하면 기존 자동 생성 경로와 사용자 저장값 우선순위를 함께 이전한다.

## 사용자 저장값

편집 정본은 사용자의 PlayerPrefs이고 Assets의 설정 파일이 아니다. 키 prefix는 정확히 `GameJam.Audio.`다. 현재 사용자별 실제 저장값은 이번 작업에서 읽지 않았으며 아래 표는 저장 스키마다.

| 키 | 저장 타입 | 의미/단위 | 누락 시 / 제약 |
|---|---|---|---|
| GameJam.Audio.Master | float | 사용자 Master gain / 무차원 | 초기 매니저 값, 0~1 |
| GameJam.Audio.Music | float | 사용자 Music gain / 무차원 | 초기 매니저 값, 0~1 |
| GameJam.Audio.Sfx | float | 사용자 SFX gain / 무차원 | 초기 매니저 값, 0~1 |
| GameJam.Audio.UI | float | 사용자 UI gain / 무차원 | 초기 매니저 값, 0~1 |
| GameJam.Audio.Ambience | float | 사용자 Ambience gain / 무차원 | 초기 매니저 값, 0~1 |
| GameJam.Audio.Muted | int | 사용자 음소거 / 0(false), 1(true) | 초기 매니저 값; 기존 읽기 코드는 0 이외의 값도 true로 처리 |

설정 UI는 SoundManager의 Set*Volume/SetMuted API를 호출하고 표시할 때 현재 getter를 읽는다. SoundSettingsRelay는 이벤트 연결용이며 새 저장소가 아니다. 변경 후 지연 저장과 앱 중단/종료 저장, 명시적 SaveSettings는 매니저가 담당한다. 키를 변경/삭제할 때는 기존 사용자 저장값 이전을 설계하며 다른 기능의 PlayerPrefs를 지우지 않는다.

## 중복 조사와 검증

SoundManager 필드/PlayerPrefs 호출, 실제 Resources 프리팹, SoundSettingsRelay, SoundCue, SceneMusic을 확인했다. 기본 볼륨 → 사용자 저장값 우선 적용 → 개별 음원 gain과 곱셈이라는 역할로 구분한다. 기본값과 사용자 값은 의도된 계층이며 독립 편집 정본 중복이 아니다. 개별 씬의 SceneMusic에는 음악 클립/전환 설정이 있으나 사용자 음량이나 글로벌 풀 설정을 복제하는 용도로 사용하지 않는다.

기존 검증 결과/사용법은 [SoundManager.md](../SoundManager.md). 이번 작업은 계약/현재 프리팹 값을 문서화했으며 사용자 설정·게임 데이터·오디오 코드는 변경하지 않았다.

매니저 프리팹의 `gameCues`는 사운드 표 번호로 [역할별 SoundCue](SoundCue.md)를 참조한다. 0번은 비워 두며 각 역할의 gain과 반복은 Cue에 저장한다. 이벤트·중간 발표의 일시적 음악 감쇠는 현재 벙커 음악에만 적용한다.
