# 엔딩 팝업·평가식 검증 완료

- 기록 ID: `20261118-ending-popup-02-verified`, 담당 Codex ending-popup-20261118, 2026-11-18 KST.
- 이전: [계획](20261118-ending-popup-01-planned.md). 사용자 확정: 현재 시각=현재 벙커 틱, ×100 금지, 시트 기준 밸런싱 위임.

## 데이터 정본과 변경

| 데이터 ID | 정본·생성물 | 변경 |
|---|---|---|
| data.game-dev.endings | 기존 [GameDevEndings 시트](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846), 로컬 Assets/Settings/Data/Csv/GameDevEndings.csv | D5=100→0.3, D6=200→0.5. 0점 행/ID/제목/본문/enabled 유지. 단위=비율 평가값 |
| data.bunker.rules | Assets/Settings/Shelter/BunkerRules.asset, GUID 01a9f70ed623c5341bec7f05cc07781d, 주소 data/bunker-rules, Game Data Local | evaluationTargetProgress(double)=96, evaluationReferenceTicks(int)=24 추가. 기존 게임 길이/속도/멘탈 규칙 유지 |
| system.game-database | Assets/Settings/Data/GameDatabase.asset, GUID 62a272592b807b3469b072879c349759, 주소 data/catalog, Game Data Local | 기존 SpreadsheetImporter로 엔딩 시트만 다운로드·검증·적용. 생성 DB는 편집 정본 아님. 접근자 스키마 변경 없음 |

평가식은 `진척 합 / (evaluationTargetProgress × 현재 틱 / evaluationReferenceTicks)`. BunkerSimulation이 이미 로드한 BunkerRules와 CurrentTick을 기존 GameDevScoreCalculator에 공급한다. 틱 0은 보류, 마지막 틱의 작업 이후 평가를 확정한다. 120틱 분모480, 최대300/480=0.625. 임시 엔딩 기준 0.3과0.5는 진척 합144와240에 해당한다.

GameData의 기존 애플리케이션 캐시/해제 소유권 유지. 신규 CSV/SO/주소/런타임 로더/저장 키 없음. 기존 목록과 Assets·ProjectSettings·Docs의 ID/별칭/필드 의미/소비자 중복 검사 후 재사용·확장했으며 완료 직전 목록도 재확인했다.

## 소비 연결·UI

`GameData.GetTable(GameDevKeys.EndingsTable)` → GameDevEndingCatalog → GameDevManager → 기존 DevGageUI 및 GameDevEndingPopup. 프리팹 `Assets/Prefabs/UI/GameDevEndingPopup.prefab`, GUID `2e6a2e71aff6e7f45af124b63e805b0d`. ShelterPrototype에 직렬화된 manager/simulation/commander 참조와 함께 1개 배치. CLI로 Editor API 작성·저장. 기존 노트북/버튼/한글 폰트 재사용.

Ended 상태에서 제목·본문·평가값 표시. 확인/Enter/Space/Escape로 닫고 HUD는 결과 유지. 표시 중 기존 pause owner/input block 사용. 닫기·비활성·재시작에 해제하며 같은 확인된 결과의 재표시 방지. 런타임 UI 생성/엔딩 데이터 복제 없음.

## 실제 검증

- Unity 6000.3.28f1 컴파일 성공. 팝업·평가식 Edit 13개, 기존 엔딩/CSV/HUD Edit 29개 통과. 설치 두 번 재실행해 팝업 1개/참조 유지 확인.
- 실제 Google Sheets CSV 내보내기로 0/0.3/0.5 저장 확인. 기존 가져오기 도구의 diff는 minimumScore 두 셀뿐. 로컬 CSV UTF-8 BOM과 생성 DB fingerprint 일치.
- Addressables Default 프로필 콘텐츠 빌드 성공: [빌드 기록](20261118-171801-1604364-data-built.md). BuildScriptPackedPlayMode에서 새 평가 파라미터와 엔딩 임계값 런타임 로드.
- 팝업 Play 28개: 세 실제 엔딩 경계, DB 제목/본문 표시, 점수 보류 중 숨김, 중복 종료, 확인 버튼, 배경 명령 차단, pause 해제, 재시작, 활성/비활성 복구, 최종120틱 자동 계산/표시 통과. 최종 틱 검증은 사건/좀비와 캐릭터 작업을 일시 비활성화한 통제된 시나리오이며 값은 런타임에만 주입하고 복원했다.
- 기존 Manager/HUD Play 17개 통과. 외부 점수/대기 계산기 계약을 테스트에서 명시적으로 사용하고 종료 시 현재 벙커 계산기를 복구하도록 검사 도구 갱신.
- 1920×1080 Game View 캡처 및 실제 1440×1080 Game View 크기에서 위·아래135px 레터박스/중앙 팝업 시각 검수. 임시 Game View 규격을 제거하고 원래 선택 복구.
- 실제 button_normal.png 픽셀 교체→재임포트 시 dependency hash 변화, 팝업 참조 및 GUID e8296d71b6e762540b24217784e1dfcb/local ID21300000 유지. 원본 PNG 바이트 복원 확인.
- 검증 구간 Console 신규 오류 0. 이전 실행의 SoundManager 오류 기록은 남아 있으나 이번 검증 구간에는 재현되지 않았으며 오디오 코드는 변경하지 않았다.
- Play 종료, 씬 저장 상태 확인. 빌드 중 뜬 Unsaved Scenes 대화상자는 기존 변경을 저장하여 진행했다.

검증 산출물: `outputs/ending-popup/edit-checks.txt`, `play-checks.txt`, `art-check.txt`, `ending-1920x1080.png`, `ending-1440x1080.png`. [UI 명세](../UI/GameDevEndingPopup.md), [엔딩 데이터 명세](../DataSpecs/GameDevEndings.md), [벙커 규칙 명세](../DataSpecs/BunkerRules.md).

제한: 초기 수치 밸런스이며 실제 플레이 난이도 장기 튜닝/전체 파밍→엔딩 플레이/Player 실행 파일 빌드/저장·로드는 이번 검증 범위가 아니다.
