# CRDD

Unity 작업 등록·수신·선점·계약·완료 보고·검수.

- 기획분해SKill이 전달된 기획서 전체를 분석하고 CRDD의 일반 계획 양식으로 작업을 작성한다.
- 통신SKill이 양식과 데이터를 교환한다. 상위 실행 스킬의 내부 단계·플레이 판단은 관제탑의 기능이 아니다.
- 사용자에게 위임된 계획은 automatic, 검토 요청은 review로 등록한다. 두 방식 모두 같은 일반 계획 양식을 사용한다.
- delivered는 최신 명세의 제출 보고 충족 여부이며 실제 파일 검증은 호출자가 수행한다.
- MCP crdd_read/crdd_write. 이전 도구 이름은 호출 호환 별칭만 유지한다.
- 기존 owner-private 접근 범위와 작업·보고·이력·확장 데이터는 보존한다.

검증: `node tests/run.mjs`, `node node_modules/typescript/bin/tsc --noEmit`, Sites build helper.
