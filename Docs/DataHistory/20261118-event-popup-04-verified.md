# 틱 기반 노트북 이벤트 구현·검증

2026-11-18 · Codex event-popup-design-20261118.

- `data.events`: planned → active (원고 임시). 정본 `Assets/Settings/Data/Csv/Events.csv`, UTF-8 BOM, id:int/title:string/content:string, 임시 사건 3건. [명세](../DataSpecs/Events.md).
- `data.bunker.rules`: `Assets/Settings/Shelter/BunkerRules.asset`의 기존 randomEventEveryTicks=10 유지. 마지막 틱을 제외한 주기 알림으로 사건 표시. 정본 수치 변경 없음. 소유자별 정지 API를 추가하여 팝업과 수동 정지가 충돌하지 않게 함.
- `system.game-database`: 기존 CSV를 보존하며 Events 추가. GUID/주소/그룹 유지. 기존 GameData 사용, 별도 데이터 로더/CSV 파서 없음.
- 소비 코드: Assets/Scripts/Events/EventCatalog.cs, EventPopupController.cs, EventLaptopPopup.cs. 씬 ShelterPrototype, 프리팹 Assets/Prefabs/UI/EventLaptopPopup.prefab (GUID 27ae7b09da16b094c9378f228832e349).
- 기존 목록을 생성 전·반영 전 재확인. 같은 의미의 데이터 없음. 이벤트 주기/캐릭터 능력/엔딩 데이터 복제 없음. 동시 작업의 맵 배치 문서 유지.

## 실제 검증

- Unity 6000.3.28f1 컴파일 오류/경고 0.
- 공용 CSV 미리보기/적용 및 Addressables 콘텐츠 빌드 성공. 첫 빌드는 Unsaved scenes로 실패했으며 씬이 clean 상태임을 재확인한 뒤 재시도 성공. 도구가 남긴 실패 이력도 보존.
- [Edit 검사](../Events/Verification-edit.txt): CSV BOM, 필수값, 추첨, 프리팹/Sprite 참조, 타 테이블 삭제 없음.
- [Play 18개 검사](../Events/Verification-play.txt): 빌드된 데이터 로드, 주기 전 미발생, 지정 틱 발생, CSV 본문 일치, 타이핑, 정지 중 출력, 두 단계 확인, 중복 입력, 외부 정지 보존, 다음 사건, 재시작, 없는 ID, 프레임 따라잡기, 비활성 해제, 마지막 틱 종료, 재활성.
- [실시간 검사](../Events/Verification-realtime.txt): 수동 Advance 없이 실제 Update 경과로 10틱에 발생.
- [에셋 검사](../Events/Verification-assets.txt): PNG 변경·복원 시 GUID/local ID/프리팹 Sprite 참조 유지. 설치 재실행 시 중복 컨트롤러 없음.
- 실제 Game View 1920×1080 및 1440×1080 4:3에서 캡처·시각 검수. 4:3은 Game View 해상도를 직접 변경해 letterbox 확인 후 원래 설정 복원.
- 검증 중 신규 Console 오류 없음(이전 작업의 Console 기록과 구분).

## 제한

Player 실행 파일은 빌드하지 않음. 의도적인 Addressables 실패 복구, 임의의 모든 긴 제목·결합 이모지, 실제 하드웨어 키보드/마우스 클릭 검사는 미실행. Play 입력 동작은 Confirm 경계 호출로 검사했다. 캐릭터 이동 애니메이션은 계속되며 벙커 틱과 새 명령만 정지한다. 능력 효과/선택지/저장/사운드는 추가하지 않았다.
