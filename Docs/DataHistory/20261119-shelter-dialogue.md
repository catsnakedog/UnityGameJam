# 벙커 대사 도입 · 2026-11-19

`data.shelter.dialogue` v1을 [CSV](../../Assets/Settings/Data/Csv/ShelterDialogue.csv)에 등록했다. 사용자 원문의 중복 없는 좀비 18행과 직군별 생존자 46행을 공용 DB에 적용했다. [필드와 참조](../DataSpecs/ShelterDialogue.md) · [말풍선](../UI/ShelterSpeech.md)

등록 전과 적용 전에 기존 정본 및 planned 목록을 조사했다. 캐릭터 직군은 Characters.role, 허기와 멘탈은 CharacterIdentity와 BunkerRules, 개발 잠금은 BunkerSimulation, 승패·처치는 기존 ZombieAgent 판정을 사용한다. 기존 선택형 이벤트의 대사 결과와는 소비 시점이 달라 별도 테이블로 등록했다. 외부 시트나 별도 런타임 로더는 추가하지 않았다.

반복 가능한 일상 대사는 작업 주기에 각 발화자별 25% 확률로 시도한다. 수면은 수면실에 정착한 첫 틱, 허기는 0 진입, 저멘탈은 기존 피곤 임계치 진입에 발화한다. 침입 대사는 침입 후 의식 있는 가장 가까운 생존자, 처치 대사는 마지막 일격을 가한 생존자가 말한다. 동시에 발생한 전투 대사는 발화자별로 최대 세 문장을 대기시킨다.

Unity 공용 DataImporter에서 이 테이블만 변경된 것을 확인한 뒤 CSV를 적용했고 Addressables 콘텐츠 빌드를 완료했다. Unity Editor에서 월드 공간 프리팹과 벙커 씬 참조를 저장했다. Play 및 독립 실행 파일에서 표시와 배치는 검증하지 않았다.
