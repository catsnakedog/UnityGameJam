# 엔딩 팝업·평가식 계획

- ID: `20261118-ending-popup-01-planned`, 담당 Codex ending-popup-20261118, 2026-11-18 KST.
- 사용자 요청: GameDevEndings 기반 엔딩 팝업. 평가값 = 현재 진척 합 / (96 × 현재 벙커 틱 / 24). ×100 하지 않으며 엔딩 시트 기준을 조정하도록 위임.
- 기존 `data.game-dev.endings` 재사용. 편집 정본은 기존 Google Sheets, 로컬 `Assets/Settings/Data/Csv/GameDevEndings.csv`는 스냅샷. ID/본문/활성 여부 유지, minimumScore 0/100/200 → 0/0.3/0.5 계획.
- 근거: 현재 120틱 종료에서 분모 480, 최대 진척 합 300이므로 최대 평가값 0.625. 첫 출시 144 이상(0.3), 새로운 시작 240 이상(0.5). 임시 밸런스이며 작업 속도/게임 길이 변경 없음.
- 기존 `data.bunker.rules` 정본 `Assets/Settings/Shelter/BunkerRules.asset` 확장: evaluationTargetProgress(double)=96, evaluationReferenceTicks(int)=24. 기존 주소 data/bunker-rules와 GUID 유지. 기존 GameData 설정 로드 재사용.
- 기존 데이터 검색: DataRegistry planned 포함, Assets/Scripts·Assets/Settings·ProjectSettings·Docs/DataSpecs의 GameDevEndings/minimumScore/엔딩/96/evaluation/referenceTicks 조사. 평가 상수의 기존 정본 없음. 벙커 작업/시간 규칙에 두 필드만 확장하며 새 SO/CSV/로더 없음.
- 평가식 구현: GameDevScoreCalculator, BunkerSimulation이 현재 틱을 제공. 틱 0은 계산 보류, 유효 점수 확정 후 GameDevManager의 기존 엔딩 선택 사용.
- UI 소비자: 저장된 GameDevEndingPopup 프리팹과 ShelterPrototype 씬 참조. 제목/본문은 선택된 엔딩에서 읽으며 중복 내용 저장 없음. 확인 시 닫기, 배경 입력 차단, 재시작 시 초기화.
- 계획 단계 검증 미실행. 이후 기록에 시트/CSV/DB 일치, 컴파일, 평가 경계, 실제 종료·팝업 및 화면 캡처를 추가한다.
