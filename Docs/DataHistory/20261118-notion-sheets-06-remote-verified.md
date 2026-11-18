# 노션·Google Sheets 연결 검증 완료 — 2026-11-18

## 사용/변경한 데이터와 정본

- `data.characters`: [Characters](https://docs.google.com/spreadsheets/d/1OHkc6LfkRrNK5U2ZG6ENSyFSC5L_QOgUnAex0D5l6X8/edit?gid=897086903#gid=897086903) → `Assets/Settings/Data/Csv/Characters.csv`. [명세](../DataSpecs/Characters.md).
- `data.events`: [Events](https://docs.google.com/spreadsheets/d/1OCAvPsKuDhUua0sQuHHT0L1Hr6R24ZBA2fU4QzJ6CMU/edit?gid=1622955486#gid=1622955486) → `Assets/Settings/Data/Csv/Events.csv`. [명세](../DataSpecs/Events.md).
- `data.example-items`: [ExampleItems](https://docs.google.com/spreadsheets/d/12naSgbJf2enFqYVX6VWJKxfTLTbE5K97FIAKn6UZ67E/edit?gid=1216243563#gid=1216243563) → `Assets/Settings/Data/Csv/ExampleItems.csv`. [명세](../DataSpecs/ExampleItems.md).
- `data.farming.pickups`: [FarmingData](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264) → `Assets/Settings/Farming/FarmingData.csv`. [명세](../DataSpecs/FarmingData.md).
- `data.game-dev.endings`: [GameDevEndings](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846) → `Assets/Settings/Data/Csv/GameDevEndings.csv`. [명세](../DataSpecs/GameDevEndings.md).
- `data.shelter.items`: [ShelterItems](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376) → `Assets/Settings/Data/Csv/ShelterItems.csv`. [명세](../DataSpecs/ShelterItems.md).
- `editor.data-tool`: 기존 `ProjectSettings/GameDataTool.asset`에 공용 5개 URL/탭과 enabled=true를 등록. [명세](../DataSpecs/DataToolSettings.md).
- `system.game-database`: 기존 `Assets/Settings/Data/GameDatabase.asset`/DataTables.cs 재생성 경로 유지. [명세](../DataSpecs/GameDatabase.md).
- `data.bunker.rules`: 기존 `Assets/Settings/Shelter/BunkerRules.asset`의 planningUnlockPercent=50. [명세](../DataSpecs/BunkerRules.md). 총 틱 등 다른 임시 밸런스는 유지.

## 검증 결과

- 6개 Google Sheets 실제 CSV 내보내기를 인증 없는 다운로드로 확인. 총 38행(캐릭터 6, 사건 12, 예제 2, 파밍 12, 엔딩 3, 물자 3)의 모든 헤더·필드·셀 값 일치. bool 대소문자와 숫자 표시 차이는 타입 기준으로 비교.
- 공용 5개는 Unity CsvTableReader로 기존 CSV와 타입·ID·배열·값 전부 대조. 기존 SpreadsheetImporter.DownloadAsync/Apply 실행 성공, 삭제 없음. 캐릭터/이벤트 의미 검증 성공. 설정 저장 파일 재확인.
- 파밍 12개는 기존 전용 파서로 ID·종류·무게 비교 후 기존 경로/DB에 적용. 공용 폴더 복사/등록 없음.
- 모든 로컬 CSV UTF-8 BOM과 3행 양식 확인. Addressables 콘텐츠 빌드 성공. Player 실행 파일은 빌드하지 않음.
- 기존 로컬 작업 검증: CSV 파서 NUnit 36/36, 이벤트 Play 67개, 캐릭터 Play 9개 통과. 기획 잠금 Play는 0 및 50 잠금, 50.1 해제, 다시 50 잠금/이동 허용 4개 통과. 원격 적용 후 콘텐츠 값은 위 대조로 동일성을 검증했으며 Play 전체를 불필요하게 반복하지 않음.
- 로컬 xlsx 6개는 artifact-tool로 생성/렌더하고 셀별 검증. Google Sheets 변환 후 이벤트 시트 화면에서도 한글·3행 고정·서식을 확인.

## 원격 정리 및 노션

- 제공 폴더에 Google Sheets 6개를 생성. 테스트 CSV 22개, 테스트 xlsx 21개, 업로드 중간 xlsx 6개를 정확한 파일 ID/선택 목록 확인 후 휴지통으로 이동. 영구 삭제하지 않음. 문서 2개와 SpreadConfig파일(삭제X) 폴더 보존.
- 노션 개발 요구사항 하단에 완료/데이터 준비/남은 작업/확인 필요·관리 폴더·검증 결과를 추가. 공간 문서를 사용자 확정인 기획 50% 초과로 수정. 양쪽 페이지 새로고침 후 내용이 저장됨을 확인.
- 새 데이터 ID/CSV/DB/로더를 만들지 않음. Assets/ProjectSettings/Docs의 전체 CSV 6종 및 registry planned 항목 재확인. 동일 의미의 편집 정본은 각 시트 하나이며 로컬 CSV/생성 DB/xlsx는 파생물. 오디오 데이터 이전 없음.

## 실제 범위와 남은 작업

좀비 기능 구현 제외. 960틱/40틱 사건/중간평가 스케줄, 멘탈 상한·기절·방별 증감, 인원 4명 제한, 상태 아이콘, 물자 드래그 소비 UI, 제출 점수 환산은 미구현/미연결이다. AED ID/획득처/부활 규칙과 목표 합 96의 기존 퍼센트 환산은 확정이 필요하다. 엔딩은 임시안이다. 노션 데이터 전체의 런타임 구현 완료로 표시하지 않았다. 시트 관리는 단방향 미리보기/적용이며 자동 실시간 동기화가 아니다.
