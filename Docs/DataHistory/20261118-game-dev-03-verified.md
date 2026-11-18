# game-dev-20261118 / verified

- 작성: 2026-11-18, Asia/Seoul. 요청: GameDevManager의 세 분야 진행도·분리 점수 계산 파일·CSV 엔딩·우측 DevGageUI.
- 선행: [01 planned](20261118-game-dev-01-planned.md), [02 import blocked](20261118-game-dev-02-import-blocked.md).
- 생성 적용: [CSV/DB snapshot](20261118-144608-7380613-data-applied.md). 콘텐츠: [Addressables built](20261118-144703-0523410-data-built.md).

## 현재 데이터 연결

| ID | 정본 / 생성물 / 실제 연결 |
|---|---|
| data.game-dev.endings | 정본 `Assets/Settings/Data/Csv/GameDevEndings.csv` (UTF-8 BOM), id/displayName/description/minimumScore/ enabled. 3개 임시 엔딩. [필드·단위·삭제·검사 계약](../DataSpecs/GameDevEndings.md) |
| system.game-database | 기존 `Assets/Settings/Data/GameDatabase.asset`, GameJam.Data.GameDatabase, GUID 62a272592b807b3469b072879c349759, 주소 data/catalog, 그룹 Game Data Local, 라벨 없음. ExampleItems + GameDevEndings를 포함하는 생성 컨테이너 |
| editor.data-tool | 기존 `ProjectSettings/GameDataTool.asset`, 공용 DataImporter 공유 읽기 수정. 설정값/스키마 변경 없음 |
| data.example-items | 기존 정본/행 유지. 실제 아이템 규칙으로 소비하지 않음. 이번 엔딩 작업은 예제 내용/인코딩을 수정하지 않았음 |

- 생성 접근자: `Assets/Scripts/Data/Generated/DataTables.cs`의 GameDevEndings. 별도 CSV 파서/로더/캐시 저장소/저장 키 없음.
- 실제 경로: 공용 임포터 → 기존 DB → GameData.InitializeAsync/GetTable → GameDevEndingCatalog → GameDevManager → DevGageUI / EndingReached.
- 수명: GameData는 애플리케이션 단위; Manager는 씬 단위 런타임 진행도/불변 제출 스냅샷/결과. 공유 SO 원본은 수정하지 않음.
- 최종 sourceHash `4f36a4a86f99ee7fb76df8582f5d655952022fea8279b9b1fed466710652e093`.
- 초기 임시안: `ending_unfinished`/남겨진 가능성/0, `ending_released`/첫 출시/100, `ending_breakthrough`/새로운 시작/200. 위 숫자는 이력 스냅샷이며 편집 정본은 CSV다. 분야 합계가 점수라는 의미가 아니다.
- 다른 CSV Excel 호환 작업이 추가한 BOM 규칙을 유지. [해당 작업 이력](20261118-csv-excel-02-applied.md). 기존 ExampleItems는 Excel에 열린 상태에서 공용 임포터로 읽었고 강제 종료/저장하지 않았다.

## 구현 및 실제 검증

- GameDevManager: 3분야 0~100%, 독립 API, 완료/조기 종료, 점수 대기, 외부 총점 제출, CSV 분기, 한 번만 엔딩 통지, 초기화.
- GameDevScoreCalculator는 별도 파일이며 요청대로 공식 미구현(false 반환). 임의 0점/합산으로 엔딩 확정하지 않음.
- DevGageUI 프리팹과 ShelterPrototype 씬에 CLI/Editor API로 저장. 기존 이미지·한글 폰트·16:9 뷰포트 재사용. 설치 재실행 시 중복 없음.
- 파밍 결과 버튼 onClick.Invoke로 실제 ShelterPrototype 전환 확인. 재진입 Manager/UI 각 1개 및 진행도 0%, 이전 점수 없음. 기존 R 파밍 재시작 경로 유지.
- Unity 6000.3.28f1 컴파일 성공. CLI `unity recompile` 별칭은 현재 패키지와 불일치(400)하여 발견된 `script recompile` / `script recompile_status`로 검증.
- [Edit 검사 29개](../GameDev/Verification-edit.txt): 범위/비정상 숫자/불변 스냅샷/기본 점수 미정, 엔딩 순서·경계·상한·비활성·중복·필수 0점, 실제 CSV/생성 DB hash·기존 예제 보존, 값 변경 반영(메모리 fixture), 프리팹 참조.
- 공용 DataToolTests NUnit **33개 Passed**. 실제 `test run --mode EditMode --filter GameJam.Data.Tests.DataToolTests` 실행. 신규 Excel 공유 읽기 및 기존 미리보기 변경 차단·BOM/한글/타입 검사가 포함됨.
- [Play 검사 16개](../GameDev/Verification-play.txt): 실제 로드·제출 전 점수 거부·독립 진행도·99.999% 표시·제출 고정·중복 종료/점수 거부·엔딩 이벤트·재시작·3분야 완료·교체 계산기·UI 재활성.
- Addressables 프로필 Default 콘텐츠 빌드 성공. ActivePlayModeDataBuilderIndex=1 (`BuildScriptPackedPlayMode`)로 위 Play 실행: AssetDatabase 모드만의 검증이 아님. [씬/참조/입력 감사](../GameDev/Verification-scene.json).
- 실제 1920×1080 / 1440×1080 / 2560×1080 Game View 캡처·시각 검사. 0/부분/100%·결과 문구 확인. 패널 Raycast 차단/외부 통과 확인.
- 공유 gauge PNG 실제 바이트 교체·재임포트·원복 성공, GUID/local ID와 3개 Fill 참조 유지.
- 완료 직전 레지스트리/Assets/ProjectSettings/Docs 재검색: 새로운 엔딩 정본/동일 의미 예약 없음. 이전 파밍/오디오 데이터의 복제·이전 없음.

## 남은 기획 및 검증 범위

- 점수 공식, 분야 진행도 증가 규칙, 파밍 획득물의 개발 능력 환산, 엔딩별 전용 연출은 미정이다. API·이벤트 연결부와 임시 CSV를 제공한다.
- 실제 Player 실행 파일 빌드/배포, 저장/로드, 의도적인 Addressables 로드 실패 후 복구는 수행하지 않았다. 실패 상태/재시도 코드는 구현되어 있다.
