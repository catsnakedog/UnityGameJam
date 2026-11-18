# 좀비 시트 게시·연결 완료 — 20261118-zombie-sheets-03

- 프로젝트 UnityGameJam, 2026-11-18 Asia/Seoul, verified / Codex zombie-sheets-20261118.
- 사용자 요청: 노션 기준 좀비 데이터와 초기값을 Spread에 추가. 후속으로 브라우저 업로드 명시 승인. [계획](20261118-zombie-sheets-01-planned.md), [로컬 검증](20261118-zombie-sheets-02-local-verified.md).

## 정본과 연결

| 데이터 ID | 편집 정본 | 생성물 / 명세 |
|---|---|---|
| data.zombies (종류) | [Zombies](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=1826540193#gid=1826540193) | Assets/Settings/Shelter/Zombies.csv, [명세](../DataSpecs/Zombies.md) |
| data.zombies (공통 규칙) | [ZombieRules](https://docs.google.com/spreadsheets/d/1mk2k50Kk5fqtwSMTrCEui_cuhgOGMbh1aIX9e0zETl8/edit?gid=551346486#gid=551346486) | Assets/Settings/Shelter/ZombieRules.csv, [명세](../DataSpecs/ZombieRules.md) |
| editor.data-tool | [SpreadConfig](https://docs.google.com/spreadsheets/d/1oivFPajqTe1rILgtI4Xe1l6kPKtfSmG4UMat7keT84A/edit?gid=961703615) | 기존 ProjectSettings/GameDataTool.asset, [명세](../DataSpecs/DataToolSettings.md) |

- 두 좀비 탭은 [기존 관리 시트 폴더](https://drive.google.com/drive/folders/15ryAgTAwQFxG0VqVVTKXXKX_OgdpspRd)의 **ZombieData** Google Sheets 문서 하나에 있다. 네이티브 전환 후 직접 화면 검수했고 중간 업로드 xlsx만 휴지통으로 옮겼다. 기존 파일은 유지했다.
- 같은 `ZombieData` SO `Assets/Settings/Shelter/ZombieData.asset`, GUID `6845f853ae90e0b479d0f51cb5d53023`, 주소 `data/zombies`, 그룹 `Game Data Local`, 라벨 없음. Inspector는 읽기 전용 생성물로 전환했다. 같은 의미의 독립 정본을 추가하지 않았다.
- 데이터 흐름: Google Sheets → SpreadConfig → ProjectSpreadsheetSync → 기존 CsvTableReader + ZombieData.Validate → 같은 ZombieData SO → Addressables. `GameData.LoadSettingsAsync<ZombieData>`의 앱 수명 캐시/핸들 및 `ZombieInvasionController`/`ZombieAgent`/`ZombieCombat` 소비 API 유지.

## 초기값과 검증

| ID | 표시명 | 시작 웨이브 | 전투력 | HP |
|---|---|---:|---:|---:|
| zombie_1 | 좀비 | 1 | 1 | 2 |
| zombie_2 | 덩치 좀비 | 4 | 2 | 4 |
| zombie_3 | 성난 좀비 | 7 | 3 | 5 |
| zombie_4 | 거대 좀비 | 10 | 4 | 6 |

- 주사위6 / 판정5틱 / 시작 방어 인원당3회 / 승리당 HP−1 / 패배당 멘탈−2 / 처치 회복+5. 이동 임시값 2, 사다리1.6, 접촉0.65, 생성거리1.2, 간격2.4. 노션 전투·전역 상수와 현재 SO를 대조했으며 밸런스 변경 없음. 위 숫자는 조회용 스냅샷이다.
- 기존 Config 5행의 모든 셀을 보존하고 A7:H8에 두 연결만 추가했다. 활성 총7개: 공용4 + 파밍1 + 좀비2. 문서ID/gid/파일명/활성 상태 대조 통과.
- 인증 없는 실제 CSV 내보내기로 두 탭의 설명/타입/필드명/모든 데이터 셀을 검증했다. 숫자 표시 형식 차이(2 → 2.00)는 타입으로 대조했다. 전체 가져오기에서 UTF-8 BOM CSV를 저장하고 이전 SO 전체 JSON과 동일함을 확인했다. [실제 가져오기 결과](../Zombies/Verification-sheets-remote.txt).
- 실제 전체 7개 시트 가져오기 성공. 공용 GameDatabase는 기존4개 테이블만 유지하며 파밍/좀비 중복 테이블 없음. 기존 5개 CSV/공용 DB/파밍 DB/생성 코드에 Git 변경 없음.
- [Edit 36개](../Zombies/Verification-sheets-edit.txt) 통과. 유효/오류/삭제/스키마/Config 쌍/값 반영/원본 보존을 확인했다. 실패 시 원본을 바꾸지 않으며 일괄 적용 실패 시 복구 코드가 있다. 실제 디스크 쓰기 실패를 강제로 주입하는 검사는 수행하지 않았다.
- [Play 17개](../Zombies/Verification-sheets-play.txt) 통과. **Use Existing Build (requires built groups)** 모드에서 실제 로드, 동일 캐시, 전체 값 일치, 웨이브1~12, 재시작 및 SO 원본 보존 확인. 원격 데이터가 같은 값임을 후속 전체 대조했고 불필요한 Play 반복은 하지 않았다.
- 원격 적용 후 Addressables 콘텐츠 빌드 성공. 최종 Unity 재컴파일 오류0/경고0. Player 실행 파일 빌드와 장시간 난이도 검증은 하지 않았다.

## 중복·이전 결과

적용 직전/완료 시 DataRegistry 최신 active/planned와 Assets/ProjectSettings/Docs의 좀비 ID·웨이브·HP·전투·간격 의미를 확인했다. 기존 `data.zombies`를 이전했으며 새 게임 데이터 ID/런타임 로더/주소 없음. 설치 코드의 최초 하드코딩 seed를 CSV 생성으로 교체했다. 기존 SO는 같은 GUID로 유지되는 생성물이고 CSV/xlsx는 시트 파생물이다. 캐릭터 능력·벙커 습격 주기·이벤트 HP 보정·오디오 데이터는 이전하거나 복제하지 않았다. 기존 사용자/다른 작업의 변경을 보존했다.
