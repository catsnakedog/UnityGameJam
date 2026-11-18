# 좀비 접근, 침입, 보초 피로 적용 및 검증

2026-11-18 · Unity 6000.3.28f1 · ShelterPrototype

| 데이터 ID | 편집 정본과 적용 내용 | 명세 |
|---|---|---|
| `data.bunker.rules` | 기존 [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset)에 접근 15틱, 침입한 좀비 한 마리당 전원 멘탈 손실 2, 보초 개인 멘탈 1점당 배치 8틱을 저장했다. 기존 Addressables 주소는 `data/bunker-rules`다. | [벙커 규칙](../DataSpecs/BunkerRules.md) |
| `data.zombies` | 기존 [ZombieRules 시트](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)의 공용 보행 속도와 5틱 전투 간격을 재사용했다. 이전 횟수 한도와 고정 생성 거리는 시트 호환 열로 남는다. | [좀비](../DataSpecs/Zombies.md) · [공통 규칙](../DataSpecs/ZombieRules.md) |
| `data.characters` | 기존 Characters 시트의 개인 전투력과 시작 멘탈을 읽는다. | [캐릭터](../DataSpecs/Characters.md) |

최신 DataRegistry의 planned 항목 및 Assets·ProjectSettings·Docs의 필드, 단위, 소비자를 대조했다. 접근 시간과 전원 침입 손실, 개인 보초 피로는 벙커 규칙에서만 편집한다. 좀비별 속도나 별도 멘탈 데이터는 추가하지 않았다. [변경 계획](20261118-zombie-approach-planned.md)

## 검증 결과

- Unity 재컴파일 오류 0건. Addressables 콘텐츠를 빌드하고 플레이 모드의 기존 로더로 설정을 읽었다.
- Edit 검사 12개 통과. [좀비](../Zombies/Verification-edit.txt) · [이동 간격](../Zombies/Verification-queue-edit.txt)
- Play 검사 118개 통과. 전투 46개, 대기열 18개, 엘리베이터 54개다. 네 명의 개별 공격, 좀비 한 마리의 단일 반격 대상, 마지막 사격으로 침입 방지, 세 회차 이후 교전 유지, 배치 피로 및 재배치 초기화, 별도 HP와 점거 유지, 여섯 마리 순차 진입을 확인했다. [전투](../Zombies/Verification-play.txt) · [대기열](../Zombies/Verification-queue-play.txt) · [엘리베이터](../Elevators-checks-play.txt)
- 정상 속도로 Update를 실행해 생성부터 20틱까지 매 틱 거리·상태·판정 수와 개인 멘탈을 확인했다. 이때 보초 한 명을 두고 좀비가 이기도록 주사위를 고정했다. 아래는 21개 관측 중 주요 시점이다.

| 생성 후 틱 | 입구까지 거리 | 상태 | 누적 판정 수 | 보초 멘탈 | 다른 구조 인원 멘탈 |
|---:|---:|---|---:|---:|---:|
| 0 | 30 | 접근 | 0 | 60 | 60 |
| 5 | 20 | 첫 사격 | 1 | 60 | 60 |
| 8 | 14 | 보초 피로 | 1 | 59 | 60 |
| 10 | 10 | 두 번째 사격 | 2 | 59 | 60 |
| 15 | 0 | 마지막 사격, 전원 침입 피해, 첫 근접전 | 4 | 55 | 58 |
| 16 | 0 | 보초 피로 | 4 | 54 | 58 |
| 20 | 0 | 두 번째 근접전 | 5 | 52 | 58 |
