# 생존자 피곤 표시와 원화 재생 검증

UnityGameJam, 2026-11-19 KST. [변경 전 조사](20261119-survivor-animation-plan.md) · [규칙 명세](../DataSpecs/BunkerRules.md) · [원화 반영 방법](../SurvivorAnimation.md)

기존 `data.bunker.rules`의 `BunkerRules.TiredMentalThreshold`가 50을 제공한다. 정본은 `Assets/Settings/Shelter/BunkerRules.asset`, GUID는 `01a9f70ed623c5341bec7f05cc07781d`, 주소는 `data/bunker-rules`, 그룹은 Game Data Local이다. `data.characters`의 기존 구조 ID와 런타임 `CharacterIdentity.CurrentMental`을 재사용했다. 중복 조사 결과 표시 임계값의 별도 정본은 없었다.

- Unity 6000.3.28f1에서 컴파일 오류·경고 0개. 반입한 .aseprite 21개를 Drive 원본과 바이트 단위로 대조했고 공용 프리팹에서 각 원본 GUID 참조를 확인했다.
- 1~4번 캐릭터의 클립 16개에서 모든 프레임 시작 시각, 전체 길이, 반복 설정을 확인했다. 다른 5개 파일은 기본 그림이며 각각 하나의 Sprite로 임포트됐다.
- 원래 열려 있던 씬의 저장 전 편집을 보존하기 위해 별도 프로젝트 복사본에서 Windows Addressables 콘텐츠를 빌드했다. 첫 빌드는 카탈로그에 데이터 키가 빠져 폐기했다. 전체 경로에서 다시 빌드한 카탈로그에 게임 데이터 키 4개가 들어 있음을 확인한 뒤 원 프로젝트의 로컬 빌드 캐시에 반영했다.
- 원 프로젝트에서 **Use Existing Build**로 벙커를 열었다. 구조 순서를 섞은 6명이 올바른 그림을 표시했다. 멘탈 51과 50의 대기·걷기, 좌우 방향, 0에서 정지 및 회복을 확인했다. 실제 프레임 업데이트에서 1번의 피곤한 대기 그림 10장이 재생됐고 5번의 정지 그림은 유지됐다.
- 앉기→타이핑, 수면, 사격 한 번, 기절 마지막 그림 유지 분기는 Play 인스턴스에 임시 클립 참조를 넣어 검사했다. 해당 동작의 원화는 아직 공유 폴더에 없어 실원화 검수는 추후 진행한다. 프리팹의 해당 슬롯은 비어 있다.
