# 벙커 입구 방어 변경

- 변경 데이터는 `data.bunker.rules`다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이다. 접근 시간을 6틱에서 1틱으로 줄이고 좀비 한 마리당 문 앞 피격 한도 3회를 추가했다. 기존 사격 간격 2틱을 재사용한다. [필드 명세](../DataSpecs/BunkerRules.md)
- 피격 횟수는 각 좀비가 보관한다. 데이터 목록과 전투 코드를 대조했다. 구 시트의 attemptsPerDefender는 보초 개인의 시도 횟수이며 문 앞의 전체 보초가 공유하는 한도와 대상이 다르다. 피해량은 기존 `data.zombies`의 ZombieRules.damageToZombie를 재사용한다. [정본과 명세](../DataSpecs/ZombieRules.md)
- Addressables 콘텐츠를 빌드하고 Unity 플레이에서 빈 방 즉시 진입, 보초 교대, 두 번째 타격에 처치, 세 번째 타격 직후 실내전, 보초 탈진, 공격 클립 6개와 HP·멘탈 피해 숫자를 확인했다. 전투 검사 53건과 대기열 검사 18건을 통과했다. [전투](../Zombies/Verification-play.txt) · [대기열](../Zombies/Verification-queue-play.txt)
