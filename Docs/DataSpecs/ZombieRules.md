# 좀비 공통 규칙 시트 명세

## 식별과 담당 범위

- 데이터 ID `data.zombies`의 공통 규칙 테이블 `ZombieRules`, 스키마 v1, 2026-11-18.
- 상태 active / Codex zombie-sheets-20261118. 기존 SO 규칙을 이전하며 새 의미의 데이터 ID를 만들지 않는다.
- 좀비 주사위 전투와 이동·접촉·대기열 간격만 소유. 웨이브 발생 시각/캐릭터 능력/전역 멘탈 상한/이벤트 HP 보정/현재 HP는 제외한다.

## 정본과 연결

- 편집 정본: [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486). 기존 ZombieData SO는 읽기 전용 생성물로 전환했다. 최초 스냅샷과 원격/로컬/SO의 모든 값을 대조했다.
- 스냅샷 `Assets/Settings/Shelter/ZombieRules.csv`, UTF-8 BOM, 설명/타입/필드명/데이터 순서. `Zombies`와 한 번에 검증·반영한다.
- 생성물, GUID/주소/그룹, 소비자, 캐시·해제 및 실패 수명은 [Zombies](Zombies.md)와 동일하다. 공용 GameDatabase에 중복 저장하지 않는다.
- 기존 전체 시트 가져오기 → 공용 CsvTableReader → ZombieCsvImporter → 같은 ZombieData SO → Addressables 콘텐츠 빌드. 새 런타임 로더 없음.

## 필드 계약

모든 열은 필수, 빈 값/암묵 기본값 없음. 규칙 ID는 ordinal `default` 한 행만 허용하며 삭제/이름 변경은 오류다. 숫자는 소수점 `.`을 사용한다. 초기값은 아래의 조회용 스냅샷이며 독립 정본이 아니다.

| 필드 | 타입 | 의미·단위 | 제약 / 초기값 | 검증 |
|---|---|---|---|---|
| id | string | 공통 규칙 키, 무차원 | 정확히 default 1행 | ZombieCsvImporter |
| diceSides | int | 주사위 면 수 | >=2 / 6 | ZombieData.Validate |
| combatEveryTicks | int | 근접전 판정 간격, 벙커 기본 틱. 보초의 원거리 사격 간격은 BunkerRules.guardVolleyEveryTicks에서 조회 | >0 / 5 | 동일 |
| attemptsPerDefender | int | 시트 호환용 구 판정 한도. 현재 교전 종료에 미사용 | >0 / 3 | 동일 |
| damageToZombie | int | 캐릭터 승리당 HP 감소, 포인트 | >0 / 1 | 동일 |
| mentalDamage | int | 좀비 승리당 해당 캐릭터 멘탈 감소, 포인트 | >0 / 2 | 동일 |
| mentalRecovery | int | 처치 시 참가자 멘탈 회복, 포인트 | >=0 / 5 | 동일 |
| walkSpeed | float | 수평 속도, world units/sec | 유한 >0 / 임시 2 | 동일 |
| climbSpeed | float | 이전 사다리 속도, world units/sec. 엘리베이터 전환 후 런타임 미소비 호환 열 | 유한 >0 / 임시 1.6 | 동일 |
| encounterDistance | float | 사람과의 접촉 반경, world units | 유한 >0 / 임시 0.65 | 동일 |
| spawnOffset | float | 시트 호환용 구 고정 거리. 현재 스폰은 공용 walkSpeed와 벙커 접근 시간으로 계산 | 유한 >0 / 최초 1.2 | 동일 |
| minimumSpacing | float | 좀비 사이 최소 간격, world units | 유한 >0 / 임시 2.4 | 동일 |

## 관계·이전·중복 조사

- `Zombies.combat <= ZombieRules.diceSides`를 함께 검증한다. 종류별 행에 공통 규칙 값을 반복 저장하지 않는다.
- 기존 SO 필드와 같은 의미이므로 정본을 이전하며, 기존 GameData.LoadSettingsAsync/ZombieData 조회 API·GUID·주소는 보존한다. 원격 내보내기와 SO 대조 성공 후 Inspector 직접 편집과 설치 도구 seed를 폐기했다.
- DataRegistry planned 예약과 Assets/ProjectSettings/Docs의 combatEveryTicks/mentalDamage/minimumSpacing 검색 결과 기존 ZombieData가 정본이다. BunkerRules의 습격 주기/멘탈 상한과 다른 의미이며 재정의하지 않는다. 기존 최소 간격 2.4는 최신 대기열 작업의 값을 보존한다.
- CSV/생성 SO/빌드 캐시는 파생물이다. 런타임 HP와 점거 상태는 별도 객체, 저장 키 추가 없음. 시트 미연결/다운로드 실패 시 임의 초기값으로 대체하지 않는다.

## 변경과 검증

- [변경 계획](../DataHistory/20261118-zombie-sheets-01-planned.md). 원격 게시·연결 및 실제 가져오기·빌드·값 보존 검증 완료. [최종 검증](../DataHistory/20261118-zombie-sheets-03-remote-verified.md)

차량 속도는 `data.shelter.elevators`에서 읽는다. 지상의 좀비 탑승 대기열과 하차 위치는 기존 `minimumSpacing`을 사용한다. 같은 층으로 걸어가는 좀비는 승강장에서 대기 중인 호출자를 통과한다. [엘리베이터 동승 이력](../DataHistory/20261118-shelter-elevator-pairs.md).

