# 벙커 캐릭터 정보 카드 연결

2026-11-18 · Unity 6000.3.28f1 · ShelterPrototype Editor Play

| ID | 사용 위치 | 정본 |
|---|---|---|
| `data.characters` | 선택한 생존자의 이름, 직군, 기획·프로그래밍·아트·전투, 초상 매칭 ID | Google Sheets Characters → `Assets/Settings/Data/Csv/Characters.csv`. [캐릭터 명세](../DataSpecs/Characters.md) |
| `data.bunker.rules` | 멘탈 표시 상한. 현재 멘탈은 각 CharacterIdentity가 소유 | `Assets/Settings/Shelter/BunkerRules.asset`. [규칙 명세](../DataSpecs/BunkerRules.md) |

목록의 planned와 기존 캐릭터·물자·장착·초상 소비자를 확인했다. 카드에 능력이나 멘탈 수치를 복제하지 않는다. 장착 목록은 캐릭터별 읽기 인터페이스이며 현재 게임에 장착 데이터를 쓰는 시스템은 없다.

- 기존 파밍 인계로 여섯 ID를 벙커에 전달한 뒤, 씬에 생성된 여섯 명을 각각 화면 좌표로 선택했다. 이름·역할·네 스탯·초상 Sprite가 실제 정의와 일치했다. 마우스 입력으로 캐릭터를 선택하고 닫기 버튼을 눌러 카드만 닫히는 것도 확인했다.
- 선택 중 현재 멘탈을 35, 0, 초기값으로 변경해 라벨과 게이지가 갱신되는지 확인했다. 가려진 월드로 클릭이 전달되지 않았고, 카드 영역 밖의 방 이동 명령이 작동했다. 선택한 생존자가 비활성화되면 카드가 닫혔다.
- 실제 캐릭터에서는 빈 장착 상태를 확인했다. Play 중 임시 장착 목록을 연결해 4개 아이콘·이름, 스크롤 범위, 목록 축소·비움, 인물 전환 후 변경 구독 해제와 재선택을 확인했다. 테스트 항목은 씬·게임 데이터에 저장하지 않았다.
- 1920×1080 및 1440×1080 Game View에서 상반신 마스크와 배치를 확인했다. 가방과 동시에 열었을 때 두 패널이 겹치지 않았다. 초상 배경 PNG의 한 픽셀을 바꿔 재임포트·원복한 후 Sprite의 GUID와 local ID 및 씬 참조를 확인했다. [화면](../UI/ShelterCharacterCard.md)
