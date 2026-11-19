# SoundCue 명세

- 담당: 오디오; 문서 스키마 v1; 확인일: 2026-11-18.
- 소유 의미: 소리 하나의 역할에 대한 클립 후보·gain·피치·반복·재생 제한·공간화 설정. 채널/사용자 볼륨은 [SoundSettings](SoundSettings.md)의 다른 계층이다.
- 타입: [GameJam.Audio.SoundCue](../../Assets/Scripts/Audio/SoundCue.cs). 각 SO가 편집 정본이며 CSV/생성 DB는 없다.
- 현재 방식: `SoundEmitter.cue` 등 직접 SO 참조 → `SoundManager.Instance.Play(cue, position, follow)`. 세 에셋은 Addressables에 **등록되어 있지 않다**. 기존 동작을 문서화한 것으로 새 로더/등록을 요구하지 않는다.
- 매니저가 재생 핸들/쿨다운/동시 재생 상태를 관리한다. 공유 Cue를 재생 중 수정하거나 쿨다운 잔여시간을 SO에 저장하지 않는다.

## 개별 인스턴스

| 데이터 ID | 정본 경로 | GUID | 담당 역할 / 상태 |
|---|---|---|---|
| `audio.cue.ui-click` | `Assets/Settings/Audio/UI_Click.asset` | `6da159228393cb146beed0c85f3aacba` | 공용 UI 클릭 / active |
| `audio.cue.sfx-default` | `Assets/Settings/Audio/SFX_Default.asset` | `221eae811fe8c334e8d679d54b961389` | 기본 효과음 설정 예제 / placeholder |
| `audio.cue.ambience-loop` | `Assets/Settings/Audio/Ambience_Loop.asset` | `dcd856e501fd81d409ad87cfb6d87b20` | 반복 환경음 설정 예제 / placeholder |

UI_Click은 10_click 원본을 사용한다. SFX_Default와 Ambience_Loop은 빈 예제다. 신규 Cue도 의미별 ID/역할/경로/GUID를 이 목록과 색인에 개별 추가한다.

## 필드와 현재 값

| 필드 | 타입 | 의미·단위 / 계약 | UI_Click | SFX_Default | Ambience_Loop |
|---|---|---|---|---|---|
| clips | AudioClip[] | 음원 참조 후보; 빈 배열은 placeholder, null 원소 무시 | 10_click.mp3 | [] | [] |
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

조사 범위: SoundCue, SoundManager.Play, SoundEmitter, 세 에셋/메타, 씬·프리팹 참조, Addressables 그룹. 소리 gain과 채널·사용자 볼륨은 곱해지는 서로 다른 의미로 확인했다. 기존 검증: [SoundManager 사용법/검증](../SoundManager.md). v4 적용 내역은 아래 표에서 관리한다.

## 파밍·벙커 사운드 v4

[공유 사운드 표와 원본](https://drive.google.com/drive/u/0/folders/1C4TILvVdK93PNoiu1yqpbPrNuan55sVw)의 초기 설정을 적용한다. 런타임 정본은 각 SoundCue이고 매니저 프리팹이 ID 순서로 참조한다. 01~06은 음악 채널, UI는 UI 채널, 나머지는 효과음 채널이다. 미제작 항목은 빈 clips로 보존하며 음원을 추가하면 같은 재생 지점을 사용한다. 삭제·이전 시 호출과 프리팹 참조를 함께 갱신한다. 필드 타입·범위는 위 공용 스키마를 따른다.

| ID | 정본 | GUID | 재생 조건 | 원본 상태 |
|---|---|---|---|---|
| `audio.cue.01_title` | `Assets/Settings/Audio/01_title.asset` | `e01d49a5543938843bf340cc719cee7e` | 타이틀 화면 | 완료 |
| `audio.cue.02_prologue` | `Assets/Settings/Audio/02_prologue.asset` | `dacc5041c0c01e345a087f0e3c362bf4` | 프롤로그 | 완료 |
| `audio.cue.03_farming` | `Assets/Settings/Audio/03_farming.asset` | `c77457684cfe8e64db2a9be0bf90a9a1` | 60초 파밍 | 완료 |
| `audio.cue.04_bunker` | `Assets/Settings/Audio/04_bunker.asset` | `7c8c2f91784f9d94780505c10d46f8c1` | 벙커 본게임 | 완료 |
| `audio.cue.05_Victory` | `Assets/Settings/Audio/05_Victory.asset` | `01baafba2448743418731f8ce242475a` | Good End 엔딩 (갓겜 · 굿) | 완료 |
| `audio.cue.06_Fail` | `Assets/Settings/Audio/06_Fail.asset` | `d2fc523a2732be945bd060a4a7c45d3d` | Bad End 엔딩 (흠 · 망겜 · 전멸) | 완료 |
| `audio.cue.07_pickup` | `Assets/Settings/Audio/07_pickup.asset` | `3449a1fdfdc27594bbb433d73c44a982` | 캐릭터 · 아이템 드래그 시작 | 완료 |
| `audio.cue.08_drop` | `Assets/Settings/Audio/08_drop.asset` | `179b2aaf45d9c5340921e3c50a831f5a` | 방 · 팀원 카드에 드롭 성공 | 완료 |
| `audio.cue.09_wrong` | `Assets/Settings/Audio/09_wrong.asset` | `586f71e076e4ef846a9c7604ad168254` | 드롭 실패 (정원 초과 · 슬롯 불일치) | 완료 |
| `audio.cue.ui-click` | `Assets/Settings/Audio/UI_Click.asset` | `6da159228393cb146beed0c85f3aacba` | 버튼 클릭 (공통) | 완료 |
| `audio.cue.11_laptop` | `Assets/Settings/Audio/11_laptop.asset` | `eb6bebac7edc82048b1e859ac64ff58f` | 이벤트 팝업 열림 (매 정시) | 완료 |
| `audio.cue.12_hour` | `Assets/Settings/Audio/12_hour.asset` | `c3a0bce7fc7c1e54faaa9dce9520b325` | 정시 알림 | 완료 |
| `audio.cue.13_progress` | `Assets/Settings/Audio/13_progress.asset` | `fe996c7ec24084849981e0ec83122e05` | 진척 전구가 뜰 때 | 완료 |
| `audio.cue.14_sleep` | `Assets/Settings/Audio/14_sleep.asset` | `de59d56a059c81d4faf722a018eed0fc` | 수면 방에서 잠듦 | 완료 |
| `audio.cue.15_wake` | `Assets/Settings/Audio/15_wake.asset` | `aca12ee5b2bfb3847a43d422c7269d28` | 잠에서 깸 | 완료 |
| `audio.cue.16_faint` | `Assets/Settings/Audio/16_faint.asset` | `22fe1165795cc2840b8206b564237d70` | 기절 (멘탈 0) | 완료 |
| `audio.cue.17_alarm` | `Assets/Settings/Audio/17_alarm.asset` | `5960dc755f18eb641bd47cd1470e3175` | 좀비 습격 시작 | 완료 |
| `audio.cue.18_doorbust` | `Assets/Settings/Audio/18_doorbust.asset` | `d20eef68be4a1674db6225465aff81cf` | 침입 (철문 부서짐) | 완료 |
| `audio.cue.19_melee_hit` | `Assets/Settings/Audio/19_melee_hit.asset` | `0c2a2b46cb68e7e43af2d4bffa25f05e` | 근접 타격 성공 | 완료 |
| `audio.cue.20_melee_miss` | `Assets/Settings/Audio/20_melee_miss.asset` | `4506df5be9c3de64e98a319596fa9f29` | 근접 헛손질 | 완료 |
| `audio.cue.21_zombieDown` | `Assets/Settings/Audio/21_zombieDown.asset` | `e417dc7357c01cd43931fd7a7519e40e` | 좀비 처치 | 완료 |
| `audio.cue.22_farmPickup` | `Assets/Settings/Audio/22_farmPickup.asset` | `e2309ef8c43f3944999644225d70d886` | 파밍 아이템 · 식량 줍기 | 완료 |
| `audio.cue.23_tick` | `Assets/Settings/Audio/23_tick.asset` | `4723651d235005e46afcee5298b617f5` | 파밍 마지막 10초 카운트다운 | 완료 |
| `audio.cue.24_hatch` | `Assets/Settings/Audio/24_hatch.asset` | `adae9e499500d154ab4ab069a54147f1` | 파밍 종료 (해치 닫힘) | 완료 |
| `audio.cue.25_stamp` | `Assets/Settings/Audio/25_stamp.asset` | `6108a51d38d645940be127f9034610db` | 중간 평가 · 엔딩 도장 | 미제작 |
| `audio.cue.26_eat` | `Assets/Settings/Audio/26_eat.asset` | `e9c0273d88376cd47a0f111ad77f644b` | 먹기 · 마시기 (통조림 · 몬스터 · 초콜렛 · 박카스) | 미제작 |
| `audio.cue.27_zombieSteps_loop` | `Assets/Settings/Audio/27_zombieSteps_loop.asset` | `7b2c4112c184f7f498f244c88365f2c0` | 좀비가 문까지 걸어오는 동안 | 미제작 |
| `audio.cue.28_canThrow` | `Assets/Settings/Audio/28_canThrow.asset` | `cbdc63a2de108454f9a3bd54d0efc6e7` | 캔 던질 때 | 미제작 |
| `audio.cue.29_canHit` | `Assets/Settings/Audio/29_canHit.asset` | `a3627d354cc6a624f94f1daf6a66aafb` | 캔 명중 | 미제작 |
| `audio.cue.30_canMiss` | `Assets/Settings/Audio/30_canMiss.asset` | `ee98d2ae00e0c3546a0763d192906ed7` | 캔 빗나감 | 미제작 |
| `audio.cue.31_doorBang` | `Assets/Settings/Audio/31_doorBang.asset` | `13cee28eb70723f40ba55dbf6b1d8054` | 좀비가 철문 두드림 | 미제작 |
| `audio.cue.32_defend` | `Assets/Settings/Audio/32_defend.asset` | `661362008e1bcbb489864d5aac8031c7` | 방어 성공 (좀비 체력 0) | 미제작 |

[게임 적용 방식과 플레이 검증](../DataHistory/20261119-audio-v4-applied.md)
