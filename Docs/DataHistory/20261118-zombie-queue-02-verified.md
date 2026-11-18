# 좀비 순차 진입 검증

- 2026-11-18 Asia/Seoul / applied + verified / Codex zombie-queue-20261118. 이전 [계획](20261118-zombie-queue-01-planned.md).
- ID `data.zombies`. 정본 `Assets/Settings/Shelter/ZombieData.asset`, ZombieData, GUID `6845f853ae90e0b479d0f51cb5d53023`, Addressables `data/zombies`, `Game Data Local`, 라벨 없음. [현재 명세](../DataSpecs/Zombies.md).
- 신규 필드 minimumSpacing: 미존재 → 2.4 world units. 양수 유한 검증. 나머지 좀비 레코드·전투·이동 값 유지. 기존 캐릭터/이벤트/BunkerRules 정본·스키마 변경 없음. 기존 GameData 로드/캐시/해제 사용, 새 SO/CSV/DB/로더 없음.
- 최종 전체 값: zombie_1 (firstWave1/combat1/hp2), zombie_2 (4/2/4), zombie_3 (7/3/5), zombie_4 (10/4/6). diceSides6/combatEveryTicks5/attemptsPerDefender3/damageToZombie1/mentalDamage2/mentalRecovery5/walkSpeed2/climbSpeed1.6/encounterDistance0.65/spawnOffset1.2/minimumSpacing2.4.
- 소비: ZombieInvasionController가 생성 순서 대기열/중복 없는 생성 위치/선분 이동 한도를 계산. ZombieAgent.Queued는 실제 입구 도착까지 전투를 소모하지 않으며, 이후 독립 HP/예산/목표/점거/보상을 유지. ZombieTraffic의 선분-원 교차 검사로 긴 프레임의 관통도 차단. ShelterWalker.Tick 선택적 이동 한도 콜백은 기존 웨이포인트/사다리 상태를 보존하여 차단 해제 후 재개. 사람 이동은 콜백 없이 기존 방식.
- 방 안에서 선두에 막히면 겹치지 않는 현재 위치에서 점거한다. 같은 방의 다른 좀비를 제거해도 남은 점거자의 소유권은 유지된다. 막힌 통로를 순간이동/추월로 우회하지 않는다.
- 중복 조사: DataRegistry planned와 Assets/ProjectSettings/Docs의 간격/대기열/spacing/separation 검색, 적용 직전 및 완료 전 목록 재확인. 기존 encounterDistance는 사람과의 교전 반경으로 다른 의미. 충돌하는 신규 정본/예약 없음.

## 실제 검증

- Unity 6000.3.28f1 컴파일 성공. Editor SerializedObject로 기존 SO의 신규 필드만 적용. GUID 유지.
- Addressables Default 프로필 / Windows 로컬 콘텐츠 빌드 성공 (16.04초), Use Existing Build로 실제 로드.
- [간격 Edit 6개](../Zombies/Verification-queue-edit.txt): 수평/사다리 선분 한도, 접촉 후 이탈, 큰 이동의 관통 방지, 다른 경로 허용, 0 간격 데이터 거부.
- [대기열 Play 19개](../Zombies/Verification-queue-play.txt): 동시 3마리 독립 HP, FIFO 생성/방어, 대기 중 무소모, 개별 피해, 앞 좀비 처치 후 한 마리씩 진입, 동시 6마리의 사다리 진입 순서와 최소 간격, 선두 정지 시 추월 금지, 전원 독립 유지, 방 점거·긴 프레임·일시정지·한 마리 처치·재시작 정리.
- 기존 [전투 Edit 6개](../Zombies/Verification-edit.txt), [Play 42개](../Zombies/Verification-play.txt) 회귀 통과. 기존 n×3/전원 탈진/보상/경로 전투/점거 복구/HP 보정 유지.
- 1600×900 실제 게임 캡처 `Temp/ZombieInvasion/queue.png` 직접 확인. 3마리 각각 HP2, 사다리의 서로 다른 위치에서 비중첩 확인 (y11.56,14.70,17.50).
- 실행 파일 빌드 및 대규모 수백 마리 부하 검증은 하지 않았다. 씬 배치·기존 프리팹은 이번 작업에서 변경하지 않았다.
