# 선택형 이벤트 효과 구현 계획

2026-11-18 / Codex event-effects-20261118. 사용자 제공 12개 사건과 Yes/No 효과 원문을 기준으로 구현.

- data.events 기존 CSV v1→v2: id를 int에서 사용자 ev_* string으로 이전. 임시 1~3 폐기, 영구 저장 소비 없음. 제목·내용 유지 필드에 yesEffects:string[], noEffects:string[], requiredItem:string, requiredCount:int 추가. 효과 배열은 공용 도구의 | 구분자 사용. ev_party만 requiredItem=choco, requiredCount=1, 나머지는 빈 ID/0. NONE은 단독 사용.
- 효과 문법 MENTAL(all/random1/room:art/room:plan/room:prog, signed int), PROGRESS(art/plan/prog/open, signed int), ITEM(등록 ID, signed int), NEXT_WAVE_HP(signed int), NONE. 잘못된 문법·대상·ID는 임포트/로드 거부. 코드 실행/eval 없음.
- data.characters: 기존 CurrentMental/SetMental 사용. 멘탈 포인트 0 하한, 상한은 기존 미정 유지. data.game-dev.endings 관련 진행도는 기존 0~100% API 재사용; PROGRESS는 %p. open은 선택 시점 100% 미만 축.
- data.shelter.items 신규 예약. 명세 먼저 작성. 벙커 재고 초기 0, 음수 재고 선택은 전체 효과 적용 전에 거부. 조건은 추첨과 선택 직전 모두 검사. 파밍 물자 ID는 자동 매핑하지 않음.
- 다음 습격 HP는 BunkerSimulation의 런타임 누적 보정 상태. 다음 ZombieInvasionDue 직전 1회 소비하고 현재 습격 보정값에 전달. 기본 적 HP·전투는 기존 구현 없음; 별도 기본값을 발명하지 않는다. 기본 HP를 받는 계산 API로 보정, 최종 HP는 최소 1.
- 같은 틱의 사건→습격 순서: 사건 선택/결과 창을 닫은 뒤 남은 틱 처리를 이어 습격 보정을 소비. 원래 작업은 중복 처리하지 않는다.
- 정시는 기존 이벤트 간격을 게임 시간 경계로 사용하며 환산은 사용자 확인 중. 같은 사건 연속 금지. 조건 통과 후보가 직전 한 건뿐이면 해당 시간은 건너뛰어 연속 금지 우선.
- UI: 타이핑 유지, Yes/No 선택 1회, 실제 적용 결과 한 줄, 잠시 표시 후 자동 닫기와 틱 재개. 효과 적용은 먼저 전체 검증/계산 후 일괄 반영.
- 구현 전 목록 재확인: 동일 물자/효과 정본 없음. 기존 주기·능력·진척·공용 DB 재사용. 원본 이력 보존.
