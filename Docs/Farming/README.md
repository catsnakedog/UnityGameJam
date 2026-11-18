# 파밍 프로토타입

`Assets/Scenes/Farming.unity`를 열어 Play 후 **E** 또는 시작 버튼으로 파밍을 시작합니다. 시작 화면에 표시되는 제한시간은 [FarmingRules](../DataSpecs/FarmingRules.md)의 Duration에서 읽습니다.

| 입력 | 동작 |
| --- | --- |
| A / D | 복도와 호실에서 좌우 이동 |
| E · 문 앞 | 호실 입장 또는 복도 복귀 |
| E · 대상 근처 | 가까운 사람/물자 들기. 합계 4칸을 초과하면 거절 |
| E · 빈 곳 | 마지막으로 든 대상을 전방으로 던져 내려놓기. 다시 줍기 가능 |
| E · 벙커 구역 | 들고 있는 모든 대상을 벙커에 던지기 → 페이드아웃 |
| R · 종료 후 | 파밍 씬을 새로 로드해 재시작 |

복도에서 1번 생존자를 든 상태로 시작합니다. 나머지 다섯 명은 벙커에 가까운 호실부터 2·2·1명씩 배정됩니다. 플레이어와 사람은 벙커와 같은 캐릭터 아트를 사용합니다. 운반 중인 사람은 수평으로 눕습니다. [맵·애니메이션](Map2D.md)

E는 벙커 투입, 문 이동, 근처 대상 들기, 빈 곳에서 내려놓기 순으로 판정합니다. 대상이 근처에 있는데 칸이 모자라면 교체/자동 드롭을 하지 않고 공간 부족을 표시합니다. 짧게 한 번 누를 때 한 동작이며, 벙커에서는 모든 운반분을 한 번에 투입합니다.

남은 시간이 0초가 되면 입력이 멈추고 같은 씬에서 파밍 종료 집계가 표시됩니다. **벙커로 이동** 버튼은 ShelterPrototype의 [게임 개발 단계](../GameDev/README.md)로 연결하고 R은 파밍을 재시작합니다. 시간 안에 던지기를 시작한 투입은 즉시 기록되며 짧은 던지기/페이드는 0초 이후에도 마무리됩니다. 들고만 있던 대상은 집계하지 않습니다. 투입한 사람 ID는 별도 Characters.csv의 능력·멘탈 정의와 연결되어 벙커에 배치됩니다. 물자의 후속 사용과 능력→개발 진행도 환산은 아직 미정입니다.

## 데이터 편집

작업 전에 [공용 데이터 목록](../DataRegistry.md)과 [FarmingData 명세](../DataSpecs/FarmingData.md), [FarmingRules 명세](../DataSpecs/FarmingRules.md)를 확인합니다. 기존 정본을 확장하고 필드/ID/역할을 바꾸면 명세와 이력을 함께 갱신합니다. 같은 무게/용량을 새 CSV나 씬 설정에 복제하지 않습니다.

- 무게 정본: `Assets/Settings/Farming/FarmingData.csv` 하나. 열은 `id,type,weight`.
- `Person`: 2~4칸, `Item`: 1~2칸. weight는 정수. ID는 공백 없는 고유 문자열.
- `Assets/Settings/Farming/FarmingDatabase.asset`는 CSV 임포트 결과입니다. 직접 숫자를 수정하지 않습니다.
- 시작 인물·방별 인원·제한시간·용량·이동속도·상호작용 거리·던지기/페이드 시간: `Assets/Settings/Farming/FarmingRules.asset`.
- CSV 저장 시 자동 검증/임포트합니다. 수동 메뉴는 `Tools > Farming > Import FarmingData CSV`.
- 전체 행 검증 후 적용하며, 오류 또는 기존 ID 누락은 기존 DB를 보존합니다. 신규 ID를 추가해도 씬에 오브젝트가 자동 배치되지는 않습니다. 프리팹의 recordId와 씬 배치를 함께 지정해야 합니다.
- 변경 후 `Tools > Farming > Build Data Content`로 로컬 Addressables 콘텐츠를 갱신합니다. 검증을 위해 Play Mode Script는 Use Existing Build로 설정했습니다. 콘텐츠 빌드는 Player 실행파일 빌드와 다릅니다.
- 이력은 `Docs/DataHistory`에 새 파일로 누적합니다. Rules를 변경한 경우에도 변경 전후 스냅샷을 이력에 남깁니다.

런타임은 씬 소유 `FarmingDataLoader.LoadAsync()`로 `data/farming/database`, `data/farming/rules`를 로드합니다. `Database.Get(recordId)`로 타입/무게를 조회하고 세션 및 운반 상태는 별도 객체에 보관합니다. SO를 씬마다 수동 연결할 필요가 없습니다. 씬 종료 시 로드 핸들을 해제합니다. 데이터 실패 시 시작을 차단하며 임의 기본값으로 진행하지 않습니다.

기존 공용 Data Manager의 ExampleItems와는 별개입니다. 파밍 CSV는 요청한 3열 계약과 전용 검증 규칙을 유지합니다.

## 편집과 확장

- 씬: `Assets/Scenes/Farming.unity`
- UI/사람/아이템/플레이어/테이블: `Assets/Prefabs/Farming`
- UI 이미지: `Assets/Art/UI/Farming` — 같은 경로의 PNG만 바꾸고 `.meta`를 보존합니다. 64×64(게이지 64×16), Single Sprite, PPU100, border 6px, Bilinear, 무압축.
- 한글 폰트: Noto Sans CJK KR, OFL 라이선스는 `Assets/Fonts/OFL.txt`. [원본](https://github.com/notofonts/noto-cjk/tree/main/Sans).
- 공용 1920×1080/16:9 화면비율은 `Docs/UI/Display.md`를 따릅니다.
- 최초 생성 도구는 `Tools > Farming > Create Farming Scene (once)`. 기존 씬이 있으면 수동 편집을 보존하고 재생성하지 않습니다.
- 후속 단계 연결점: `FarmingSession.Completed`, `DepositedIds`, `PeopleSaved`, `ItemsSaved`. 벙커로 이동할 때 투입 Person ID를 FarmingRunTransfer로 넘깁니다. 영구 저장은 없습니다.
- 검증 도구: `FarmingVerification.RunEditChecks()`와 Farming Play/Ready 상태에서 `StartPlayChecks()`. 입력 검증용 가상 키보드와 백그라운드 설정은 검사 후 해제/복원합니다.
