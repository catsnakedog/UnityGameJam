# 시작 인물 선택

시작 화면은 `data.characters`의 인물 ID를 현재 회차에 전달한다. 인물 표시 정보는 Characters 표, 파밍 인원의 무게와 종류는 `data.farming.pickups`, 직접 씬 실행의 시작 인물은 `data.farming.rules.startingPersonId`에서 읽는다. 선택한 ID는 파밍 재시작 동안 유지하고 새 게임 선택으로 교체한다.

`audio.user-settings`는 기존 SoundManager의 PlayerPrefs를 사용한다. 회차 선택과 옵션 UI를 위해 CSV, 설정 에셋 또는 별도 저장 키를 추가하지 않았다. [시작 화면](../UI/Title.md) · [파밍 규칙](../DataSpecs/FarmingRules.md) · [사운드 설정](../DataSpecs/SoundSettings.md)
