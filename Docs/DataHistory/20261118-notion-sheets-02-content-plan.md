# 노션 확정 콘텐츠 반영 계획

- 작업: Codex notion-sheets-20261118.
- `data.characters` 기존 6행을 노션 캐릭터 순서와 대응한다. 기존 Person 참조를 지키기 위해 person_01~06을 유지하고 designId로 jiwoo/taesan/saebyeok/dasom/dohyun/gunwoo를 명시한다. displayName, programming, art, planning, mental을 노션 값으로 갱신한다. role/combat/description을 기존 테이블에 추가한다. combat은 인물 정의이며 좀비/전투 기능 구현은 하지 않는다.
- `data.shelter.items`는 기존 3개 물자의 소비 효과를 mentalRestore=10, useKind=mental로 명시한다. sourceEvent는 기존 Events ID를 참조한다. AED는 노션에서 ID/획득처가 비어 있고 기절 해제 문구가 모호하므로 임의 ID/효과로 생성하지 않는다.
- 각 현재 명세와 목록을 먼저 갱신하고 같은 CSV·같은 생성 DB를 사용한다. 별도 캐릭터/물자 DB 또는 파밍 복제본 없음.
- 데이터 정의/API 연결과 플레이에서 기존 이름·능력·멘탈 반영을 확인한다. 아이템 드래그 사용, 기절, 중간 평가 등 미구현은 완료 처리하지 않는다.
