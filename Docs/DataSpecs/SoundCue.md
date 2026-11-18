# SoundCue 명세

- 담당: 오디오; 문서 스키마 v1; 확인일: 2026-11-18.
- 소유 의미: 소리 하나의 역할에 대한 클립 후보·gain·피치·반복·재생 제한·공간화 설정. 채널/사용자 볼륨은 [SoundSettings](SoundSettings.md)의 다른 계층이다.
- 타입: [GameJam.Audio.SoundCue](../../Assets/Scripts/Audio/SoundCue.cs). 각 SO가 편집 정본이며 CSV/생성 DB는 없다.
- 현재 방식: `SoundEmitter.cue` 등 직접 SO 참조 → `SoundManager.Instance.Play(cue, position, follow)`. 세 에셋은 Addressables에 **등록되어 있지 않다**. 기존 동작을 문서화한 것으로 새 로더/등록을 요구하지 않는다.
- 매니저가 재생 핸들/쿨다운/동시 재생 상태를 관리한다. 공유 Cue를 재생 중 수정하거나 쿨다운 잔여시간을 SO에 저장하지 않는다.

## 개별 인스턴스

| 데이터 ID | 정본 경로 | GUID | 담당 역할 / 상태 |
|---|---|---|---|
| `audio.cue.ui-click` | `Assets/Settings/Audio/UI_Click.asset` | `6da159228393cb146beed0c85f3aacba` | 공용 UI 클릭 / placeholder |
| `audio.cue.sfx-default` | `Assets/Settings/Audio/SFX_Default.asset` | `221eae811fe8c334e8d679d54b961389` | 기본 효과음 설정 예제 / placeholder |
| `audio.cue.ambience-loop` | `Assets/Settings/Audio/Ambience_Loop.asset` | `dcd856e501fd81d409ad87cfb6d87b20` | 반복 환경음 설정 예제 / placeholder |

세 에셋 모두 clips가 비어 있다. 이번 검색에서 세 GUID의 씬/프리팹 참조는 발견하지 못했다. 실제 소리 재생·청감은 미연결 상태이며, 지원 API가 있다는 사실과 실제 연결 완료를 구분한다. 신규 Cue도 의미별 ID/역할/경로/GUID를 이 목록과 색인에 개별 추가한다.

## 필드와 현재 값

| 필드 | 타입 | 의미·단위 / 계약 | UI_Click | SFX_Default | Ambience_Loop |
|---|---|---|---|---|---|
| clips | AudioClip[] | 음원 참조 후보; 빈 배열은 placeholder, null 원소 무시 | [] | [] | [] |
| channel | SoundChannel | Sfx=0, UI=1, Ambience=2 | UI | Sfx | Ambience |
| volume | float | 개별 소리 gain / 0~1, 무차원 | 1 | 1 | 1 |
| minPitch | float | 최소 피치 배율 / 무차원 | 1 | 1 | 1 |
| maxPitch | float | 최대 피치 배율 / 무차원, minPitch 이상 | 1 | 1 | 1 |
| loop | bool | 반복 재생 | false | false | true |
| priority | int | 우선순위 / 0~256, 작을수록 중요 | 128 | 128 | 128 |
| cooldown | float | 같은 Cue의 최소 재생 간격 / unscaled 초, 0 이상 | 0.04 | 0.05 | 0 |
| maxInstances | int | 같은 Cue 동시 재생 수 / 개, 0은 개별 제한 없음 | 4 | 8 | 1 |
| spatialBlend | float | 2D(0)~3D(1) 혼합 / 무차원, UI는 2D | 0 | 0 | 0 |
| minDistance | float | 거리 감쇠 시작 / Unity 거리 단위, 양수 | 1 | 1 | 1 |
| maxDistance | float | 거리 감쇠 끝 / Unity 거리 단위, minDistance 이상 | 30 | 30 | 30 |

위 값은 현재 SO 스냅샷. 새 SO의 C# 필드 초기값은 [SoundCue.cs](../../Assets/Scripts/Audio/SoundCue.cs)에 있고 세 예제의 cooldown/maxInstances/loop 오버라이드와 구분한다. 필드는 구조상 존재하며 음원 참조 외에 “빈 문자열” 개념은 없다. Inspector Range/Min 속성과 SoundManager의 런타임 범위 보정이 있지만 CSV 임포터처럼 전체를 거부하는 독립 Validate 함수는 없다. 재생 피치는 현재 매니저에서 0.01~3 범위로 제한한다. 문서화가 새 유효성 검사를 구현한 것은 아니다.

## 추가·참조·삭제와 중복 방지

- 새 Cue를 만들기 전에 소리의 역할, 채널, 후보 클립, 제한 정책을 비교한다. 같은 클릭음을 버튼별로 복제하지 말고 기존 UI_Click을 참조한다.
- 다른 역할의 음원이 같은 클립을 사용하는 것은 가능하나, 별도 재생 제한/피치/의미가 필요한 이유를 명세에 적는다. 같은 클립이라고 무조건 같은 Cue인 것은 아니다.
- 동일 역할의 음원 변형은 먼저 기존 clips 후보에 추가할 수 있는지 검토한다. UI_Click2처럼 근거 없이 이름만 바꾸지 않는다.
- 재생 제한은 **Cue 객체별**로 적용된다. 동일 역할의 복제 에셋을 만들면 쿨다운/동시 재생 제한도 분리되므로 중복을 특히 주의한다.
- 삭제 시 SoundEmitter/이벤트/직접 코드 참조를 검사하고 동일 역할의 정본으로 전환한다. 외래키는 Unity 에셋 참조이며 안정성을 위해 GUID를 보존한다.

조사 범위: SoundCue, SoundManager.Play, SoundEmitter, 세 에셋/메타, 씬·프리팹 참조, Addressables 그룹. 소리 gain과 채널·사용자 볼륨은 곱해지는 서로 다른 의미로 확인했다. 기존 검증: [SoundManager 사용법/검증](../SoundManager.md). 이번에는 파일 대조만 수행했으며 음원/수치/참조 변경 없음.
