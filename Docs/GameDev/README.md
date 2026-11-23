# 벙커 이후 게임 개발

파밍 종료 화면의 **벙커로 이동**을 누르면 `ShelterPrototype`으로 이동한다. 파밍 재시작은 기존 R 키로 가능하다. 벙커 씬의 `GameDevManager`가 현재 개발 상태를 소유하며 우측 `DevGageUI`가 기획·프로그래밍·그래픽을 각각 표시한다.

## 진행도와 종료

```csharp
using UnityGameJam.GameDev;

await manager.InitializeAsync();
manager.AddProgress(GameDevField.Planning, earnedPercentagePoints);
manager.SetProgress(GameDevField.Programming, currentProgrammingPercent);
manager.AddProgress(GameDevField.Graphics, earnedGraphicsPercentagePoints);
```

입력은 퍼센트포인트(0~100), 범위 밖은 clamp하며 NaN/Infinity·잘못된 분야는 거부한다. **진행도 증가와 총 틱 종료는 [벙커 진행](../BunkerProgress.md)에 연결되어 있다.** 세 분야가 모두 100%여도 자동 종료하지 않는다. `Progress.IsComplete`는 게이지가 모두 채워졌는지만 알려 준다. 향후 종료 조건을 담당하는 기능에서 `FinishDevelopment()`를 명시적으로 호출하면 제출 스냅샷이 고정되고 추가 진행도 변경은 false를 반환한다.

개발 속도·캐릭터 능력·작업 배정은 BunkerSimulation에 연결되어 있다. Editor Play에서 GameDevManager를 선택하면 Inspector의 **Play Mode 검증 도구**로 각 게이지를 시험할 수 있다. 이 값은 SO/CSV/씬의 밸런스 데이터로 저장되지 않는다.

## 점수와 엔딩

`GameDevScoreCalculator.cs`가 평가 = **진척 합 ÷ (목표 진척 합 × 현재 벙커 틱 ÷ 기준 틱)** 을 계산한다. 목표 진척 합 96, 기준 틱 192는 기존 `BunkerRules.asset`에서 편집한다. ×100하지 않는다. BunkerSimulation이 현재 틱을 공급하며 마지막 틱의 작업 이후 자동으로 평가·엔딩을 확정한다. 틱 0 또는 시계 미연결 상태만 `AwaitingScore`로 보류한다.

- 기존 클래스에 공식을 구현하거나 `IGameDevScoreCalculator` 구현을 `SetScoreCalculator`로 전달한다. 대기 중 교체하면 `TryEvaluateScore()`로 다시 계산한다.
- 외부 점수 시스템이라면 개발 종료 후 `SubmitFinalScore(totalScore)`를 호출한다. 총점은 0 이상의 유한한 double이다.
- 성공 시 `TotalScore`, `Ending`, `Phase=Ended`가 함께 확정되고 `EndingReached(ending, totalScore)`가 한 번 발생한다. 엔딩별 연출은 이 이벤트의 `ending.Id`를 사용해 연결한다.
- `RestartDevelopment()`는 진행도·제출 스냅샷·총점·엔딩을 초기화한다. 애플리케이션 공용 GameData 캐시는 유지한다. 세이브/로드는 이번 범위에 포함하지 않는다.

## 데이터 편집

편집 정본은 [GameDevEndings 시트](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846), 데이터 ID는 `data.game-dev.endings`다. [GameDevEndings.csv](../../Assets/Settings/Data/Csv/GameDevEndings.csv)는 내려받은 스냅샷이다. 제목·본문·최소 평가값·활성 여부를 시트에서 편집한다. 현재 임시 기준은 0 / 0.3 / 0.5이며 960틱 종료 시 진척 합 144/240에서 두 상위 엔딩에 도달한다. 목표/틱 설정이나 게임 길이를 바꾸면 이 기준도 검토한다.

`Tools > Game Dev > Import Ending CSV`는 공용 DataImporter에 엔딩 검사를 더해 기존 DB를 갱신한다. 공용 Data Manager의 미리보기/적용도 가능하며, 적용 후 `Tools > Game Dev > Validate Ending Rules and UI`로 의미 제약을 확인한다. 배포용 데이터는 공용 Data Manager의 **Addressables 콘텐츠 빌드**로 갱신한다. 생성 DB나 접근자 소스를 직접 편집하지 않는다.

분기는 `minimumScore <= totalScore`를 만족하는 활성 행 중 가장 높은 최소 점수를 선택한다. 행 정렬은 결과에 영향이 없다. 활성 0점 행은 필수이고 활성 행끼리 같은 최소 점수는 오류다. 상세 계약은 [데이터 명세](../DataSpecs/GameDevEndings.md), 변경 이력은 [DataHistory](../DataHistory)를 참조한다.

## UI 편집과 검증

- 엔딩은 `Assets/Prefabs/UI/GameDevEndingPopup.prefab`의 중앙 노트북 팝업으로 표시한다. 확인/Enter/Space/Escape로 닫으며 HUD 결과는 남는다. [팝업 명세](../UI/GameDevEndingPopup.md).
- 설치 `Tools > Game Dev > Install Ending Popup`, 검증 `GameDevEndingPopupChecks.RunEdit()` 및 Play에서 `await GameDevEndingPopupChecks.RunPlay()`.

- 프리팹: `Assets/Prefabs/UI/DevGageUI.prefab`. 씬 인스턴스의 manager 참조는 직렬화되어 있다.
- 씬 설치: `Tools > Game Dev > Install Shelter Development UI`. 기존 프리팹과 사용자 배치를 보존하고 중복 생성하지 않는다.
- [UI 명세](../UI/DevGageUI.md). 기존 한글 TMP 폰트·패널/게이지 Sprite·16:9 viewport를 재사용한다.
- 검사 도구: `GameDevChecks.RunEdit()`, Play 중 `await GameDevChecks.RunPlay()`. 결과는 이 폴더의 Verification 텍스트에 기록한다. Player 실행 파일 빌드와 구분한다.
