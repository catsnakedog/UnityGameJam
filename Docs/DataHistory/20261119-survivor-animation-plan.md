# 벙커 생존자 애니메이션 · 변경 전

UnityGameJam, 2026-11-19 KST. 사용자가 멘탈 50 이하에서 피곤한 대기와 걷기를 재생하도록 확정했다.

Assets, ProjectSettings, Docs 및 데이터 목록의 예약 항목에서 tired, 피곤, 저멘탈, wakeMental과 멘탈 경계를 조사했다. 회복 멘탈 30, 상한 100, 개발 해금 퍼센트는 각각 별개의 의미다. 기존 규칙에는 피곤 표시 기준이 없다.

기존 `data.bunker.rules`의 BunkerRules SO에 정수 필드 하나를 추가한다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`, GUID는 `01a9f70ed623c5341bec7f05cc07781d`, 주소는 `data/bunker-rules`, 그룹은 Game Data Local이다. 기존 BunkerSimulation과 CharacterIdentity가 설정을 공급한다. `data.characters`의 ID로 그림을 매칭하고 런타임 멘탈을 읽는다. 원화는 Drive에서 Unity 아트 폴더로 반입하며 캐릭터 표와 별개다.
