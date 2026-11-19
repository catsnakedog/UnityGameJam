# 벙커 대사 시트

## 식별과 범위

`data.shelter.dialogue` v1, 2026-11-19, Codex dialogue-20261119. 생존자와 좀비가 상황에 따라 말하는 한국어 후보를 관리한다. 직군과 전투·작업·멘탈 판정은 각각 기존 캐릭터와 벙커 규칙이 소유한다.

## 정본과 연결

편집 정본은 `Assets/Settings/Data/Csv/ShelterDialogue.csv`다. Excel에서 열 수 있는 UTF-8 BOM CSV로 공용 Data Manager의 설명·타입·변수명 3행 형식을 쓴다. 외부 시트 연결은 없다. 공용 임포터가 기존 GameDatabase.asset과 DataTables.cs를 갱신한다. 공용 주소는 `data/catalog`이며 별도 주소나 로더는 없다. 벙커가 초기화된 뒤 ShelterSpeech가 GameData.GetTable("ShelterDialogue")에서 후보를 읽는다. 말풍선 인스턴스와 대기열은 벙커 회차 안에서만 유지된다.

## 필드 계약

모든 열은 단위 없는 필수 문자열이며 빈 값이나 기본값을 사용하지 않는다.

| 열 | 의미와 제약 |
|---|---|
| id | 테이블 내 고유하고 안정적인 행 ID. 역할·상황·일련번호로 발급한다. 공용 CSV 임포터가 중복 키를 거부한다. |
| role | 생존자는 Characters.role의 기획·개발·아트, 좀비는 좀비. |
| situation | 발화 시점 키. 아래 목록에서 고른다. 같은 역할·상황에 여러 행을 둘 수 있다. |
| line | 말풍선에 표시할 한국어 원문 한 개. |

좀비 상황은 순서대로 접근·철문·캔 피격·침입·근접 피격·처치에 해당하는 approach, knock, can_hit, invade, melee_hit, death다. 생존자는 작업 work, 기획 대기 wait_plan, 보초 guard, 캔 투척과 결과 can_throw/can_hit/can_miss, 침입 invasion, 근접 승패 melee_win/melee_loss, 처치 kill, 저멘탈 low_mental, 허기 hungry, 수면 sleep, 기절 faint를 사용한다.

## 변경·삭제와 중복 조사

행 순서는 자유이며 한 행을 삭제하면 후보 하나가 제거된다. 후보가 없는 조합은 발화하지 않는다. 저장 데이터에서 대사 ID를 참조하지 않는다. Assets, ProjectSettings, Docs와 등록 목록의 예약 항목에서 dialogue·말풍선·대사와 원문을 검색했다. Characters.description은 인물 소개이고 Events는 선택지·효과이므로 양쪽 모두 상황별 대사와 중복되지 않는다. 직군은 Characters.role을 참조한다. 실제 적용 범위는 [이력](../DataHistory/20261119-shelter-dialogue.md)에 기록한다.
추가 상황 키는 ending_bad(좀비 불평), ending_satisfied(만족한 좀비), ending_joy(개발자 축하)다. 전용 Ending 씬은 해당 후보를 ID 순서로 재생한다. Normal 연출은 기존 invade 행을 사용한다. 기쁜 대사는 같은 직군의 개발자에게 연결한다.

## 주최 측 튜토리얼 방송

기존 role에 `주최측`을 추가한다. 이 역할의 `tutorial_XX` 행 ID는 두 자리 재생 순서다. situation은 tutorial_start(파밍 시작 구도), tutorial_overview(복도 전체), tutorial_entrance(셔터 확대), tutorial_bag(열린 가방), tutorial_clock(시계) 중 하나다. TutorialBriefing이 공용 GameData에서 이 역할의 행만 ID 오름차순으로 읽어 클릭할 때마다 다음 문장을 표시한다. 씬에서 나가면 읽기 위치를 버린다. 순서 변경은 행 ID를 함께 조정하며 저장 키와 외부 참조는 없다. CSV 로컬 정본과 기존 DB 생성 절차를 그대로 사용한다. 등록 목록·Assets·ProjectSettings·Docs에서 튜토리얼, speaker, 원문의 첫 문장과 주최측을 검색했다. Events의 라디오 선택지는 별개이며 튜토리얼 문장이나 순서와 겹치는 원본은 발견되지 않았다.
