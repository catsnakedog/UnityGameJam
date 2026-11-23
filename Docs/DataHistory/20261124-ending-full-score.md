# 세 분야를 모두 완성하면 최종 1점

기존 960틱 마감 평가에서는 300%p를 480으로 나눠 최대 0.625점이 나왔다. 최종 제출 점수는 기획·프로그래밍·그래픽의 진척 합을 세 분야의 최대 진척 합으로 나눈다. 시간 만료와 조기 종료에 같은 계산을 사용한다.

기존 데이터 목록, 예약 항목, 평가 소비자를 대조했다. 중간 발표는 [벙커 규칙 정본](../../Assets/Settings/Shelter/BunkerRules.asset)의 `data.bunker.rules`를, 최종 등급은 [엔딩 시트](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846)의 `data.game-dev.endings`를 사용한다. 분야별 상한은 기존 `GameDevProgress.MaximumPercent`를 재사용한다. [벙커 규칙 명세](../DataSpecs/BunkerRules.md) · [엔딩 명세](../DataSpecs/GameDevEndings.md)

Windows 배포본의 계산기로 0점, 평범·갓겜의 경계와 그 직전, 만점 직전, 정확한 만점까지 일곱 경우를 검사했다. 100/100/99.99는 ‘1.000 미만’, 100/100/100은 ‘1.000’으로 표시됐다. Unity Play Mode에서는 실제 벙커의 완료·탈진·동시 종료로 제출한 점수와 결과창 텍스트를 대조했다. 에디터의 기존 Addressables 캐시를 배포 번들로 갱신하고 컷씬 뒤 1920×1080 정산지에 ‘갓겜’과 ‘평가 점수 1.000’이 함께 표시되는 것까지 확인했다.
