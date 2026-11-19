# 엔딩 컷씬 대사

`data.shelter.dialogue`의 정본은 `Assets/Settings/Data/Csv/ShelterDialogue.csv`다. 기존 데이터 목록과 상황 키·대사 원문을 확인했다. Bad 불평 3개, Good 좀비 반응 2개와 개발자 대사 3개를 추가했다. Normal은 기존 좀비 침입 대사 3개를 재사용한다. [대사 명세](../DataSpecs/ShelterDialogue.md)

점수 판정과 결과 제목·설명은 기존 `data.game-dev.endings`에서 조회한다. [엔딩 정본과 명세](../DataSpecs/GameDevEndings.md) · [화면 동작](../UI/GameDevEndingPopup.md)

공용 Data Manager로 CSV를 재임포트하고 Addressables 콘텐츠를 빌드했다. Unity 플레이에서 세 판정의 검은 전환, 말풍선, 군중의 도착 위치, Good의 파밍 방 전환, 공통 결과의 제목·점수를 확인했다. 마지막 벙커 틱에서 Bad 결과창이 자동으로 열렸고, 메인 버튼은 타이틀로, 재시도 버튼은 선택한 person_02로 초기화된 파밍 씬으로 이동했다.
