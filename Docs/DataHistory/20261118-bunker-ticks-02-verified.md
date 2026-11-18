# 벙커 틱·작업 적용/검증 — 2026-11-18 KST

작업 ID bunker-ticks-20261118. 이전 [계획](20261118-bunker-ticks-01-planned.md). 사용자 요청에 따라 벙커 시한과 작업 틱 구현, 랜덤/침공은 알림만 제공.

관리 SO 전체(이번 작업): `data.bunker.rules`, `UnityGameJam.Shelter.BunkerRules`, `Assets/Settings/Shelter/BunkerRules.asset`, GUID `01a9f70ed623c5341bec7f05cc07781d`, 주소 `data/bunker-rules`, 그룹 `Game Data Local`, 라벨 없음. 정본 SO, 생성 CSV 없음. 이전 값 없음 → totalTicks=120, secondsPerTick=1, randomEventEveryTicks=10, invasionEveryTicks=30, workEveryTicks=1, percentPerAbility=0.1. 모든 초기값은 임시 튜닝값. 전체 필드 계약은 [명세](../DataSpecs/BunkerRules.md).

공용 GameData.LoadSettingsAsync<T>가 동시 로드/캐시/해제를 담당하며 기존 CSV DB 로더는 유지. BunkerSimulation이 설정·캐릭터 로드 완료 후 틱 시작. 기존 CharacterIdentity.Definition에서 능력 참조 → GameDevManager.AddProgress → 기존 UI. 총 틱 종료 시 FinishDevelopment로 최종 스냅샷 제출. 점수 공식은 기존 미정 상태 유지하므로 AwaitingScore에서 외부 점수를 기다린다.

씬 정본 `Assets/Scenes/ShelterPrototype.unity`: GameDevManager에 BunkerSimulation 추가, 기존 roster 참조. 방의 ShelterArea.roomType 저장: 하단 왼쪽 프로그래밍, 하단 가운데 그래픽, 중단 왼쪽 기획. ENTRY 및 나머지 기본 공용. 수면/침공 타입 지정 가능하나 효과 없음. 기존 방 위치/이동 구조 유지.

재사용 데이터: `data.characters` 정본 `Assets/Settings/Data/Csv/Characters.csv`, `data.game-dev.endings` 정본 `Assets/Settings/Data/Csv/GameDevEndings.csv`. 값 변경 없음; 소비 계약 문서 갱신. 기존 DB/진행도/캐릭터 능력 복제 없음. Assets/ProjectSettings/Docs 검색 및 적용 전·완료 전 최신 목록 재확인: 충돌 없음.

검증 결과:
- Unity 컴파일 오류/경고 0 (최종 런타임 코드).
- [Edit 9개](../Bunker-edit.txt): 시간 누적, 정지, 순서, 종료, 잘못된 값, 임시 SO 템포 변경 및 원본 보존.
- [Play 21개](../Bunker-play.txt): 실제 설정/캐릭터 로딩, 캐시 공유, 이동 소속, 3분야 비례, 인원 합산, 비활성/닫힌 방 제외, 3개 비작업 타입, 재시작, 정지, 주기, 마지막 작업/종료.
- Addressables BuildPlayerContent 성공, Error=null, 출력 `Library/com.unity.addressables/aa/Windows/settings.json`, 빌드 시간 12.57초. 현재 프로필 사용. Play builder `Use Existing Build (requires built groups)`에서 검증.
- 실제 Update 1.5초 후 CurrentTick=1 확인.
- 테스트에서 발견한 구독자가 없는 시계의 증가 누락 수정 후 Edit/Play 재검증 통과.

미검증/범위 밖: Player 실행 파일, 영구 저장, 의도적인 Addressables 실패/복구, 랜덤 이벤트 내용, 좀비 전투, 수면/공용 효과. UI 게이지와 점수 계약은 기존 시스템 재사용. 개발 총점 계산은 이번 범위가 아니다.
