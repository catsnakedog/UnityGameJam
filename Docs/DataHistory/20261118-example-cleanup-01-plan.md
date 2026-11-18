# 도구 예제 제거·시트/CSV 양식 대조 계획

- 요청: 예시 데이터 삭제, 스프레드시트와 내부 CSV 양식 통일 확인.
- 대상: `data.example-items`의 example_wood/example_stone. 실제 게임 소비자 없음. ExampleItems 시트, 로컬 CSV, 가져오기 설정, 생성 DB/접근자에서 제거한다. 업로드용 xlsx 사본도 제거한다.
- 의존성: GameDevChecks가 예제 테이블 존재를 강제하므로 실제 가져오기 대상의 테이블명/행 수 일치 검사로 바꾼다. DataImporter 보고서의 예제 전용 문구와 DataManager 조회 예시를 실제 Characters 기준으로 고친다.
- 다른 데이터: Characters/Events/ShelterItems/FarmingData는 실제 연결된 게임 데이터. GameDevEndings 임시 3행은 기존 유지 요청이 있으며 삭제 범위에 포함되는지 사용자에게 확인 중이다. 다른 작업의 data.zombies planned 및 BunkerRules 확장에는 관여하지 않는다.
- 삭제 전 각 대상 경로/시트 ID 확인. Unity AssetDatabase 및 기존 DataImporter의 삭제 허용 절차를 사용하고 GUID/주소는 보존한다. 클라우드는 휴지통 이동, 영구 삭제 없음.
- 검증: 남은 모든 시트의 최신 CSV를 다운로드하여 설명/타입/필드명, 열 순서, 모든 셀을 내부 CSV와 직접 대조한다. UTF-8 BOM을 확인한다. Unity 컴파일, 기존 데이터/엔딩 검사, 콘텐츠 빌드 및 생성 DB의 예제 부재 확인.
- 신규 데이터 ID/CSV/DB/로더 없음. 과거 이력과 테스트 내부 fixture는 실행 데이터가 아니므로 보존한다.
