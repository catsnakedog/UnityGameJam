# 노션 대조와 스프레드시트 준비 계획

- 작업: Codex notion-sheets-20261118. 사용자 요청: 좀비를 제외한 노션 세부 기획 대조, 기존 CSV 전체의 Drive 스프레드시트 정리.
- 출처: https://app.notion.com/p/41caca30f5dc80239e4dd03a4d451a46 및 공간/시간표/이벤트 하위 문서.
- 대상 폴더: https://drive.google.com/drive/folders/116nqm_BANPMT_eHzeiEUlBwtfo_W7OBo. 예시 파일은 하위 SpreadSheet 폴더의 stamina_config.xlsx를 실제 다운로드·검사했다. 1행 설명(예시는 빈 값), 2행 타입, 3행 변수명, 4행부터 데이터인 양식이다.
- 데이터 ID: data.characters, data.events, data.shelter.items, data.game-dev.endings, data.example-items, data.farming.pickups. 기존 CSV 6개와 기존 명세를 재사용한다. data.bunker.rules 및 미구현 기획은 대조 대상이며 이 준비 단계에서 값을 임의 변경하지 않는다.
- 중복 조사: DataRegistry의 active/planned, Assets/ProjectSettings/Docs의 CSV·설정·소비자를 확인. 기존 공용 CSV 5개, 전용 파밍 CSV 1개. ExampleItems는 예제로 명시. 파밍을 공용 DB에 중복 등록하지 않는다. 오디오 이전 없음.
- 로컬 xlsx는 가져오기 준비 산출물이다. 원격 게시/왕복 검증 전에는 기존 CSV가 편집 정본이며 xlsx를 독립적인 추가 정본으로 선언하지 않는다. 연결 성공 시 시트 URL/탭과 갱신 방향을 명세에 확정한다.
- 공용 파서는 예시의 3행 헤더를 지원하도록 확장하고 기존 typed-header/Idler 형식을 유지한다. 잘못된 타입·중복 ID·따옴표/한글 보존을 확인한다.
- 현재 원격 제약: 연결된 Drive 플러그인 없음. 브라우저에서는 Drive/Notion 모두 로그아웃. 공개 읽기는 성공했지만 생성/삭제/노션 체크 반영은 미실행. 사용자에게 연결을 요청했다. 삭제X 설정 폴더와 양식 문서는 보존 대상.
- 기획 충돌: 본문은 기획 50% 초과, 공간 문서는 기획 100% 완료를 아트/프로그래밍 시작 조건으로 제시. 사용자에게 기준 확인 중. 평가 목표 합 96과 기존 3축 100% 사이 환산도 불명확하다.
