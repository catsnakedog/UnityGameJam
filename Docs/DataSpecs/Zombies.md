# 좀비 전투·침입 데이터 명세

실행 시 시트 연동은 기존 종류/규칙 쌍 또는 현재 SpreadConfig의 종류 별칭 ZombieData를 읽는다. 종류만 연결된 별칭 형식에서는 내부 공통 규칙을 사용한다. 실제 전투는 GameData.LoadSettingsAsync<ZombieData>로 이번 실행의 검증된 SO를 받는다. 공용에 이미 존재하는 ZombieData.csv 호환 테이블과 전투가 같은 다운로드를 사용한다. [선택 설정](DataToolSettings.md)

## 시트 계약 v2 (2026-11-18)

`data.zombies` active / Codex zombie-sheets-20261118. 편집 정본은 [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193)와 [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)이다. `Zombies` 필수 열은 `id:string`, `displayName:string`, `firstWave:int`, `combat:int`, `hp:int`이며 아래 entries 계약과 같다. ID 앞뒤 공백 금지/ordinal 고유, 공백 이름 금지, firstWave 양수·고유·1 시작 필수, combat 1~공통 diceSides, hp 양수다. 모든 열과 값은 필수이며 암묵 기본값은 없다.

`Assets/Settings/Shelter/Zombies.csv`와 `ZombieRules.csv`는 내려받은 UTF-8 BOM 스냅샷(설명/타입/필드명 3행)이다. 공통 규칙은 [ZombieRules 명세](ZombieRules.md)의 default 한 행에만 둔다. 공용 파서와 기존 검증기로 검증한 뒤 기존 SO/GUID/주소에 생성한다. ID 삭제는 명시적 이전이 필요하며 정렬/표시명 변경은 ID를 바꾸지 않는다. 원격 4종/공통 규칙 전체 셀과 SO를 대조했고 초기 수치는 바꾸지 않았다. [최종 검증](../DataHistory/20261118-zombie-sheets-03-remote-verified.md).

## 식별과 담당 범위

- ID `data.zombies`, v2, 2026-11-18, active / Codex zombie-sheets-20261118.
- 소유: 좀비 종류·웨이브별 선택, 기본 HP/전투력, 주사위 전투 규칙, 좀비 이동/접촉 수치. 별칭 Zombies, zombie_1~4, COMBAT_TICK, RAID_COMBAT_TICKS.
- 제외: 캐릭터 능력·초기 멘탈(data.characters), 전역 멘탈/기절·발생 주기(data.bunker.rules), 다음 웨이브 HP 보정(data.events의 런타임 효과), 방 배치(ShelterArea), 남은 HP·점거·전투 횟수(런타임).
- 좀비는 6틱 거리에서 접근하고 문 앞 방어가 끝나면 수비자에게 걸어간다. 수비자가 접촉 반경 안에 있으면 근접전을 시작하고, 전원이 탈진하면 내부 이동을 시작한다. [벙커 규칙](BunkerRules.md)

## 정본과 연결

- 편집 정본: [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193), [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486). 기존 `Assets/Settings/Shelter/ZombieData.asset` (ZombieData SO)은 생성물이며 Inspector 읽기 전용이다. 노션은 기획 근거이며 자동 양방향 동기화는 없다.
- Addressables: `data/zombies`, 기존 `Game Data Local`, 라벨 없음. GUID `6845f853ae90e0b479d0f51cb5d53023`.
- 시트 편집 → `Tools > Data Manager > 전체 시트 가져오기` → `ZombieCsvImporter` → 기존 SO. 두 시트는 함께 활성화한다. 모든 다운로드·검증 후 적용하며 실패/취소는 기존 파일 보존. Install Invasion 최초 생성도 CSV를 사용하고 코드 seed는 없다. 콘텐츠 빌드 전 CSV/SO 일치를 검사한다.
- `GameData.LoadSettingsAsync<ZombieData>`가 앱 수명 캐시/핸들을 소유. `ZombieInvasionController.InitializeAsync`가 소비. 실패 시 오류 노출, 임의 기본값 없음. 공용 로더의 재시도/해제 계약 재사용.
- 소비 씬 `ShelterPrototype`, 프리팹 `Assets/Prefabs/Shelter/Zombie.prefab`. 프리팹은 외형만 소유, 밸런스 수치는 로드한 데이터 적용.

## 필드 계약

모든 필드는 필수, 빈 값/암묵 기본값 없음. 아래 값은 최초 기획 스냅샷이며 정본은 위 시트. CsvTableReader/ZombieCsvImporter/ZombieData.Validate에서 검사.

| 필드 | 타입 | 의미·단위 | 제약 / 최초 값 |
|---|---|---|---|
| entries | ZombieDefinition[] | 좀비 카탈로그 | 1개 이상, null 금지 |
| entries.id | string | 안정적 좀비 ID | 공백 불가, ordinal 고유 |
| entries.displayName | string | 표시 이름 | 공백 불가 |
| entries.firstWave | int | 이 종류를 쓰기 시작하는 1-based 웨이브 | >0, 고유, 첫 구간 1 필수; 다음 구간 전까지 적용, 마지막은 계속 적용 |
| entries.combat | int | 주사위에 더할 능력 포인트 | 1~diceSides |
| entries.hp | int | 기본 생명력 포인트 | >0 |
| diceSides | int | 주사위 면 수 | 2 이상, 최초 6 |
| combatEveryTicks | int | 근접전 판정 간격 / 벙커 기본 틱 | >0, 5 |
| attemptsPerDefender | int | 구 시트 호환 열, 현재 전투에서 미사용 | >0, 최초 3 |
| damageToZombie | int | 문 앞 명중 또는 근접전 승리당 HP 감소 | >0, 최초 1 |
| mentalDamage | int | 좀비 승리당 해당 캐릭터 멘탈 감소 | >0, 최초 2 |
| mentalRecovery | int | 처치당 참가자 멘탈 회복 | >=0, 최초 5 |
| walkSpeed / climbSpeed | float | 수평 이동 / 이전 사다리 속도(미소비 호환 필드), world units/sec | 유한 >0, 임시 2 / 1.6 |
| encounterDistance | float | 발 위치 기준 접촉 반경 / world units | 유한 >0, 임시 0.65 |
| spawnOffset | float | 구 시트의 고정 생성 거리, 현재 생성에서 미사용 | 유한 >0, 최초 1.2 |
| minimumSpacing | float | 좀비 발 위치 사이 최소 거리, 생성·지상 대기열 공용 / world units | 유한 >0, 최초 임시 2.4. 사람과의 encounterDistance와 다른 의미 |

초기 레코드: zombie_1 / 좀비 / firstWave 1 / combat 1 / hp 2; zombie_2 / 덩치 좀비 / 4 / 2 / 4; zombie_3 / 성난 좀비 / 7 / 3 / 5; zombie_4 / 거대 좀비 / 10 / 4 / 6. 웨이브마다 한 마리. `ForWave`가 조회하며 런타임 HP는 별도 복사; 현재 웨이브 보정은 생성 때만 적용.

ID 변경/삭제 전 소비자·기획 참조 검색 필수. 영구 저장 없음. 정렬과 표시명은 ID를 바꾸지 않으며 firstWave로 선택. 주소 이전 시 로더/콘텐츠를 함께 갱신, 구 주소는 참조 전환 검증 후 폐기.

## 동작 계약

선두 좀비는 철문 밖에서 BunkerRules.InvasionApproachTicks × SecondsPerTick × ZombieData.WalkSpeed만큼 떨어진 곳에 생성된다. 뒤에 생성된 개체는 줄 간격을 지킨다. 길이 막히면 실제 도착도 늦어진다. 보초는 각자 서 있던 위치에서 선두가 문에 도착하면 공격한다. 돌파 후에는 좀비가 가장 가까운 보초에게 걸어가며 encounterDistance 안에 있는 인원만 근접전에 참여한다.

선두 좀비가 입구에 도착하면 보초가 문 안쪽에서 번갈아 공격한다. 매번 기존 damageToZombie만큼 확정 피해를 주고, 도착 시 행동 가능한 보초가 없거나 세 번째 타격에서 생존하면 즉시 침입한다. 문 앞에서 쓰러지면 침입 멘탈 손실은 발생하지 않는다. [벙커 규칙](BunkerRules.md)

실내전은 좀비가 죽거나 행동 가능한 아군이 없어질 때까지 같은 대상으로 반복한다. 한 회차에 좀비는 한 명만 공격한다. 실내 동점자만 재굴림한다. 판정 횟수에 의한 돌파는 없다. 전투 방에서는 수비 중인 동안 그 방을 점거하고, 수비가 무너지면 남은 HP로 다른 방까지 실제 경로로 이동한다. 통로 조우와 목표 방 점거 전투도 같은 종료 조건을 사용한다. 점거·HP·보상은 개체 단위이며 재시작 시 정리한다. [변경 계획](../DataHistory/20261118-zombie-approach-planned.md)

## 기존 데이터와 중복 조사

최초 구현 조사(이전 이력): Assets/ProjectSettings/Docs의 zombie/좀비/HP/combat/mental/faint 및 DataRegistry planned 조사: 좀비 정본/예약 없음. Characters.Combat 재사용, BunkerSimulation.GetCurrentWaveEnemyHp 재사용, 공용 GameData 로더와 기존 자동 경로 재사용. 전역 멘탈 상한/기절은 기존 BunkerRules 확장(100 / 40틱 / 회복30), 새 캐릭터/멘탈 DB를 만들지 않음. 기존 이벤트·침입 주기와 전체 틱 설정은 보존. 노션의 원래 거실 즉시 출현은 최신 사용자 경로 이동 요청으로 대체. 기획의 시간당 전원 멘탈 감소는 이번 요청 범위에 추가하지 않음.

## 변경과 검증

구현·씬 연결 완료. [계획](../DataHistory/20261118-zombie-invasion-01-planned.md), [적용](../DataHistory/20261118-zombie-invasion-02-applied.md), [최종 검증](../DataHistory/20261118-zombie-invasion-03-verified.md). 컴파일/콘텐츠 빌드, Edit 6개, Play 42개 통과. 기존 이벤트 Edit/Play 회귀 검사 통과. 실제 Update 6틱 동안 전투 1회 발생, 방어/점거 화면 캡처 확인. 플레이어 실행 파일 빌드/장시간 밸런스/시간당 침입 멘탈 감소는 범위 밖. 연결 가능한 레이아웃만 이동하며 끊긴 경로를 순간이동으로 우회하지 않는다.

2026-11-18 시트 이전 시 최신 Registry와 Assets/ProjectSettings/Docs의 동일 의미를 재검사했다. 기존 data.zombies를 확장·이전했고 공용 GameDatabase에는 두 테이블을 넣지 않는다. 기존 SO GUID/주소/소비자 유지, CSV와 SO는 시트 생성물이다. Editor 36개/Play 17개 및 원격 7개 시트 실제 가져오기·콘텐츠 빌드 검증을 완료했다. [최종 검증](../DataHistory/20261118-zombie-sheets-03-remote-verified.md)

좀비는 사람과 같은 차량을 이용하며 같은 층에서 둘까지 동승한다. 탑승자는 지상의 전투·충돌 후보에서 제외하고 하차 후 복귀한다. climbSpeed는 시트 호환용으로 유지한다. [엘리베이터 공용 규칙](ElevatorRules.md).
