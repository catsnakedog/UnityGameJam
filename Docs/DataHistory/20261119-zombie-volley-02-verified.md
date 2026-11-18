# 좀비 접근 6틱과 보초 사격 2틱 검증

UnityGameJam · 2026-11-19 00:15 KST · Unity 6000.3.28f1 · Codex. [변경 계획](20261118-zombie-volley-01-plan.md)

| 데이터 ID | 편집 위치와 소비 |
|---|---|
| `data.bunker.rules` | [BunkerRules.asset](../../Assets/Settings/Shelter/BunkerRules.asset)의 `invasionApproachTicks`를 15에서 6으로 조정하고, 보초 사격에 `guardVolleyEveryTicks` 2를 적용했다. `ZombieInvasionController.SpawnWave`가 거리를 계산하고 `ZombieAgent`가 첫 사격과 반복 사격을 예약한다. GUID `01a9f70ed623c5341bec7f05cc07781d`, 그룹 Game Data Local, 주소 `data/bunker-rules`. [필드 명세](../DataSpecs/BunkerRules.md) |
| `data.zombies` | [ZombieRules 시트](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486)의 공용 보행 속도 2유닛/초와 근접전 간격 5틱을 재사용했다. 이동 중 개체와 실내 교전은 기존 `data/zombies` 설정을 읽는다. [명세](../DataSpecs/ZombieRules.md) |

등록·예약 목록과 Assets, ProjectSettings, Docs를 대조한 결과, 보초의 원거리 사격에만 적용되는 기존 필드는 없었다. 벙커 규칙 SO 한 곳에서 접근 및 보초 사격을 편집한다. Editor에서 인스턴스의 두 값을 조회하고 사격 주기 0을 검증기가 거부하는 것도 확인했다.

동일한 프로젝트 사본에서 Default 프로필의 Windows Addressables 콘텐츠를 빌드했다. 주소가 참조하는 에셋·스크립트 12개의 파일 해시를 원본과 대조한 뒤 원본 프로젝트에서 Use Existing Build로 다시 로드했다. 원본 플레이 모드에서 전투 검사 50개와 대기열 검사 18개가 통과했다. [전투 결과](../Zombies/Verification-play.txt) · [대기열 결과](../Zombies/Verification-queue-play.txt)

정식 습격 시계로 30틱에 좀비를 생성하고 보초 한 명을 배치했다. 이 관측에서는 랜덤 이벤트를 끄고 주사위를 고정해 적이 입구까지 생존하도록 했다.

| 생성 후 경과 | 입구까지 남은 거리 | 관측 |
|---:|---:|---|
| 0틱 | 12유닛 | 생성 |
| 2틱 | 8유닛 | 첫 원거리 사격 |
| 4틱 | 4유닛 | 두 번째 원거리 사격 |
| 6틱 | 0유닛 | 세 번째 사격, 침입, 첫 근접전 순으로 처리 |
| 11틱 | 0유닛 | 두 번째 근접전 |
