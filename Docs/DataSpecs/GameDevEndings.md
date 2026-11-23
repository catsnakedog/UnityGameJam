# 게임 개발 엔딩 명세

## 현재 960틱 회차의 최종 평가

세 분야의 진척 합을 480으로 나눠 시트의 최소 점수와 비교한다. 합계 144부터 첫 출시(0.3), 240부터 새로운 시작(0.5)이며 300에서 최고 0.625점이다. 분모는 [벙커 규칙](BunkerRules.md)의 목표 진척 96과 기준 192틱에서 계산한다. [검증](../DataHistory/20261123-ending-score-verified.md)

## 2026-11-18 당시 120틱 회차의 평가값·팝업 변경

사용자 확정: 평가값은 진척 합 / (96 × 현재 벙커 틱 / 24), ×100 없음. 평가 파라미터 정본은 [BunkerRules](BunkerRules.md). `minimumScore` 단위는 비율 평가값으로 변경하며 기존 시트의 0/100/200을 0/0.3/0.5로 조정한다(임시 밸런스 위임). 아래 과거 점수 공식 미구현/0·100·200 설명은 이 계약으로 대체한다. 120틱 종료 시 진척 합 144/240이 두 상위 엔딩 경계이고 최대 점수는 0.625다. ID/제목/본문/enabled/스키마는 유지한다. UI 소비자는 기존 DevGageUI와 새 GameDevEndingPopup, 기존 Manager의 Ended 상태/선택 결과를 읽는다. 신규 데이터/독립 로더/내용 복제 없음. [계획](../DataHistory/20261118-ending-popup-01-planned.md), [UI 명세](../UI/GameDevEndingPopup.md).

## 현재 편집 원본 — Google Sheets (2026-11-18)

- 데이터 ID: `data.game-dev.endings`. 편집 정본: [GameDevEndings](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846).
- 로컬 스냅샷: `Assets/Settings/Data/Csv/GameDevEndings.csv` (UTF-8 BOM). 시트 → CSV → 기존 생성 DB/소비자 방향으로만 갱신한다. 로컬 CSV와 xlsx를 병행 편집하지 않는다.
- 양식: 1행 설명, 2행 타입, 3행 필드명, 4행부터 데이터. 기존 ID·필드 의미·타입·검증 제약은 유지한다.
- 갱신: Tools > Data Manager에서 전체 시트 가져오기 · CSV / 클래스 갱신을 누른다. 고급 변경은 미리보기 후 적용한다. 연결 목록은 SpreadConfig의 sheetName/id/gid/enabled에서 관리하며 Unity에는 Config URL만 저장한다. [연결 명세](DataToolSettings.md).
- 삭제/이전: URL/탭 변경은 이 명세와 연결 설정을 함께 갱신하고 참조 검증 후 이전한다. 시트 연결 삭제만으로 CSV/DB를 지우지 않는다. 업로드용 xlsx는 편집 정본이 아니다.
- 검증: 실제 공개 CSV 내보내기로 3행의 모든 셀/헤더를 대조했다. 게임 런타임은 원격 시트를 직접 읽지 않는다. 아래의 외부 원본 없음/CSV 정본/게시 대기 설명은 이 연결 계약으로 대체한다.

벙커 연결 갱신 (2026-11-18): 아래 진행도 증가·종료 조건의 미정 기술은 이번 연결로 대체한다. [BunkerRules](BunkerRules.md)가 능력 비례 진행과 총 N틱 종료를 제공한다. BunkerSimulation이 마지막 작업 후 FinishDevelopment를 호출한다. 엔딩 CSV/점수 공식 변경 없음; 점수 계산 미정으로 AwaitingScore 유지. [검증 이력](../DataHistory/20261118-bunker-ticks-02-verified.md).

- CSV 저장 인코딩: UTF-8 BOM 포함. Excel 기본 열기를 위한 규칙이며 엔딩 행/점수/ID는 변경하지 않는다.

## 식별과 담당 범위

| 항목 | 명세 |
|---|---|
| 데이터 ID / 상태 / 담당 | `data.game-dev.endings` / active (밸런스 임시) / Codex ending-popup-20261118 |
| 스키마 / 확인일 | v1 / 2026-11-18 |
| 소유 의미 | 벙커 이후 게임 개발의 최종 총점에 대응하는 엔딩 ID·표시 문구·최소 필요 점수 |
| 검색 별칭 | GameDevManager, GameDevEndings, Ending, score, minimumScore, 엔딩, 필요 점수 |
| 제외 범위 | 분야 진행도(런타임 상태), 평가식 파라미터(data.bunker.rules), 개발 속도·작업 배정·파밍 데이터·저장 키 |

## 정본과 연결

| 항목 | 명세 |
|---|---|
| 편집 정본 | 상단 GameDevEndings Google Sheets |
| 로컬 스냅샷 | `Assets/Settings/Data/Csv/GameDevEndings.csv` (UTF-8 BOM) |
| 생성물 | 기존 `Assets/Settings/Data/GameDatabase.asset`, `Assets/Scripts/Data/Generated/DataTables.cs` |
| GUID / 주소 / 그룹 | 컨테이너 GUID `62a272592b807b3469b072879c349759`, `data/catalog`, `Game Data Local`, 라벨 없음. 테이블 자체는 별도 SO/주소 없음 |
| 갱신 | 공용 Data Manager 검증/미리보기 → 적용 → 콘텐츠 빌드. `Tools > Game Dev > Import Ending CSV`는 동일 임포터 호출 전에 엔딩 제약도 검사 |
| 조회 / 소비자 | `GameData.GetTable("GameDevEndings")` → `GameDevEndingCatalog.FromTable` → `Select(totalScore)` → `GameDevManager` → `DevGageUI` / `GameDevEndingPopup` |
| 수명 | 기존 GameData 애플리케이션 캐시/핸들 재사용. Manager가 초기화 후 읽기 전용 엔딩 스냅샷 보관, 씬 종료 시 참조 소멸. 로드 실패 시 개발 시작 차단·재시도 가능. 별도 로더/폴백 없음 |

## 필드 계약

| 필드 | 타입 | 의미·단위 | 필수/빈 값/기본값 | 제약·참조 | 검사 |
|---|---|---|---|---|---|
| id | string | 안정적 엔딩 ID / 무차원 | 필수, 공백 금지, 기본값 없음 | 테이블 내 대소문자 구분 고유, 앞뒤 공백 금지 | 공용 CSV/DB + catalog |
| displayName | string | 엔딩 제목 / 텍스트 | 필수, 공백만 불가, 기본값 없음 | 참조 없음 | catalog |
| description | string | 엔딩 본문 / 텍스트 | 필수, 공백만 불가, 기본값 없음 | 참조 없음 | catalog |
| minimumScore | double | 최소 평가값 / 무차원 비율 (×100 없음) | 필수, 기본값 없음 | 유한한 0 이상. 활성 행끼리 고유, 활성 0점 행 필수 | 공용 타입 검사 + catalog |
| enabled | bool | 분기 사용 여부 / 무차원 | 필수, 기본값 없음 | 활성 행 최소 1개 | 공용 타입 검사 + catalog |

## 레코드·분기·삭제 정책

- 현재 `ending_unfinished`, `ending_released`, `ending_breakthrough`의 시트 최소 평가값은 각각 0/0.3/0.5다. 사용자 위임에 따른 **임시 밸런스**이며 제목·본문은 기존 값을 유지했다. 편집 정본은 Google Sheets이며 로컬 CSV/DB와 문서는 스냅샷이다.
- 입력 총점은 유한한 0 이상. `minimumScore <= totalScore` 중 최소 필요 점수가 가장 높은 활성 엔딩을 선택한다. 경계값 포함, CSV 행 순서 무관, 상한 없음.
- 0점 활성 행이 있으므로 모든 유효 점수에 정확히 하나가 선택된다. 같은 필요 점수의 활성 엔딩은 모호하므로 오류 처리한다.
- 제목 변경 시 ID 유지. 퇴역은 우선 enabled=false. 0점 행 비활성화/삭제 시 대체 활성 0점 행 필요. 삭제·타입 변경은 공용 임포터 보호 규칙 적용. 현재 저장 데이터/외래키 없음; 향후 저장 도입 시 ID 이전 정책 추가.
- 런타임에 CSV를 직접 읽지 않는다. 생성 SO/접근자를 별도 정본으로 편집하지 않는다.

## 중복 조사

| 비교 대상 | 검색 범위·근거 | 판단 |
|---|---|---|
| data.example-items | Assets/Scripts, Assets/Settings, ProjectSettings, Docs에서 GameDev/Ending/엔딩/score/점수/progress 검색 | 도구 예제의 가격·무게와 개발 총점/엔딩은 다른 의미. 재사용하지 않음 |
| data.farming.pickups / rules | FarmingSession, FarmingBunker, FarmingRules, FarmingData 및 각 명세 | 파밍 운반/시간 데이터이며 개발 엔딩 계약 없음. 복제·이전하지 않음 |
| system.game-database | GameDatabase, GameData, DataImporter, 그룹 등록 확인 | 저장·로딩 기반을 그대로 재사용하고 테이블만 추가 |
| planned 예약 | 생성 전 목록 확인 | 기존 엔딩 예약 없음; 이 작업이 단일 예약 |

## 변경과 검증

- 근거: 사용자 요청(3분야 진행도, 분리된 점수 계산 C#, CSV 엔딩 분기, DevGageUI).
- 계획 이력: [20261118-game-dev-01-planned](../DataHistory/20261118-game-dev-01-planned.md).
- 완료 이력: [20261118-game-dev-03-verified](../DataHistory/20261118-game-dev-03-verified.md).
- 후속 범위 확정: 엔딩 CSV 임시안 유지. 진행도 증가 및 개발 종료 조건은 후속 작업으로 둔다. 100% 자동 종료를 제거하고 외부에서 명시적으로 호출하는 `FinishDevelopment()` 연결부만 유지한다. CSV 값·스키마·GUID·주소·임포터 변경 없음. [종료 계약 이력](../DataHistory/20261118-game-dev-04-finish-contract.md).
- 최신 소비자 검증: 컴파일 및 Play 17개 통과. 100% 상태에서 자동 종료 없음과 명시적 종료 후 점수/엔딩 처리 확인. [검증 이력](../DataHistory/20261118-game-dev-05-finish-verified.md).
- 실제 검증: 컴파일, 29개 Edit 검사, 16개 Play 검사, 공용 CSV NUnit 33개, Addressables Default 콘텐츠 빌드 및 BuildScriptPackedPlayMode에서 런타임 로드, 경계값·외부 총점·중복 종료·재시작·씬 전환·UI 표시 통과.
- 적용 직전/완료 시 목록 재확인: 새 엔딩 계약/예약 충돌 없음. 공용 DB·GameData 재사용, Farming/Audio 정본 복제 없음.
- 최신 변경: 평가식·현재 벙커 틱 연결·중앙 팝업 구현 완료. [팝업 검증 기록](../DataHistory/20261118-ending-popup-02-verified.md). Player 실행 파일 빌드, 저장/로드 및 장기간 플레이 난이도 검증은 수행하지 않았다.
세 분기의 전용 Ending 씬은 기존 판정 ID와 점수를 읽고 공용 결과창으로 전환한다. [엔딩 연출과 결과창](../UI/GameDevEndingPopup.md)
