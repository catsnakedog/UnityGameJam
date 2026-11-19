# 튜토리얼 화면 전환 · 2026-11-19

기존 `data.shelter.dialogue`의 [ShelterDialogue.csv](../../Assets/Settings/Data/Csv/ShelterDialogue.csv)에서 방송 시작 3문장과 마무리 2문장의 상황 키를 tutorial_start로 지정했다. 개발 방 소개 3문장은 tutorial_overview를 유지한다. 상황 키의 의미와 수명은 [대사 명세](../DataSpecs/ShelterDialogue.md)에 기록했다.

열린 가방의 초콜렛(choco)과 노트북(laptop)은 기존 [벙커 물자 정본](../DataSpecs/ShelterItems.md)의 data.shelter.items.displayName과 기존 아이콘·슬롯을 사용한다. 실제 재고는 튜토리얼에서 변경하지 않는다. 등록 목록과 프로젝트의 파밍 카메라·가방 프리팹·물자 표를 대조했고 새 데이터 정본이나 로더를 추가할 필요가 없었다.

Data Manager 변경 대조에서 기존 5행의 situation 값만 바뀐 것을 확인하고 Addressables 콘텐츠를 빌드했다. 타이틀에서 튜토리얼을 열어 파밍 시작 구도, 복도 전체, 입구 확대, 가방, 시계와 마지막 구도를 검사했다. 가방은 해당 대사 2개 동안 열리고 두 예시 이름과 보유 개수를 표시한다. 14개 대사를 진행해 파밍 Preparing 진입을 확인했다. [플레이 화면](../Images/TutorialViews.jpg)
