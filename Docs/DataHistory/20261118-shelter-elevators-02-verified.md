# 엘리베이터 교체 완료 / 2026-11-18

담당 Codex shelter-elevators-20261118. 사용자 요청: 모든 사다리를 투영식 엘리베이터로 교체하고, 먼저 호출한 인물이 하차까지 주도권을 유지하여 층간 호출 왕복 루프를 방지한다. 문 애니메이션 없음.

## 적용

- 기존 ShelterPrototype의 방 6개와 위치를 유지하고 사다리 2개를 비활성 보존. 같은 위치에 Elevator 프리팹 2개와 승강장 4개 설치, 기존 안내 문구 수정.
- ShelterNavigation은 배치에서 중간층을 포함한 정차층과 승하차 경로를 자동 계산. 현재 차량 층/소유자는 그래프 변경으로 취급하지 않음.
- ShelterElevator는 정원 1, 호출 순서 FIFO, Owner가 빈 차 호출·탑승·운행·하차까지 유지. 대기자 취소/비활성/전투/탈진 시 다음 호출로 인계. 탑승 중 정지/목적지 변경은 하차 후 적용.
- ShelterWalker는 차량이 운반하는 동안 서 있는 모습 유지. 사다리 애니메이션 소비 제거. ShelterTransit은 기존 GameData 캐시 및 시뮬레이션 일시정지/입력 차단과 연결.
- 좀비도 같은 차량을 순서대로 이용하며 독립 HP/상태 보존. 호출 지점을 지나쳐 뒤의 대기열로 역주행하지 않도록 진입 지점을 승강장과 연결. 탑승 중에는 지상 강제 전투/충돌에서 제외. 하차 후 전투/점거 유지. 전투 중 좀비도 자신의 차량 호출을 보류한다.

## 데이터

- 신규 ID `data.shelter.elevators`, 정본 `Assets/Settings/Shelter/ElevatorRules.asset`, GUID `6facc373492cc584ca04ebc3968d1007`, Addressables `data/elevators`, 기존 Game Data Local 그룹. [명세](../DataSpecs/ElevatorRules.md).
- 초기 임시값: travelSpeed 3 world units/sec, queueSpacing 2.4, boardingOffset 2.4, exitOffset 1.2 world units. 사용자 조정 정본은 SO 하나다. 별도 CSV/설정 저장 키/런타임 로더 없음.
- 재사용 `data.zombies`: 기존 Zombies/ZombieRules 원격 시트 → CSV → ZombieData.asset 경로/값/ID 유지. climbSpeed는 호환 열/속성/검증만 남기고 이동 소비 중단. [종류 명세](../DataSpecs/Zombies.md), [규칙 명세](../DataSpecs/ZombieRules.md).
- 재사용 `data.characters`와 `data.bunker.rules`: 기존 구조 명단·멘탈·일시정지 API, 값/정본 변경 없음.
- 최종 반영 직전 최신 DataRegistry planned와 Assets/ProjectSettings/Docs의 elevator/lift/엘리베이터/속도/대기열을 재검색. 차량 속도 정본 중복 없음, data/elevators 등록 1개. 좀비 개인 사다리 속도와 차량 속도는 대상/의미가 다름. 기존 다른 작업의 시트·카메라·파밍·엔딩 변경 보존.

## 검증

Unity 6000.3.28f1 Editor, 기존 프로젝트에서 수행.

- 컴파일 오류 0. 기존 Default 프로필 Addressables 콘텐츠 빌드 성공, Use Existing Build 로드 성공.
- [Edit 39개](../Elevators-checks-edit.txt): 클릭 명령, 3층 자동 인식, 반대층 FIFO, 반복 호출, 하차 후 목적지 변경의 실제 좌표, 중도 정지, 대기자 잠금/비활성, 탑승 중 잠금, 차량 제거·복구, 배치 변경, 30/60/144 FPS 걷기, 수치 검증.
- [실제 씬 Play 검사](../Elevators-checks-play.txt): 활성 사다리 0, 사용 가능한 차량 2, 방 6×6 연결, 두 차량 환승, 세 캐릭터 반대 방향 이동, 일시정지, 탑승 중 탈진, 좀비 6마리 FIFO/독립 HP/한 차량 정원 1/지상 최소 간격 2.4/점거/재시작.
- [좀비 전투 회귀 42개](../Zombies/Verification-play.txt): 방어 n×3 마지막 판정, 전원 멘탈 0, 접촉 전투, 방 점거/해방, 멘탈 회복, 재시작/엔딩, 연결 해제/복구 유지.
- [좀비 대기열 회귀 19개](../Zombies/Verification-queue-play.txt): 개별 HP/진입 순서, 잠긴 선두, 승강장 대기, 6마리 순차 하강, 점거 중 지상 간격, 처치 후 대기열 진행, 큰 프레임·일시정지.
- [실제 Update](../Elevators-checks-live.txt): 세 캐릭터를 반대 방향으로 명령. 운행 중 한 명이 보이는 캐빈과 두 명의 승강장 대기 화면 확인. 이후 15.8초 실제 프레임에서 각 목적지 도착, 전체 차량 Owner/Passenger/Queue 정리 확인. [화면](../../outputs/ShelterElevators/in-flight.png).

테스트 시간 제어를 사용한 Play 검사와 실제 Update 검증을 구분했다. 실행 파일은 빌드하지 않았으며 문 열고 닫기 애니메이션은 요청대로 없다. 기존 최소 간격은 지상 좀비에 적용하며 차량에는 정원 1 계약을 적용한다. 새 빈 씬에는 기존 시뮬레이션/명단/Transit 관리자가 필요하다. 안내는 [ShelterMovement](../ShelterMovement.md).
