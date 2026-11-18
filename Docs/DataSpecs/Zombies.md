# 좀비 전투·침입 데이터 명세

## 식별과 담당 범위

- ID `data.zombies`, v1, 2026-11-18, active / Codex zombie-invasion-20261118.
- 소유: 좀비 종류·웨이브별 선택, 기본 HP/전투력, 주사위 전투 규칙, 좀비 이동/접촉 수치. 별칭 Zombies, zombie_1~4, COMBAT_TICK, RAID_COMBAT_TICKS.
- 제외: 캐릭터 능력·초기 멘탈(data.characters), 전역 멘탈/기절·발생 주기(data.bunker.rules), 다음 웨이브 HP 보정(data.events의 런타임 효과), 방 배치(ShelterArea), 남은 HP·점거·전투 횟수(런타임).
- 근거: [노션 전투](https://app.notion.com/p/41caca30f5dc808cae8bc2fc08977a8f), [전역 상수](https://app.notion.com/p/41caca30f5dc800d82dee2b37b164f48), 사용자 2026-11-18 확인: **전투 공간 전원 멘탈 0**, **시작 인원×3회 판정 후 생존 시 침입**. 동점은 판정 횟수에서 제외. 침입 경로 이동/점거는 사용자의 최신 요청을 적용.

## 정본과 연결

- 편집 정본: `Assets/Settings/Shelter/ZombieData.asset` (ZombieData SO). CSV/별도 로더 없음. 외부 원본은 노션 기획이며 자동 동기화 없음.
- Addressables: `data/zombies`, 기존 `Game Data Local`, 라벨 없음. GUID `6845f853ae90e0b479d0f51cb5d53023`.
- `Tools > Zombies > Install Invasion` 최초 생성만 초기값 입력. 기존 에셋은 보존하고 Validate. 수정은 Inspector, 이후 Addressables 콘텐츠 빌드.
- `GameData.LoadSettingsAsync<ZombieData>`가 앱 수명 캐시/핸들을 소유. `ZombieInvasionController.InitializeAsync`가 소비. 실패 시 오류 노출, 임의 기본값 없음. 공용 로더의 재시도/해제 계약 재사용.
- 소비 씬 `ShelterPrototype`, 프리팹 `Assets/Prefabs/Shelter/Zombie.prefab`. 프리팹은 외형만 소유, 밸런스 수치는 로드한 데이터 적용.

## 필드 계약

모든 필드는 필수, 빈 값/암묵 기본값 없음. 아래 값은 최초 기획 스냅샷이며 정본은 SO. ZombieData.Validate에서 검사.

| 필드 | 타입 | 의미·단위 | 제약 / 최초 값 |
|---|---|---|---|
| entries | ZombieDefinition[] | 좀비 카탈로그 | 1개 이상, null 금지 |
| entries.id | string | 안정적 좀비 ID | 공백 불가, ordinal 고유 |
| entries.displayName | string | 표시 이름 | 공백 불가 |
| entries.firstWave | int | 이 종류를 쓰기 시작하는 1-based 웨이브 | >0, 고유, 첫 구간 1 필수; 다음 구간 전까지 적용, 마지막은 계속 적용 |
| entries.combat | int | 주사위에 더할 능력 포인트 | 1~diceSides |
| entries.hp | int | 기본 생명력 포인트 | >0 |
| diceSides | int | 주사위 면 수 | 2 이상, 최초 6 |
| combatEveryTicks | int | 판정 간격 / 기존 벙커 기본 틱 | >0, 최초 5 |
| attemptsPerDefender | int | 방어 시작 인원당 판정 예산 | >0, 최초 3 |
| damageToZombie | int | 캐릭터 승리당 HP 감소 | >0, 최초 1 |
| mentalDamage | int | 좀비 승리당 해당 캐릭터 멘탈 감소 | >0, 최초 2 |
| mentalRecovery | int | 처치당 참가자 멘탈 회복 | >=0, 최초 5 |
| walkSpeed / climbSpeed | float | 좀비 수평/사다리 이동 / world units/sec | 유한 >0, 임시 2 / 1.6 |
| encounterDistance | float | 발 위치 기준 접촉 반경 / world units | 유한 >0, 임시 0.65 |
| spawnOffset | float | 전투 방 오른쪽 경계부터 생성 거리 / world units | 유한 >0, 임시 1.2 |

초기 레코드: zombie_1 / 좀비 / firstWave 1 / combat 1 / hp 2; zombie_2 / 덩치 좀비 / 4 / 2 / 4; zombie_3 / 성난 좀비 / 7 / 3 / 5; zombie_4 / 거대 좀비 / 10 / 4 / 6. 웨이브마다 한 마리. `ForWave`가 조회하며 런타임 HP는 별도 복사; 현재 웨이브 보정은 생성 때만 적용.

ID 변경/삭제 전 소비자·기획 참조 검색 필수. 영구 저장 없음. 정렬과 표시명은 ID를 바꾸지 않으며 firstWave로 선택. 주소 이전 시 로더/콘텐츠를 함께 갱신, 구 주소는 참조 전환 검증 후 폐기.

## 동작 계약

방어 중 전투 공간에 있는 행동 가능한 인원을 순환 선택. 전투 중인 1명만 이동 잠금. 시작 인원으로 고정한 예산 사용; 이탈/전원 탈진 시 침입. 주사위 동점 재굴림은 프레임당 유한 횟수만 처리하고 다음 프레임에 이어서 실행하여 무한 루프 방지.

침입은 남은 HP를 유지, 연결된 비전투 방 중 목표 선택, 기존 ShelterNavigation 경로로 이동. 경로에서 만난 캐릭터와 강제 1회 판정 후 살아 있으면 이동 계속. 같은 접촉 구간에서 중복 전투하지 않으며 떨어진 후 재접촉하면 재전투. 목표 방에 들어가면 점거; 전투 가능한 입실자와 처치까지 반복 전투. 방 점거는 이동 가능 여부와 별개이며 IsOperational/CanWorkInCurrentRoom을 차단. 여러 좀비가 있으면 마지막 점거자가 사라져야 복구.

방어 처치 시 현재 전투 공간 인원, 침입/점거 처치 시 해당 좀비와 전투한 참가자에게 1회 회복. 더 이상 이동 가능한 목표가 없으면 대기하고 맵 변경 후 경로 재탐색. 팝업 정지 중 전투·이동 정지. 재시작/씬 종료 시 잠금·점거 정리.

## 기존 데이터와 중복 조사

Assets/ProjectSettings/Docs의 zombie/좀비/HP/combat/mental/faint 및 DataRegistry planned 조사: 좀비 정본/예약 없음. Characters.Combat 재사용, BunkerSimulation.GetCurrentWaveEnemyHp 재사용, 공용 GameData 로더와 기존 자동 경로 재사용. 전역 멘탈 상한/기절은 기존 BunkerRules 확장(100 / 40틱 / 회복30), 새 캐릭터/멘탈 DB를 만들지 않음. 기존 이벤트·침입 주기와 전체 틱 설정은 보존. 노션의 원래 거실 즉시 출현은 최신 사용자 경로 이동 요청으로 대체. 기획의 시간당 전원 멘탈 감소는 이번 요청 범위에 추가하지 않음.

## 변경과 검증

구현·씬 연결 완료. [계획](../DataHistory/20261118-zombie-invasion-01-planned.md), [적용](../DataHistory/20261118-zombie-invasion-02-applied.md), [최종 검증](../DataHistory/20261118-zombie-invasion-03-verified.md). 컴파일/콘텐츠 빌드, Edit 6개, Play 42개 통과. 기존 이벤트 Edit/Play 회귀 검사 통과. 실제 Update 6틱 동안 전투 1회 발생, 방어/점거 화면 캡처 확인. 플레이어 실행 파일 빌드/장시간 밸런스/시간당 침입 멘탈 감소는 범위 밖. 연결 가능한 레이아웃만 이동하며 끊긴 경로를 순간이동으로 우회하지 않는다.
