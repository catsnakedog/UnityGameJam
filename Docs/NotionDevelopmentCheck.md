# 노션 개발 내용 체크 — 2026-11-18

## 후속 반영 — 좀비 침입 (2026-11-18)

아래 원래 시트 작업의 범위 제외 설명 이후, 별도 사용자 요청으로 좀비 전투/침입/경로 조우/방 점거를 구현했다. 캐릭터 Combat과 NEXT_WAVE_HP는 이제 실제 전투·생성 소비에 연결된다. 멘탈 상한100/기절40틱/회복30도 완료. [사용법·데이터 연결](Zombies/README.md), [검증](DataHistory/20261118-zombie-invasion-03-verified.md). 수면/보초의 지속 멘탈 변화와 전체 시간표의 원문 수치 이전은 그대로 별도 범위다.

원문: [개발 요구사항](https://app.notion.com/p/41caca30f5dc80239e4dd03a4d451a46), [공간](https://app.notion.com/p/41caca30f5dc8009aa61e62d155c94bd), [시간표](https://app.notion.com/p/41caca30f5dc8065a8aae3eccbbd09c0), [이벤트](https://app.notion.com/p/41caca30f5dc808a9c2bcda8f0c01844). 좀비/전투 기능 구현·밸런스 변경은 이번 범위에서 제외한다. 캐릭터의 전투 스탯과 기존 이벤트의 NEXT_WAVE_HP 문자열은 원문 보존용으로 유지한다.

코드/데이터/실제 검증을 대조한 보고서다. 노션 본문 하단에 개발 반영 체크를 추가했고 공간 문서의 선행 조건을 50% 초과로 수정했다. 두 페이지 모두 새로고침 후 저장을 확인했다.

## 개발 체크

| 상태 | 요구사항 | 확인한 구현/데이터 | 남은 작업 |
|---|---|---|---|
| 완료 | 12개 사건 내용·Yes/No 효과 | Events.csv, EventCatalog/Executor | 이벤트 간격/평가 대체는 아래 별도 항목 |
| 완료 | 무작위 선택, 같은 이벤트 연속 금지 | EventCatalog.Pick, Play 검증 | 없음 |
| 완료 | 초콜렛 있을 때 ev_party, 빈 방 효과 생략 | 조건/효과 실행기, Play 검증 | 없음 |
| 완료 | 노트북·타이핑·결과 1줄·시간 정지/재개 | EventLaptopPopup/Controller, Play 검증 | 없음 |
| 완료 | 캐릭터 6명 이름·4능력·멘탈 시작 60 | Characters v2, 기존 Person ID 보존, 6명 실제 생성 검사 | 직군/소개 UI는 별도 |
| 데이터 준비 | 물자 3종 멘탈 +10, 획득 이벤트 | ShelterItems v2, Definition API, sourceEvent 검증 | 창고 UI와 캐릭터 드래그 소비 연결 |
| 부분 구현 | 방 배정과 능력별 진척 | ShelterWalker.CurrentRoom, BunkerSimulation.ProcessWork | 노션의 기간별 진척 단위/목표 합 96에 맞춘 환산 미정 |
| 부분 구현 | 시작 시 전원 거실 | 구조 Person만 기존 거실 spawnPoints에 생성 | 노션의 6명 중 4명 제한 미구현. 현재 구조 수만큼 생성 |
| 미반영 | 전체 960틱, 틱 길이 0.9375초 | BunkerRules는 현재 120틱/1초 임시값 | 전체 시간표를 평가·종료와 함께 연결 |
| 미반영 | 40틱 사건, 240/480/720틱에는 평가, 960틱 종료 | 현재 사건은 10틱마다, 총 틱 종료 API 존재 | 평가 3회와 사건 20회 스케줄 구현 |
| 완료 | 멘탈 상한 100, 0이면 40틱 기절, 깨어날 때 30 | BunkerRules → CharacterIdentity, 틱 상태·이동/작업/전투 제한·멘탈 라벨 연결 | 좀비 침입 후속 작업 Play 검증 완료 |
| 미구현 | 수면 +8/40틱, 보초 −5/40틱 | 방 분류는 존재, 지속 멘탈 증감 없음 | 분수 누적과 기절 처리. 보초 전투 기능은 제외 |
| 완료 | 기획 선행 조건 | 사용자 확정대로 50% 초과, BunkerRules/ProcessWork 적용 및 공간 문서 수정 | 0/50/50.1/다시 50 경계 Play 검사 4개 통과. 이동/통과 허용 |
| 미구현 | 기획서 대기/전구 아이콘 | 관련 UI 코드 없음 | 개발 잠금/진척 발생 상태에 연결 |
| 미구현 | 중간 평가 4등급·전원 멘탈 | 아래 원문 기준 확인 | 평가 점수 환산, 데이터 계약, 팝업·정지 구현 |
| 확인 필요 | AED 부활 | 원문 ID/획득처 비어 있음, 기절 해제 문구 모호 | 임의 ID/효과를 추가하지 않음 |
| 부분 구현 | 제출 판정·엔딩 | 종료 API·엔딩 CSV 분기 존재 | GameDevScoreCalculator는 아직 false 반환, 엔딩 점수/이름 임시안 |

## 미연결 원문 기준

아래는 원문 대조용 스냅샷이다. CSV/SO와 병행 편집하는 게임 데이터 정본이 아니다.

- 전체 960틱. 기준 기간 40틱. 틱 길이 0.9375초. 개발 작업 1틱마다.
- 인원 4명, 캐릭터 능력 1~6, 네 능력 합 12.
- 멘탈 0~100, 시작 60. 기절 40틱, 회복 시 30.
- 기획/아트/프로그래밍 방은 기간당 해당 능력만큼 진척, 멘탈 변화 0. 거실은 진척/멘탈 변화 0. 수면은 40틱당 멘탈 +8. 보초는 40틱당 −5.
- 평가 시점 240/480/720틱. 원문의 평가 점수 = 현재 진척 합 / (96 × 현재 틱 / 960).
- 갓겜: 점수 1.2 이상, 멘탈 +20, 금색. 굿: 1.0 이상, +10, 초록. 흠: 0.7 이상, 0, 회색. 망겜: 0.7 미만, −15, 빨강.
- 기존 개발 상태는 각 축 0~100 퍼센트다. 원문 목표 합 96과 각 축 목표량이 명확히 연결되지 않아 평가에 퍼센트 합을 그대로 대입하지 않았다.

## 스프레드시트와 정본

폴더: [공용 csv 파일](https://drive.google.com/drive/folders/116nqm_BANPMT_eHzeiEUlBwtfo_W7OBo) / [관리 시트](https://drive.google.com/drive/folders/15ryAgTAwQFxG0VqVVTKXXKX_OgdpspRd). 예시의 설명/타입/필드명 3행 양식을 재사용했다. 기존 테스트 CSV 22개, 테스트 xlsx 21개, 이번 업로드 중간 xlsx 6개는 휴지통으로 옮겼다. 문서 2개와 `SpreadConfig파일(삭제X)` 폴더는 보존했다. 영구 삭제 없음.

| 데이터 ID | 편집 정본 | 로컬 스냅샷 | 명세 |
|---|---|---|---|
| data.characters | [Characters](https://docs.google.com/spreadsheets/d/1OHkc6LfkRrNK5U2ZG6ENSyFSC5L_QOgUnAex0D5l6X8/edit?gid=897086903#gid=897086903) | `Assets/Settings/Data/Csv/Characters.csv` | [Characters](DataSpecs/Characters.md) |
| data.events | [Events](https://docs.google.com/spreadsheets/d/1OCAvPsKuDhUua0sQuHHT0L1Hr6R24ZBA2fU4QzJ6CMU/edit?gid=1622955486#gid=1622955486) | `Assets/Settings/Data/Csv/Events.csv` | [Events](DataSpecs/Events.md) |
| data.farming.pickups | [FarmingData](https://docs.google.com/spreadsheets/d/1-Lh4oAsNLFpcfi4KGDePAxdBvVdU3pueHlGUxzibsM8/edit?gid=1800384264#gid=1800384264) | `Assets/Settings/Farming/FarmingData.csv` | [FarmingData](DataSpecs/FarmingData.md) |
| data.game-dev.endings | [GameDevEndings](https://docs.google.com/spreadsheets/d/1aqTIvURQ7oMhWfVxXXmFpTbALwQnsVaHUbgAORpmxcM/edit?gid=459802846#gid=459802846) | `Assets/Settings/Data/Csv/GameDevEndings.csv` | [GameDevEndings](DataSpecs/GameDevEndings.md) |
| data.shelter.items | [ShelterItems](https://docs.google.com/spreadsheets/d/158_0ouUqo0t8b6hI9U_r-ilHkHkzPqoQ_tOVGc4wjp4/edit?gid=1351218376#gid=1351218376) | `Assets/Settings/Data/Csv/ShelterItems.csv` | [ShelterItems](DataSpecs/ShelterItems.md) |

시트 → UTF-8 BOM CSV → 기존 DB/Addressables로 갱신한다. 5개 모두 `Tools > Data Manager`의 전체 시트 가져오기 버튼으로 처리한다. 공용 4개는 기존 DB/타입 접근자를, FarmingData는 기존 전용 CSV/DB/FarmingRecord를 갱신한다. 적용 후 콘텐츠를 다시 빌드한다. 런타임 원격 로드나 자동 실시간 동기화는 없다.

동일 의미의 별도 데이터 ID/CSV/DB를 만들지 않았다. 파밍을 공용 DB에 중복 등록하지 않았고 ExampleItems는 사용자 요청으로 시트/CSV/DB/연결 설정에서 폐기했다. 오디오 경로는 그대로다. 엔딩 임시값 및 미구현 기능은 위 표에 표시했다. 최종 검증: [이력](DataHistory/20261118-notion-sheets-06-remote-verified.md).

2026-11-18 예제 제거 후 재검증: 현재 5개 시트·36행, 설명/타입/필드명 3행과 모든 셀이 내부 CSV와 일치한다. [검증 기록](DataHistory/20261118-example-cleanup-02-verified.md).
