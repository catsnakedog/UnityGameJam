# 좀비 침입 시스템

`ShelterPrototype.unity`에 연결되어 있다. 기존 벙커 틱의 습격 시점마다 좀비 1마리를 생성한다. 현재 침입 간격은 기존 BunkerRules의 30틱이며 전체 게임은 기존 120틱을 유지한다.

## 플레이

1. Farming에서 사람을 구조한 뒤 벙커로 진입한다. 벙커 씬 직접 실행 시 구조 명단이 없으므로 사람이 자동으로 추가되지는 않는다.
2. 캐릭터를 클릭하고 위쪽 `전투 장소`를 클릭해 보초를 배치한다.
3. 좀비가 전투 방 오른쪽에 생성된다. 5틱마다 1명이 주사위 1~6 + 전투력으로 판정한다. 승리하면 좀비 HP −1, 패배하면 그 캐릭터 멘탈 −2, 동점은 횟수 없이 재굴림한다.
4. 방어 시작 인원 × 3회까지 판정한다. 좀비가 죽으면 종료하고 현재 전투 공간 인원 멘탈 +5. 마지막 허용 판정에서 죽여도 성공이다. 전투 공간 전원이 멘탈 0이 되거나 판정 한도 뒤 살아 있으면 침입한다. 한 명만 탈진하면 나머지가 방어한다.
5. 침입 좀비는 남은 HP를 유지하고 연결된 방을 골라 통로·사다리로 이동한다. 경로에서 캐릭터를 만나면 잠시 멈추고 강제 전투 1회 후 살아 있으면 이동을 계속한다.
6. 목표 방 점거 시 빨간 이름/바닥 표시와 `기능 정지`를 표시한다. 캐릭터를 그 방으로 보내면 자동 전투한다. 마지막 좀비를 처치하면 방 작업이 재개된다. 처치한 좀비와 전투한 참가자는 멘탈 +5.

현재 멘탈/탈진 남은 틱은 캐릭터 이름 아래, HP/방어 횟수/침입 상태는 좀비 위에 표시한다. 멘탈 0이면 행동 정지, 40틱 후 30으로 회복하며 상한은 100이다. 전투 중인 캐릭터만 이동 잠금, 전투가 끝나면 이전 이동 명령을 이어간다. 이벤트 팝업이 열린 동안 전투·이동·기절 타이머가 함께 멈춘다.

## 배치와 조정

- 기존 `ShelterArea` 방·통로, `ShelterLadder` 사다리를 배치한다. 가장 위쪽의 활성 `ZombieInvasion` 타입 방이 생성/방어 공간이다. 방 종류/좌표를 직접 바꿔도 기존 자동 경로를 사용한다.
- 점거는 `accessible`을 끄지 않는다. 다른 방 기능을 추가할 때 `ShelterArea.IsOperational`을 확인한다. 기존 작업은 `ShelterWalker.CanWorkInCurrentRoom`으로 연결되어 있다.
- `ZombieData.asset` Inspector에서 좀비 4종과 전투/이동 값을 수정한 뒤 `Tools > Zombies > Build Data Content` 실행. 방 배치나 프리팹에 HP/전투 수치를 복제하지 않는다.
- 개발 매니저의 `ZombieInvasionController` Inspector에 Play용 `좀비 1마리 생성` 버튼이 있다. 정식 습격은 BunkerSimulation 이벤트로 발생한다.
- 설치 재연결은 `Tools > Zombies > Install Invasion`. 기존 방 배치와 SO 값은 보존한다.

## 데이터 정본·명세

| ID | 정본 / 이번 변경 | 명세 |
|---|---|---|
| data.zombies | Assets/Settings/Shelter/ZombieData.asset 신규. data/zombies | [Zombies](../DataSpecs/Zombies.md) |
| data.bunker.rules | Assets/Settings/Shelter/BunkerRules.asset, 멘탈 상한/기절/회복 추가. 기존 주기 보존 | [BunkerRules](../DataSpecs/BunkerRules.md) |
| data.characters | 기존 Characters 시트 → Assets/Settings/Data/Csv/Characters.csv → 공용 DB. 전투력/멘탈 소비 재사용, 값 변경 없음 | [Characters](../DataSpecs/Characters.md) |
| data.events | 기존 Events 시트 → Assets/Settings/Data/Csv/Events.csv → 공용 DB. NEXT_WAVE_HP 실제 생성 연결, 값 변경 없음 | [Events](../DataSpecs/Events.md) |

Assets/ProjectSettings/Docs와 DataRegistry planned까지 중복 검사 완료. 새 캐릭터 DB/이벤트 DB/CSV 로더 없이 기존 GameData/Addressables·자동 경로를 재사용했다.

[검증 이력](../DataHistory/20261118-zombie-invasion-03-verified.md), [Edit 검사](Verification-edit.txt), [Play 검사](Verification-play.txt). 임시 외형/이동 속도이며 연출·애니메이션·장시간 밸런스는 별도다. 방어/경로/점거가 이번 구현 범위이며 노션의 시간당 침입 멘탈 감소와 수면/보초의 지속 회복·소모는 추가하지 않았다.
