# 좀비 입구 대치와 다인 전투

전투 장소에 행동 가능한 수비자가 있으면 선두 좀비는 입구 밖에서 첫 판정까지 기다린다. 입구, 이동 경로, 점거한 방에서 한 좀비와 교전하는 캐릭터는 각자 공격한다. 좀비는 한 회차에 한 명을 노리며, 동점자는 해당 판정만 다시 굴린다. 최종 틱에 처치한 경우에도 살아남은 좀비의 상태와 점거를 결과 화면에 보존한다.

- 전투력·HP·판정 주기·피해는 기존 `data.zombies`의 Zombies와 ZombieRules 시트, 캐릭터 전투력은 `data.characters`의 Characters 시트에서 읽는다. 시트 값, CSV, 생성 SO, 로더 변경은 없다. [좀비 명세](../DataSpecs/Zombies.md) · [공통 규칙](../DataSpecs/ZombieRules.md) · [캐릭터 명세](../DataSpecs/Characters.md)
- DataRegistry의 등록·예정 항목과 Assets, ProjectSettings, Docs의 `attemptsPerDefender`, `combatEveryTicks`, `damageToZombie`, `mentalRecovery`를 대조했다. 기존 규칙을 소비하며 정본을 추가하지 않았다.
- Unity Edit 검사 12개와 Play 검사 125개를 통과했다. Play 검사는 전투 52개, 대기열 19개, 엘리베이터 54개다. 바닥 경계의 수비자 네 명, 한 명에게만 들어가는 피해, 지원군·동점, 같은 방의 두 점거자, 여섯 마리 중 한 마리 처치, 최종 틱의 처치를 확인했다. [전투 결과](../Zombies/Verification-play.txt) · [대기열 결과](../Zombies/Verification-queue-play.txt) · [엘리베이터 결과](../Elevators-checks-play.txt)
