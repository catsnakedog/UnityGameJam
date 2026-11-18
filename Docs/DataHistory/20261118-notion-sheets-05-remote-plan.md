# Google Sheets 정본 전환 계획 — 2026-11-18

사용자가 브라우저 업로드를 명시적으로 선택했다. 제공 Drive 폴더의 SpreadSheet 하위에 기존 CSV 6종을 Google Sheets로 변환했다. 설명/타입/필드명 양식과 기존 ID를 유지하며 실제 내보내기 CSV 38행을 전부 비교해 일치했다.

DataRegistry와 각 6개 명세에 실제 URL/탭·기존 로컬 경로·단방향 전환 정책을 먼저 등록했다. 다음 단계는 기존 SpreadsheetImporter를 통해 공용 5개를 검증/적용하고 기존 ProjectSettings에 URL을 저장하는 것이다. FarmingData는 기존 전용 파서로 검증 후 기존 경로/DB만 갱신한다. 공용 경로로 복사하지 않는다.

원격 테스트 CSV 및 대응 테스트 xlsx, 업로드용 중간 xlsx는 사용자 허용에 따라 휴지통으로 옮긴다. SpreadConfig파일(삭제X)와 docx 문서는 보존한다. 영구 삭제는 하지 않는다. 실제 적용·검증·정리 결과는 새 이력으로 추가한다.
