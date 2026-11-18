# game-dev-20261118 / explicit completion contract

- 작성: 2026-11-18 14:52 Asia/Seoul. 선행 [03 verified](20261118-game-dev-03-verified.md).
- 사용자 확정: 엔딩은 임시 지정, 진행도 증가와 개발 종료 조건은 후속 작업. 현재는 시스템과 연결부만 제작.
- 변경: GameDevManager.SetProgress에서 세 분야 100% 시 자동 종료하던 호출 제거. 게이지는 값만 반영하며, 향후 규칙이 명시적으로 FinishDevelopment를 호출해야 제출 스냅샷·점수 대기로 전환한다. 점수 공식은 계속 별도 GameDevScoreCalculator.cs의 미구현 계약이다.
- 데이터 ID: `data.game-dev.endings`; 정본 `Assets/Settings/Data/Csv/GameDevEndings.csv`의 기존 3개 임시 엔딩·0/100/200점 유지. [현재 명세](../DataSpecs/GameDevEndings.md).
- 기존 생성 컨테이너 `system.game-database`: GameDatabase.asset / GUID 62a272592b807b3469b072879c349759 / data/catalog / Game Data Local. 정본·DB·스키마·접근자·주소·값·로더 변경 없음. Addressables 재빌드 불필요.
- 중복 검사: 최신 목록 및 기존 GameDev 소비자 조사. 증가 규칙/종료 조건 CSV·SO를 추가하지 않았다.
- 검증 계획: 기존 Play 검사를 수정하여 3분야 100%에서 Developing 유지, 명시적 종료 후 AwaitingScore/외부 총점 엔딩으로 전환하는지 검사. 결과는 별도 검증 이력에 추가한다.
