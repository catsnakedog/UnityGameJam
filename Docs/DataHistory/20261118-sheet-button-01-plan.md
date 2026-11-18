# Google Sheets 단일 관리·Editor 일괄 가져오기 계획

사용자 요청: Drive에는 Google Sheets만 관리하고 Unity Editor 버튼으로 내려받기 → 내부 CSV 저장 → 클래스 파싱/DB 갱신을 수행한다.

- `editor.data-tool` 기존 ProjectSettings에 `farmingSheetUrl:string`을 추가한다. 공용 sheets 4개는 유지, 파밍 URL 하나만 별도 저장한다. 기본 빈 값, 전체 가져오기 전 필수 HTTPS Google Sheets URL/gid, 단위 없음. 파밍을 공용 CSV 폴더나 공용 DB에 등록하지 않는다.
- 기존 DataManagerWindow의 주 작업을 전체 시트 가져오기로 변경. `Assets/Editor/ProjectSpreadsheetSync.cs`에서 프로젝트의 캐릭터/이벤트/물자/엔딩/파밍 검증기를 연결한다. 기존 asmdef 경계는 유지하며 창에는 비동기 작업 콜백만 등록한다.
- 기존 SpreadsheetImporter의 다운로드를 재사용한다. 새 CSV 파서/런타임 로더/DB 없음. 파밍은 기존 FarmingCsvImporter 및 FarmingRecord 클래스로 파싱한다. 공용 데이터는 기존 DataTables.cs 접근자와 GameDatabase를 갱신한다.
- 전체 다운로드/형식·참조·삭제 정책 검증이 완료되기 전에는 쓰지 않는다. 다운로드 도중 변경한 로컬 파일/DB는 적용을 거부한다. 파밍 적용 후 공용 적용이 실패하면 파밍 파일/DB도 복원한다. 공용 적용 자체의 롤백은 기존 도구를 사용한다.
- 누락 행/열/테이블을 정상 원클릭으로 자동 삭제하지 않는다. 기존 고급 미리보기의 명시적 삭제 절차를 유지한다. 다운로드 취소/중복 실행/Play 중 적용을 막는다.
- 정본: 기존 5개 시트. 내부 CSV는 UTF-8 BOM 생성 스냅샷. Addressables 콘텐츠 빌드는 별도 버튼으로 유지하고 성공 메시지에서 안내한다. Drive에 CSV를 다시 업로드하지 않는다.
- 조사: DataRegistry planned 포함 확인. 다른 작업의 파밍 맵/좀비 룰은 이 작업 범위 밖. 새 데이터 ID/게임값/정본 복제 없음.
- 검증 계획: 실제 5개 시트 일괄 가져오기, 클래스·DB·CSV 일치, 잘못된 참조/파밍 값/삭제 거부, 취소·실패 시 무변경, 기존 NUnit 및 Editor 검사, 콘텐츠 빌드.
