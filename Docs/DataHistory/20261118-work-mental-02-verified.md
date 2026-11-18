# 벙커 작업 멘탈 적용 및 검증

2026-11-18 KST · UnityGameJam · Codex work-mental. 사용자가 요청한 작업 중 멘탈 소모를 기존 개발 효율과 같은 벙커 규칙 SO에 연결했다. [변경 전 조사](20261118-work-mental-01-planned.md) · [현재 명세](../DataSpecs/BunkerRules.md)

| 데이터 | 정본과 소비 |
|---|---|
| `data.bunker.rules` | `Assets/Settings/Shelter/BunkerRules.asset`, 타입 `BunkerRules`, GUID `01a9f70ed623c5341bec7f05cc07781d`. Addressables 주소 `data/bunker-rules`, 그룹 `Game Data Local`, 라벨 없음. `GameData.LoadSettingsAsync<BunkerRules>`가 로드·캐시하고 `BunkerSimulation.ProcessWork`가 읽는다. |
| `data.characters` | 기존 Characters 시트에서 생성된 능력과 초기 멘탈을 읽는다. 현재 멘탈은 캐릭터별 `CharacterIdentity`가 보유한다. [캐릭터 명세](../DataSpecs/Characters.md) |

기존 `percentPerAbility`는 능력 1당 0.1%p, `workEveryTicks`는 1틱, `secondsPerTick`은 1초다. 새 `mentalCostPerWork`는 일한 캐릭터 한 명당 매 회차에 적용한다. 임시 초깃값 1이며 0은 소모 해제다. 데이터 목록의 예약과 Assets·ProjectSettings·Docs를 재확인했다. 개발 효율과 작업 주기는 기존 필드를 재사용했고, 작업 멘탈 비용의 편집 정본과 주소 등록은 각각 한 곳이다.

## Unity 6000.3.28f1 검증 결과

- Default 프로필로 Addressables 콘텐츠 빌드 성공. 현재 프로젝트의 ShelterPrototype 씬에서 Use Existing Build로 로드한 개발 효율 0.1과 작업 비용 1을 SO와 대조했다.
- 같은 빌드의 한지우(아트 2)를 사용한 틱 재현에서 기획이 50%일 때 멘탈 60·아트 0%를 유지했다. 해금 후 두 번 일하자 멘탈은 59, 58, 아트는 0.2%, 0.4%가 됐다. 수면방에서 다음 작업 틱을 진행한 후에도 58·0.4%였다.
- 설정 복사본을 3틱 간격·능력당 0.5%p·비용 7로 바꾼 검사에서 첫 2틱은 대기했고 세 번째 틱에 아트 +1%p, 멘탈 60→53을 확인했다. 비용 0에서도 진행도가 올랐고, 잔여 멘탈을 넘는 비용은 0에서 기절 40틱을 시작했다.
- [Edit 검사 11개](../Bunker-edit.txt), [Play 검사 31개](../Bunker-play.txt) 통과. Play 검사는 구조 캐릭터 3명이 있는 실제 벙커 씬의 시간을 제어해 이동·작업·동시 작업자·점거·탈진·일시정지·재시작·마지막 틱 제출을 확인했다. 검사에서는 사건 팝업을 비활성화했다.
- 첫 공유 에디터 실행은 데이터 로드 중 취소됐으며 임시 프로젝트는 SO를 읽지 못했다. 최종 Play 결과는 현재 프로젝트에서 공용 데이터 로드 후 실행한 결과다.
