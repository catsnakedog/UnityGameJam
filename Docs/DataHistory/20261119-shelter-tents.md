# 수면실 텐트

기존 `data.bunker.rules`의 정본은 `Assets/Settings/Shelter/BunkerRules.asset`이다. 방 바닥에는 여러 명이 머무를 수 있다. `sleepingRoomCapacity`는 같은 방에서 동시에 예약하거나 사용 중인 텐트 수를 제한한다. 개별 텐트의 사용자와 도착 여부는 씬의 런타임 상태다.

직접 들어간 사람과 구조된 사람 모두 텐트 안에서만 `sleepMentalPerTick`만큼 회복한다. 구조된 사람의 각성에는 `wakeMental`을, 텐트에서 나오는 시점에는 `mentalMaximum`을 사용한다. 기존 규칙과 씬 참조를 재사용하며 새 설정이나 저장 키를 추가하지 않았다.

Unity 플레이 모드에서 여섯 명의 동시 입장, 두 텐트의 개별 예약과 사용, 점유 중 외형·이름·게이지 숨김, 완전 회복 시 표시 복원 및 구조된 환자의 회복을 확인했다. [필드 명세](../DataSpecs/BunkerRules.md)
