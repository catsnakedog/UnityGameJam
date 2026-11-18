# 사운드 매니저

`GameJam.Audio` 네임스페이스의 공용 오디오 시스템입니다. 어느 씬에서 Play해도 자동 생성되며 씬 사이에서 유지됩니다.

데이터 작업 전에 [공용 데이터 목록](DataRegistry.md), [SoundCue 명세와 기존 인스턴스](DataSpecs/SoundCue.md), [사운드 기본값/저장값 명세](DataSpecs/SoundSettings.md)를 확인합니다. 새 Cue는 같은 소리 역할의 기존 에셋을 먼저 찾아 재사용하고, 실제로 다른 역할이 필요하면 명세와 목록에 개별 등록합니다. 새 볼륨 저장 키나 설정 복제본을 임의로 만들지 않습니다.

## 파일과 설정

| 위치 | 용도 |
| --- | --- |
| `Assets/Scripts/Audio` | 런타임 코드와 독립 어셈블리 |
| `Assets/Prefabs/Audio/Resources/SoundManager.prefab` | 자동 로딩되는 공용 매니저 설정 |
| `Assets/Settings/Audio` | SoundCue 예제. 실제 AudioClip을 연결해서 사용 |
| `Assets/Tests/PlayMode/Audio` | PlayMode 회귀 테스트 |

프리팹의 기본값은 사전 생성 효과음 8개, 최대 32개, Master 1 / Music 0.7 / SFX 1 / UI 1 / Ambience 0.8입니다. 프리팹은 씬에 따로 배치할 필요가 없습니다. `SoundManager`라는 Resources 이름은 하나만 유지하세요. 씬에 직접 배치하려면 전용 루트 GameObject에 넣으세요. 최초 활성 매니저가 사용됩니다.

음원이 아직 없는 프로젝트이므로 예제 SoundCue의 Clips는 비어 있습니다. 소리 파일을 프로젝트에 추가한 뒤 연결해야 실제 소리가 납니다. 씬의 카메라에는 활성 AudioListener가 하나 있어야 합니다. 매니저는 AudioListener를 추가하지 않습니다.

## 코드에서 재생

```csharp
using GameJam.Audio;
using UnityEngine;

// AudioClip 필드에 Inspector에서 음원을 연결합니다.
SoundManager.Instance.PlayMusic(bgmClip); // 0.5초 크로스페이드, 반복
SoundManager.Instance.PlaySfx(hitClip);
SoundManager.Instance.PlayUI(clickClip);
SoundManager.Instance.PlayAtPosition(explosionClip, transform.position);

// SoundCue는 음원 변형, 볼륨, 피치, 채널, 반복, 재생 제한을 묶습니다.
SoundHandle sound = SoundManager.Instance.Play(engineCue, transform.position, transform);
sound.Stop(); // 해당 재생만 정지. 풀에서 같은 소스가 재사용되어도 안전
SoundManager.Instance.StopMusic(0.5f);
SoundManager.Instance.StopChannel(SoundChannel.Ambience);
SoundManager.Instance.StopAllEffects();
```

SoundCue는 Project 창의 `Create > Audio > Sound Cue`에서 추가합니다. `Clips` 중 비어 있지 않은 음원을 무작위로 선택합니다. `Cooldown`은 같은 Cue의 재생 간격, `Max Instances`는 동시 재생 수(0은 개별 제한 없음)입니다. `Spatial Blend` 0은 2D, 1은 3D이고 UI 채널은 항상 2D입니다. 3D 거리 감쇠는 선형이며 Min/Max Distance를 사용합니다. `follow` 대상이 파괴되면 재생이 종료됩니다.

풀이 가득 차면 숫자가 큰 Priority의 비반복 소리 중 가장 오래된 소리를 교체합니다. 더 중요한 소리와 반복음은 보호되므로 자리가 없으면 무효 핸들을 반환합니다. `IsValid`는 재생/일시정지 중 true이며 완료 후 다음 업데이트에 false가 됩니다. AudioSource를 외부에서 직접 조작하지 말고 매니저/핸들을 사용하세요.

## Inspector 이벤트와 씬 연결

- 버튼이나 애니메이션 오브젝트에 `SoundEmitter`를 추가하고 Cue를 연결한 뒤 `Play()`를 이벤트에 지정합니다. `Play On Enable`, 위치 추적, 비활성화 시 정지를 설정할 수 있습니다. 반복음은 항상 비활성화 시 정지합니다.
- 씬에 `SceneMusic`을 추가하고 Clip을 연결하면 Start 때 BGM이 전환됩니다. 같은 곡은 재시작하지 않습니다. 음악 컴포넌트가 없는 다음 씬에서는 이전 곡이 계속됩니다.
- 설정 화면 오브젝트에 `SoundSettingsRelay`를 추가합니다. Slider의 동적 float 이벤트를 `SetMasterVolume`, `SetMusicVolume`, `SetSfxVolume`, `SetUIVolume`, `SetAmbienceVolume`에 연결합니다. 값 범위는 0~1입니다. Toggle의 bool 이벤트는 `SetMuted`에 연결합니다.
- 설정 화면을 열 때 Slider에 `SoundManager.Instance.MusicVolume` 같은 현재 값을 `SetValueWithoutNotify`로, 음소거 Toggle에는 `Muted`를 `SetIsOnWithoutNotify`로 반영하세요.

## 일시정지·전환·저장

```csharp
SoundManager.Instance.SetPaused(true);   // 음악·게임 효과음·환경음 일시정지, UI 소리 유지
SoundManager.Instance.SetPaused(false);  // 기존 위치에서 재개
SoundManager.Instance.SetMusicVolume(0.6f);
SoundManager.Instance.SetMuted(true);    // 기존 채널 볼륨을 보존
SoundManager.Instance.SaveSettings();    // 옵션 화면 종료 시 명시적으로 저장 가능
```

`Time.timeScale = 0`만으로 오디오는 멈추지 않습니다. 게임 일시정지와 함께 `SetPaused`를 호출하세요. 페이드는 unscaled 시간으로 동작하지만 매니저/리스너 일시정지 중에는 정지합니다. 앱 중단과 포커스 상실 시 전체 소리를 일시정지하며, 포커스 처리는 프리팹에서 해제할 수 있습니다. 사용자 일시정지 상태는 앱 복귀 후에도 유지됩니다.

기본적으로 활성 씬이 바뀌면 효과음(UI/환경음 포함)을 정리하고 음악은 유지합니다. Additive 로딩만 하고 활성 씬을 바꾸지 않으면 정리하지 않습니다. 이 동작은 `Stop Effects On Scene Change`로 설정합니다.

볼륨과 음소거는 `GameJam.Audio.*` PlayerPrefs에 저장됩니다. 조작 중에는 마지막 변경 후 0.5초 뒤 디스크에 저장하고 앱 중단/종료 때도 저장합니다. 저장된 사용자 설정이 프리팹 기본값보다 우선합니다. 다른 PlayerPrefs는 건드리지 않습니다.

AudioMixer가 없어도 모든 채널 제어가 동작합니다. 리버브/덕킹 등 믹서 효과가 필요하면 프리팹의 Optional Mixer Routing에 각 채널 그룹을 연결하세요. 출력 볼륨은 `음원 볼륨 × 채널 볼륨 × 마스터 볼륨`입니다. 외부 AudioSource에는 적용되지 않습니다.

## 검증

Unity Test Runner의 PlayMode에서 `GameJam.Audio.Tests.SoundManagerTests`를 실행합니다. 자동 로딩, 중복 방지, 풀 포화/우선순위, 핸들 재사용, 일시정지/UI, 리스너 일시정지, 자연 종료, 볼륨/음소거, 설정 복원, BGM 전환/재시작 방지, 재생 제한, 위치 추적, 씬 전환을 확인합니다. 테스트는 무음 런타임 클립을 만들고 변경한 PlayerPrefs를 복원합니다.

2026-11-18, Unity 6000.3.28f1에서 PlayMode 테스트 **18/18 통과**. 무음 BGM의 재생 위치 유지, 일시정지 중 크로스페이드 정지 및 재개도 검증했습니다. 실제 음원의 청감·음량 밸런스는 음원 연결 후 확인해야 합니다.

Unity의 [AudioSource.isPlaying 문서](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource/isplaying)에 따라 일시정지 상태를 별도로 추적하여 정지된 소리가 풀에서 회수되지 않도록 합니다.
