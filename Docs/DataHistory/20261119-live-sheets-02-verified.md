# 실행 시 시트 연동 검증

UnityGameJam · 2026-11-19 00:03 KST · verified · [변경 전 상태와 계획](20261118-live-sheets-01-planned.md)

Data Manager의 `useLiveSheets`를 `editor.data-tool`에 추가했다. 설정 정본은 `ProjectSettings/GameDataTool.asset`이며 현재 값은 false다. Editor Play는 이 설정을 읽고 Player는 빌드할 때 선택한 값을 사용한다. 시트 모드는 시작 시 한 번 다운로드한다. 씬을 이동해도 같은 데이터를 사용한다. [도구 설정 명세](../DataSpecs/DataToolSettings.md)

| 사용한 데이터 ID | 정본 및 연결 |
|---|---|
| system.game-database | 공용 CSV 생성물 `Assets/Settings/Data/GameDatabase.asset` / `data/catalog`. 같은 GameData API가 검증 후 이번 실행의 DB를 제공한다. [컨테이너 명세](../DataSpecs/GameDatabase.md) |
| data.characters, data.events, data.game-dev.endings, data.shelter.items | 각 Google Sheets → 공용 내부 스냅샷 `Assets/Settings/Data/Csv`. 편집 원본과 필드는 [Characters](../DataSpecs/Characters.md), [Events](../DataSpecs/Events.md), [GameDevEndings](../DataSpecs/GameDevEndings.md), [ShelterItems](../DataSpecs/ShelterItems.md)에 있다. |
| data.farming.pickups | FarmingData 시트 → 내부 FarmingData.csv / FarmingDatabase.asset. 파밍과 벙커에서 실행 단위로 같은 DB를 조회한다. [파밍 명세](../DataSpecs/FarmingData.md) |
| data.zombies | 종류/규칙 시트 → `Assets/Settings/Shelter/ZombieData.asset`. 현재 Config에는 종류 별칭 ZombieData가 연결되어 있고 규칙은 내부 데이터를 사용한다. [종류](../DataSpecs/Zombies.md), [규칙](../DataSpecs/ZombieRules.md) |

중복 조사에서 현재 Config의 좀비 종류 연결과 기존 공용 ZombieData 테이블이 같은 종류를 나타냄을 확인했다. 이미 존재하는 공용 접근자와 전투 SO에 같은 다운로드를 제공한다. 새 편집 정본·CSV·콘텐츠 ID·Addressables 주소는 생성하지 않았다. 실행 모드 선택은 내부 CSV나 SO에 시트 내용을 쓰지 않는다.

## 실제 결과

- Unity 6000.3.28f1에서 공용 데이터 EditMode 회귀 테스트 57/57 통과. Unity에서 실제 Config와 활성 6개 시트를 내려받고 메모리 DB를 생성했다. 최신 Events는 38행·9열이고 같은 시점의 내부 DB는 12행이었다.
- 검증 도중 원격 Events에 yesResult/noResult가 추가됐다. 기존 이벤트 소비자가 요구하는 ID와 7개 필드는 이름·타입으로 검증하고, 추가 열도 공용 DataRow에 제공한다. 최신 ev_crunch의 yesResult 조회를 Windows Player에서 확인했다.
- 격리된 프로젝트 복사본에서 내부 CSV에 구분용 값을 넣고 Addressables 콘텐츠와 Windows Player를 두 모드로 각각 빌드·실행했다. 파밍 씬에 생성된 실제 픽업의 레코드와 파밍 DB의 레코드가 같았고, 벙커 씬으로 이동한 뒤에도 같은 캐릭터 행과 좀비 SO를 사용했다.

| Windows 실행 모드 | person_01 표시명 | 실제 픽업 무게 | 첫 웨이브 좀비 HP | 이벤트 수 |
|---|---|---:|---:|---:|
| 내부 CSV (검증용 값) | LOCAL_QA | 3 | 8 | 12 |
| 스프레드시트 | 한지우 | 2 | 2 | 38 |

- 잘못된 활성 URL로 실행한 검증용 Player는 SpreadConfig 오류를 보고했고 GameData.IsReady는 false였다. 별도 Unity 검증에서 종류/규칙 쌍의 규칙 누락을 거부했고, 공용·파밍·좀비 메모리 값을 바꿔도 원본 에셋 값이 보존됐다.
- 검증용 CSV·빌드는 작업 프로젝트 밖의 임시 복사본에서 만들었다. 외부 시트 계정으로 쓰기 작업을 수행하지 않았다. Player 실행 검증 플랫폼은 Windows x64다.
