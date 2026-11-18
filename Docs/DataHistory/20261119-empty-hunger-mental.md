# 허기 소진 시 멘탈 감소

작업 전 등록 목록, 허기·음식 명세, 방·전투·선택 사건·중간 발표의 멘탈 처리 경로를 확인했다. 기존 허기 정본에 감소 배율 필드를 추가했다. 각 효과는 기존 정본에서 기본량을 읽고 CharacterIdentity에서 개인별 배율을 한 번 적용한다. 별도 CSV·로더·아이템별 배율은 추가하지 않았다. [data.bunker.rules 정본](../../Assets/Settings/Shelter/BunkerRules.asset) · [필드와 적용 시점](../DataSpecs/BunkerRules.md)

허기가 0인 동안 감소량은 2배다. 허기 1칸 이상과 멘탈 회복은 1배다. 사용자 요청으로 테스트 및 Play 검증을 생략했다.
