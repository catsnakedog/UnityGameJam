# AI 데이터 작업 지침과 명세 · documented

- 기록 ID: 20261118-data-governance-01-documented.
- 프로젝트: UnityGameJam.
- 기록일/시간대: 2026-11-18 / Asia/Seoul.
- 요청 근거: AI가 데이터 작업 때 참고할 Markdown과 데이터별 명세를 작성해 중복 생성을 예방하도록 사용자 요청.
- 단계: documented. 기존 데이터의 문서화이며 CSV/SO/프리팹/코드/저장값 변경이나 데이터 이전은 수행하지 않았다.
- 관련 이전 기록: [데이터 도구 검증](20261118-data-tool-02-verified.md), [파밍 검증/전체 값](20261118-143045-3883526-verified.md).

## 추가·갱신 문서

- 루트 `AGENTS.md`: 데이터 작업 전 필독 문서, 정본 재사용, 생성 전 명세/목록 등록, 변경 이력, 적용 직전 재확인 규칙.
- [DataRegistry](../DataRegistry.md): 10개 데이터 ID의 현재 상태·담당 의미·정본·명세 색인. 이름뿐 아니라 의미/대상/단위로 중복을 판단하도록 규정.
- `Docs/DataSpecs`: 7개 현재 명세와 `_Template.md`. 필드 의미/타입/단위/제약, 기본값과 현재 값 구분, ID/참조/삭제 정책, 소비자/수명, 중복 조사 근거.
- `Docs/DataManager.md`, `Docs/Farming/README.md`, `Docs/SoundManager.md`: 공용 목록과 관련 명세로 진입 링크 추가.

## 문서화한 연결

| 데이터 ID | 정본/생성 관계 | 명세 |
|---|---|---|
| data.example-items | ExampleItems.csv → GameDatabase.ExampleItems → 공용 조회 | [ExampleItems](../DataSpecs/ExampleItems.md) |
| data.farming.pickups | FarmingData.csv → FarmingDatabase → 씬 소유 FarmingDataLoader | [FarmingData](../DataSpecs/FarmingData.md) |
| data.farming.rules | FarmingRules SO → 씬 소유 FarmingDataLoader | [FarmingRules](../DataSpecs/FarmingRules.md) |
| audio.cue.ui-click | UI_Click SO → SoundManager.Play 직접 참조 API, 음원 미연결 | [SoundCue](../DataSpecs/SoundCue.md) |
| audio.cue.sfx-default | SFX_Default SO → 같은 스키마의 별도 역할 예제, 음원 미연결 | [SoundCue](../DataSpecs/SoundCue.md) |
| audio.cue.ambience-loop | Ambience_Loop SO → 반복 환경음 역할 예제, 음원 미연결 | [SoundCue](../DataSpecs/SoundCue.md) |
| audio.manager-defaults | 기존 Resources/SoundManager 프리팹 기본값 | [SoundSettings](../DataSpecs/SoundSettings.md) |
| audio.user-settings | 기존 GameJam.Audio.* PlayerPrefs 6개 키 | [SoundSettings](../DataSpecs/SoundSettings.md) |
| system.game-database | 공용 CSV의 생성 컨테이너; 독립 편집 정본 없음 | [GameDatabase](../DataSpecs/GameDatabase.md) |
| editor.data-tool | 기존 ProjectSettings/GameDataTool.asset | [DataToolSettings](../DataSpecs/DataToolSettings.md) |

각 명세에 기존 6개 데이터 SO 및 사운드 매니저 프리팹의 실제 GUID를 기록했다. 공용/파밍의 기존 Addressables 주소·그룹·라벨을 유지했다. 오디오 SO/프리팹은 현재 Addressables 미등록 상태임을 명시했다. 렌더러/머티리얼·패키지 내부 에셋은 이 데이터 색인의 범위가 아니다.

## 중복 조사 결과

- ExampleItems.weight는 데모용 float이고 FarmingData.weight는 실제 운반 칸 수 int이므로 같은 게임 규칙으로 취급하지 않는다.
- FarmingRules.capacity와 개체 weight는 전체 한도와 개체 비용의 관계다. 새 인벤토리/HUD용 정본을 만들지 않도록 기록했다.
- SoundCue.volume, 공용 초기 채널 볼륨, 사용자 저장 볼륨은 서로 다른 적용 계층이다. 동일 역할의 Cue 복제는 재생 제한을 분산시키므로 재사용 우선으로 규정했다.
- GameDatabase와 생성 접근자, 런타임 캐시는 파생물이며 새 데이터 정의로 복제하지 않는다.
- 현 목록과 실제 파일에서 위 계약의 두 번째 독립 편집 정본은 발견하지 못했다. 이는 명시한 범위의 파일 조사 결과이며 자동 의미 중복 판정 기능을 구현했다는 뜻은 아니다.

## 검사

- 문서 로컬 링크의 대상 존재, 데이터 ID 고유성, 두 CSV의 모든 헤더, FarmingRules/SoundCue/SoundManager 직렬화 필드의 명세 기재 여부를 검사했다.
- 관리 대상 6개 SO GUID의 명세 포함, 도구 설정 필드 6개, PlayerPrefs 키 6개가 문서에 포함되는지 검사했다.
- 최초 검사: 현재 안내 문서 13개, 로컬 링크 56개, 목록 10개 항목, 명세 7개 모두 통과. 이 이력과 링크 추가 후에도 링크 검사를 다시 수행한다.
- 이번 변경은 Markdown뿐이므로 Unity 컴파일/Play/콘텐츠 빌드의 재실행은 하지 않았다. 기존 동작 검증은 위 관련 기록을 참조한다.
- 명세/의미 중복 확인은 AI 작업 절차이며 임포터의 자동 차단 규칙으로 추가된 것은 아니다. 여러 작업의 예약은 파일 잠금이 아니므로 병합 전 최신 목록 확인이 필요하다.
