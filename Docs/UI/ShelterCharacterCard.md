# 벙커 캐릭터 정보 카드

벙커에서 구조한 생존자를 클릭하면 1920×1080 게임 영역 왼쪽 24px, 위쪽 212px에 정보 카드가 열린다. 확대된 시계와 카드 사이의 간격은 16px다. 다른 생존자를 클릭하면 같은 카드에 그 사람의 정보가 표시된다. 닫기 버튼은 카드만 닫고, 기존 방 이동 명령과 캐릭터 선택은 유지한다.

| 영역 | 표시와 편집 |
|---|---|
| 이름·직군 | 선택한 생존자의 `CharacterIdentity.Definition` |
| 초상과 스탯 | 404×264 마스크 안에 해당 인물의 전신 Sprite를 확대해 머리부터 허리까지 표시한다. 초상 위 왼쪽에 현재 멘탈, 게이지, 기획·프로그래밍·아트·전투 능력을 겹쳐 표시한다. |
| 장착 아이템 | 선택한 인물의 읽기 전용 장착 목록에서 아이콘과 이름을 표시한다. 비어 있으면 빈 상태 문구를 표시하고, 항목이 영역을 넘으면 스크롤한다. |

- 화면 프리팹은 `Assets/Prefabs/UI/ShelterCharacterCard.prefab`, 장착 행은 `Assets/Prefabs/UI/CharacterEquipmentRow.prefab`이다. 기존 16:9 뷰포트와 한글 폰트를 사용한다. 카드 밖은 월드 입력을 통과시키고 이벤트·엔딩 팝업은 카드 위에 표시한다.
- 초상은 `Assets/Art/Farming/SideView/person_01.png`부터 `person_06.png`까지 기존 Sprite를 참조한다. 캐릭터 ID와 Sprite 이름을 대응시킨다. 구도는 Portrait mask 아래 Image의 RectTransform에서 조절한다.
- 멘탈은 캐릭터의 현재 값, 상한은 벙커 규칙에서 읽는다. [캐릭터 명세](../DataSpecs/Characters.md) · [벙커 규칙](../DataSpecs/BunkerRules.md)
- 초상 바로 아래에 허기 10칸을 표시한다. 채운 칸은 호박색이며 소모된 칸은 빈 테두리로 남는다. 현재 선택한 인물의 상태를 표시한다.
- CharacterIdentity가 ICharacterEquipmentView를 제공한다. 장착 칸을 더블클릭하면 장비가 가방으로 돌아간다. 가방에서 카드 위에 놓으면 카드에 보이는 인물에게 사용·장착한다.

- 초상 배경은 `Assets/Art/UI/CharacterCard/portrait_field.png`의 404×264 Single Sprite이며 Point, 100 PPU, 압축·mipmap 끔이다. PNG 교체 시 같은 경로와 .meta를 유지한다. 프레임·게이지·닫기 버튼은 기존 `Assets/Art/UI/Farming` 이미지를 참조한다.

[1920×1080 플레이 화면](../../outputs/ShelterCharacterCard/character-card.png) · [4:3에서 가방과 동시 표시](../../outputs/ShelterCharacterCard/card-with-bag-4x3.png) · [검증 범위](../DataHistory/20261118-shelter-character-card-verified.md)
