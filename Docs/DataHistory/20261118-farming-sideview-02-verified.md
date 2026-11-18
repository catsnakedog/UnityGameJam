# 파밍 사이드뷰 전환 적용·검증

- 기록 ID: `20261118-farming-sideview-02-verified`
- 작성: 2026-11-18, Asia/Seoul. 담당: Codex farming-sideview-20261118.
- 프로젝트: UnityGameJam, Unity 6000.3.28f1.
- 이전: [계획](20261118-farming-sideview-01-planned.md). 사용자 요청에 따라 아이소메트릭을 폐기하고 사이드뷰와 E 문 이동으로 전환했다.

## 결과

기존 `Assets/Scenes/Farming.unity`를 정면 2D 복도와 325/324/323호로 전환했다. A/D 수평 이동, E로 각 문 왕복, 문 이동 시 운반·타이머·파밍 상태 유지. 문은 Scene에 저장된 양방향 Target/Arrival 참조를 사용한다. 배경 테이블 앞의 수평 이동선을 사용해 가구와 겹쳐 이동이 막히지 않으며 끝점에서 큰 이동 간격도 제한한다.

사람 6명·물자 12개·4칸 운반·60초·벙커 투입 페이드·후속 벙커 연결은 기존 Session/Inventory/Transfer를 사용한다. 월드 PNG 30개를 동일 PPU64/Point/무압축으로 제작하고 기존 파밍 프리팹 GUID를 유지해 연결했다. [맵·아트·편집 계약](../Farming/Map2D.md), [HUD](../UI/FarmingHud.md).

## 재사용 데이터와 소비

| 데이터 ID | 정본 / 생성물 | 명세 | 연결 |
|---|---|---|---|
| `data.farming.pickups` | 기존 FarmingData Google Sheet → `Assets/Settings/Farming/FarmingData.csv` → `FarmingDatabase.asset` | [FarmingData](../DataSpecs/FarmingData.md) | GUID `117bb726d48c1a0439f32806bea7df2d`, `data/farming/database` |
| `data.farming.rules` | `Assets/Settings/Farming/FarmingRules.asset` | [FarmingRules](../DataSpecs/FarmingRules.md) | GUID `22168f09c24cbfb49aeb55b136dd0298`, `data/farming/rules` |

두 에셋 모두 `Farming Local` 그룹, `farming` 라벨. `FarmingDataLoader.LoadAsync()`의 같은 씬 수명을 사용하며 Session 종료 시 해제한다. 실제 [전체 값·주소·그룹 스냅샷](../Farming/SideViewDataSnapshot.json)을 Editor에서 수집했다. `Use Existing Build (requires built groups)` 모드로 기존 빌드된 Addressables 콘텐츠를 로드했다. 이번 변경은 데이터 값을 바꾸지 않아 콘텐츠를 재빌드하지 않았다.

수치·스키마·ID·주소 변경 없음. moveSpeed=5.8은 수평 이동, interactionRadius=2는 같은 공간의 물체 및 문 탐색에 사용한다. roomRevealDuration=0.5는 입실 공간의 노출, mapHalfExtents.x=11은 호실 기본 너비 설계에 사용한다. y=8은 구 2축 설계 호환용으로 보존하며 현재 사이드뷰에서 소비하지 않는다. 씬 끝점과 문 도착점은 배치 정본이며 별도 맵 CSV/SO를 만들지 않았다.

중복 조사: DataRegistry의 planned 항목과 Assets/Scripts/Farming, Assets/Editor/Farming, Assets/Settings/Farming, ProjectSettings, Docs를 확인했다. 기존 무게/용량/시간/속도/페이드 정본을 재사용하며 새 데이터 ID·CSV·DB·로더 및 공용 DB 이중 등록 없음. 캐릭터·벙커·좀비 데이터의 정본은 변경하지 않았다.

## 실제 검증

- [92개 Play 검증](../Farming/Verification-SideView.txt) 통과. 복도 시작, 문 3쌍과 도착점, 문 접촉만으로 전환되지 않음, E 입퇴실, 공간 분리, 수평 입력/끝점/잘못된 위치 복구, 운반 중 실제 키보드 이동, 모든 테이블 앞 통행, 운반 상태 유지, 2+3 초과 거부/2+2 및 4칸 사람/1+1+2 물자, 벙커 투입 페이드, 던지는 중 다른 방 이동, 재입실 상태 유지, 실제 60초 종료와 이동·상호작용·문 입력 차단, 데이터 불변을 확인했다.
- 첫 검사에서는 대기 중 추가 조작으로 예상 인벤토리 상태가 달라졌다. 이동 차단 자체는 별도 확인했으며, 검증기의 마지막 대기 구간만 일반 입력에서 분리해 전체 92개를 다시 통과했다. 테스트 입력·플레이어 활성 상태는 종료/중단 시 복구한다.
- [아트 규격](../Farming/SideViewArtManifest.json): 30개 모두 PPU64, Point, mipmap off, 무압축. [PNG 교체 검사](../Farming/Verification-SideViewArt.json): 문 이미지 픽셀 변경 후 GUID/local ID/Prefab Sprite 참조 유지 확인, 원본 PNG 바이트와 참조 복구 확인.
- 1600×900 실제 카메라 캡처를 검수했다. [복도](../../outputs/Farming/sideview-corridor.png), [325호](../../outputs/Farming/sideview-room.png), [운반 장면](../../outputs/Farming/sideview-carry-check.png).
- 재시작 후 Ready/60초/복도/빈 인벤토리/투입량 0/18개 모두 Available/양방향 문/Player·Session 활성/테스트 키보드 0 확인. 씬의 Missing Script 0.
- FarmingRules / FarmingDatabase / CSV 파일의 전후 SHA256이 동일하다: `D294F4F9F6E045ACCA20287299CF2EC8A210B5102CE5EFA7E08A1A64A05EA9EB`, `E3DC1FC412E76FFB9F3B10B6D3A6511448626232472FAC4CE3B6CF9FDD7CD846`, `78FE6B2252B13F5BAB20D2CB8F73ED9E0D6C9591DD4875CBB9A880AA9F2A5C54`.
- Editor Play 검증 완료. 실행 파일 빌드는 요청 범위가 아니므로 만들지 않았다. 후속 벙커 씬 자체의 기능은 이번 변경 범위 밖이며 기존 연결 코드를 유지했다.
