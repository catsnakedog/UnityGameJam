# 좀비 침입 검증 — 20261118-zombie-invasion-03

- UnityGameJam / 2026-11-18 Asia/Seoul / verified / Codex zombie-invasion-20261118.
- 이전 [적용 이력](20261118-zombie-invasion-02-applied.md), [전체 연결·사용법](../Zombies/README.md), [데이터 명세](../DataSpecs/Zombies.md).
- 최종 데이터: data.zombies SO `Assets/Settings/Shelter/ZombieData.asset`, GUID 6845f853ae90e0b479d0f51cb5d53023, data/zombies / Game Data Local. 4종의 firstWave/combat/hp = (1,1,2), (4,2,4), (7,3,5), (10,4,6). diceSides6/combatEveryTicks5/attemptsPerDefender3/damageToZombie1/mentalDamage2/mentalRecovery5/walkSpeed2/climbSpeed1.6/encounterDistance0.65/spawnOffset1.2.
- data.bunker.rules: 기존 GUID 01a9f70ed623c5341bec7f05cc07781d / data/bunker-rules. mentalMaximum100/faintDurationTicks40/wakeMental30 추가, 기존 120틱/1초/이벤트10/침입30/작업1/계수0.1/기획해금50 유지. 소비 CharacterIdentity/BunkerSimulation, 기존 이벤트 멘탈 결과도 같은 ClampMental을 거쳐 실제 상한 적용량을 표시.
- data.characters / data.events: 기존 시트/CSV/공용 GameDatabase(data/catalog, GUID62a272592b807b3469b072879c349759) 유지, 수치 변경 없음. 새 정본이나 로더 없음. ZombieAgent는 Combat/CurrentMental을 사용하고 생성기는 기존 다음 웨이브 HP 보정을 1회 사용.
- 설치: ShelterPrototype 씬 기존 지도 보존, ZombieInvasionController 참조 연결, Zombie.prefab 외형·한글 HP 라벨 추가. Survivor.prefab 이름 라벨은 기존 한글 폰트로 연결하여 현재 멘탈/탈진 틱을 표시. 숫자 정본 복제 없음.
- 중복 조사: 작업 전·적용 직전·마무리 DataRegistry planned와 Assets/ProjectSettings/Docs 검색. 동시 파밍 맵/시트 작업과 담당 의미 충돌 없음. 관련 없는 데이터/맵 이전 없음.

## 실제 실행 결과

- 컴파일 성공. Addressables Default 프로필 콘텐츠 빌드 성공(Windows 로컬, Library/com.unity.addressables/aa/Windows/settings.json), Use Existing Build로 실제 로드.
- [Edit 6개](../Zombies/Verification-edit.txt): 4종 웨이브 구간, 최종 구간 연장, 주사위 승/패/동점, 중복 ID 거부.
- [Play 42개](../Zombies/Verification-play.txt): 실제 구조자 명단·공용 로더, n×3 예산/마지막 판정 처치, 한 명 탈진과 전원 탈진 구분, 동점 무소모·무한 루프 방지, 40틱 기절/30회복/100상한, HP 보존, 사다리 경로 강제 전투·이전 이동 명령 보존, 점거·입실 전투·해방, 복수 점거 소유권, 실제 작업 틱 차단/복구, 팝업 정지, 다음 웨이브 HP 소비/후속 웨이브 복원, 복수 좀비, 재시작/종료 정리, 끊긴 사다리 대기/재연결.
- 기존 EventPopupChecks Edit 및 Play 67개 회귀 검사 통과: 12개 사건 Yes/No·원자성·팝업·정지·NEXT_WAVE_HP·재시작 보존.
- 실제 프레임 Update 6.2초 실행: tick6, 방어 판정1회, HP2→1 확인. `Temp/ZombieInvasion/defense.png`, `occupation.png` 1600×900 캡처를 직접 확인: 위 방 옆 좀비, 한글 멘탈/HP, 점거 빨간 방 이름/바닥, 상태 메시지 정상.
- 최초 경로 접촉 테스트는 인물을 사다리에서 반경 밖에 세워 실패했으며, 실제 사다리 위 경로로 테스트 배치를 수정하고 전체 Play 42개 통과. 경로 밖 인물을 강제 공격하도록 게임 규칙을 바꾸지 않음.
- Unity를 공유하는 다른 작업의 Play 전환/컴파일 중 일부 CLI 요청이 타임아웃/무응답을 반환. 안정된 Play에서 위 검증을 완료. 임시 테스트 데이터는 정본에 쓰지 않음.

## 한계

실행 파일 빌드, 장시간 난이도/다수 좀비 성능, 시간당 침입 멘탈 감소, 수면·보초의 지속 멘탈 변화는 이번 검증/구현 범위 밖. 룸 기능 차단 공용 계약은 IsOperational이며 현재 구현된 개발 작업에 실제 연결되었다. 이후 추가하는 방 기능도 이 계약을 사용해야 한다.
