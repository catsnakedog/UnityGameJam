# game-dev-20261118 / planned

- 작성: 2026-11-18 (Asia/Seoul), 프로젝트 UnityGameJam, 담당 Codex.
- 사용자 요청: 벙커 이후 GameDevManager의 기획/프로그래밍/그래픽 진행도, 별도 점수 계산 파일, CSV 기반 총점별 엔딩, 우측 DevGageUI.
- 사전 상태: GameDev/엔딩 데이터·소비자·planned 예약 없음. `system.game-database`는 ExampleItems만 포함. 기존 farming/audio 및 진행 중 파일 변경은 유지.
- 추가 예정 ID: `data.game-dev.endings`; 정본 `Assets/Settings/Data/Csv/GameDevEndings.csv`; 컨테이너/주소는 기존 `GameDatabase.asset` (GUID 62a272592b807b3469b072879c349759), `data/catalog`, `Game Data Local` 재사용.
- 연결: 공용 CSV 임포터 → 기존 DB → GameData → GameDevEndingCatalog → GameDevManager → DevGageUI. 기존 ExampleItems 소비 경로 유지.
- 임시안: 엔딩 3개 및 최소 총점 0/100/200. 이름·문구·점수는 기획 미정이며 검증용. 진행도 합산을 점수 공식으로 확정하지 않음.
- 수명: 현재 진행도/최종 결과는 Manager 씬 상태. SO 원본 수정/새 저장 키/별도 CSV 로더 없음.
- 검증 계획: 진행도 범위·비정상 숫자·완료 스냅샷·중복 종료·초기화/재시작, 엔딩 경계/순서/비활성/오류, CSV 반영/번들 로드, 프리팹 참조·화면 비율·이미지 교체.
- 현재 검증: 레지스트리·명세·소비 코드와 Assets/ProjectSettings/Docs 의미 검색. 엔진 실행 검증은 미실행.
