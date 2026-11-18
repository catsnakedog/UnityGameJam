# 파밍 충돌 겹침으로 인한 이동 정지 수정

2026-11-18

플레이어가 테이블이나 벽과 이미 겹쳐 있으면 기존 CircleCast가 거리 0의 충돌을 반복 반환하여 모든 방향의 이동이 막혔다. 운반 용량에 의한 제한은 아니며 빈손에서도 재현됐다. 첫 테이블 중심인 논리 좌표 `(-5, 6)`에서 수정 전 네 방향 이동 거리가 모두 0인 것을 확인했다.

`Assets/Scripts/Farming/FarmingPlayer.cs`에서 이동 전후 Collider2D.Distance로 겹침을 해소한다. 입력이 없어도 복구하며, 접촉면에서 멀어지는 이동은 차단하지 않는다. 모서리 등에서 복구할 수 없거나 맵 바깥으로 밀려날 상황에는 마지막으로 검증한 바닥 위치로 돌아간다. 플레이어 자식의 충돌체는 이동 장애물에서 제외한다.

충돌 수치의 부호와 분리 방향은 [Unity ColliderDistance2D.normal 문서](https://docs.unity3d.com/kr/2023.2/ScriptReference/ColliderDistance2D-normal.html)를 확인하고 실제 테이블·벽 겹침으로 검증했다. 이동 속도·운반 용량·무게 및 데이터 정본은 변경하지 않았다.

검증 결과:

- [Verification-Movement.txt](Verification-Movement.txt): Unity Play에서 86개 검증 통과. 여섯 테이블, 벽, 모서리 겹침 복구, 외벽 밖 복구, 큰 이동 간격의 충돌 방지, 접촉 직후 후퇴, 2/3/4칸 사람과 1/2칸 아이템 및 가득 찬 인벤토리의 이동·벙커 투입 확인. 테이블·문·복도 외벽 주변에서 1,800회 이동 후 잔여 겹침 없음.
- [Verification-CarryKeyboard.json](Verification-CarryKeyboard.json): 2칸 사람을 든 상태에서 Input System의 W/A/S/D 입력이 실제 Update를 통해 이동으로 이어짐을 확인. 테스트 키보드 및 임시 입력 설정은 종료 시 복구.
- 검증기는 `Assets/Editor/Farming/FarmingMovementVerification.cs`. 새 파밍 Play 세션을 시작하고 Begin 이후 한 프레임 이상 진행한 상태에서 `FarmingMovementVerification.Run()` 실행. 런타임 물체를 운반·투입하므로 검증 후 씬을 재시작한다.
- 이번 변경에서 60초 전체 흐름 검증은 다시 실행하지 않았다. 기존 흐름 결과는 `Verification-2D.txt`에 있으며 이번 검증은 이동·충돌·운반 회귀에 집중했다.
