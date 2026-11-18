# 2026-11-18 벙커 맵 재배치 및 검증

사용자가 제공한 연결도와 사다리 지정에 맞춰 기존 `Assets/Scenes/ShelterPrototype.unity`를 편집했다. 상층 전투 장소, 중층 기획·거실·수면, 하층 아트·프로그래밍의 6개 방과 사다리 2개다. 하층 사다리는 기획–거실 통로에서 분기한다.

## 데이터 정본 및 중복 검사

- 사용 ID `data.characters`: 정본 `Assets/Settings/Data/Csv/Characters.csv`, [명세](../DataSpecs/Characters.md). CSV 값·ID·생성 DB·로더 변경 없음. 기존 ShelterRoster.spawnPoints 6개의 Transform을 거실 바닥으로 이동했다.
- 사용 ID `data.bunker.rules`: 정본 `Assets/Settings/Shelter/BunkerRules.asset`, [명세](../DataSpecs/BunkerRules.md). 틱/능력 환산 수치 변경 없음. 방 분류 정본은 기존 씬 ShelterArea.roomType이며 Programming/Graphics/Planning/Common/Sleeping/ZombieInvasion을 재사용했다.
- 적용 시 최신 DataRegistry와 Assets/ProjectSettings/Docs의 기존 명세·방 이름·분류·스폰 소비자를 확인했다. 새 CSV/SO/enum/설정 저장소는 만들지 않았다. 다른 작업의 `data.events` planned 예약은 이 배치와 의미가 달라 유지했다.
- CharacterSetup은 기존 스폰 참조를 재사용할 때 ENTRY 이름을 조회하지 않도록 수정했다. 스폰을 최초 생성할 때만 ENTRY 또는 유일한 Common 방을 선택한다.
- CharacterLinkChecks의 이동 목적지는 이전 LOOKOUT 표시 이름 대신 기존 ZombieInvasion 분류로 조회한다.

## 실제 검증

Unity 6000.3.28f1의 연결된 Editor에서 씬 편집·저장 및 Play 검증을 수행했다.

- 활성 방 6개·통로 3개·사다리 2개.
- 6×6 총 36개 방향별 경로 조회 성공(자기 방 포함).
- 사다리 양 끝 4개가 바닥에 연결되고, 아래 사다리 위 끝이 기획–거실 통로에 위치함을 확인.
- 아트→전투 경로가 두 사다리를 모두 사용함을 확인. 아래 사다리를 닫으면 우회 경로가 없음을 확인하고 즉시 원래 상태로 복원.
- 기존 스폰 지점 6개가 거실 바닥에 위치함을 확인.
- 기존 CharacterLinkChecks.RunPlay 통과: 구조 인원 2/0/6명, CSV 능력 연결, 중복 생성 없음, 씬 재진입 시 ID 유지, 전투 장소 경로 수락, 벙커 직접 실행 시 임의 캐릭터 없음.
- Play 중 임시 Survivor 프리팹을 아트실에 배치하고 실제 Update 이동을 관찰했다. 두 사다리를 거쳐 전투 장소에서 ARRIVED 확인. 임시 캐릭터는 Play 종료로 제거되며 씬에 저장하지 않았다.
- 1600×900 카메라 캡처에서 6개 방·한글 이름·사다리 및 HUD 겹침 없음을 확인.
- 검증 종료 시 컴파일 실패 없음, 현재 Console 오류/경고 0. 과거 Pipeline 로그는 보존했고 확인한 cursor 이후 새 오류 없음.
- Player 빌드는 수행하지 않았다. 새 수면/전투 게임 규칙은 구현 범위 밖이다.

[맵 배치 문서](../ShelterMapLayout.md) · [Play 이동 확인 이미지](../Images/ShelterMapLayout.png)
