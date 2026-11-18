# 엘리베이터 교체 계획

- 2026-11-18 Asia/Seoul / planned / Codex shelter-elevators-20261118.
- 요청: 사다리를 엘리베이터로 전부 교체, 현재 층 준비 시만 탑승, 호출 주도권으로 왕복 루프 방지, 문 모션 없이 내부 인물이 보이는 운반.
- 신규 data.shelter.elevators: Assets/Settings/Shelter/ElevatorRules.asset / ElevatorRules / data/elevators / Game Data Local / GUID 미생성. travelSpeed3, queueSpacing2.4, boardingOffset2.4, exitOffset1.2는 최초 임시 값. [필드·수명 명세](../DataSpecs/ElevatorRules.md).
- 기존 data.zombies는 최신 시트 정본/CSV 스냅샷/생성 SO 유지. 개인 climbSpeed 소비만 중단. 기존 BunkerRules/Characters/Events 수치 변경 없음. 공용 GameData 사용, 새 CSV 로더 없음.
- 기존 2개 사다리 위치에 차량과 투영 샤프트를 배치, 각 연결층 호출 발판 보강. 기존 방/통로와 자동 그래프 재사용. 소유권 FIFO/취소/탑승 중 재명령/씬 정리와 플레이어·좀비 연동 검증 예정.
