# 데이터 양식 외부 문서 갱신

- 작업: Codex data-reference-document-20261118. 사용자 지정 기존 문서에 현재 데이터 양식 정리.
- 대상: [CSV 데이터 레퍼런스](https://docs.google.com/document/d/1wl9JY-Iiv316ucy_pDvmogp1xr20tYZP/edit). 기존 DOCX 파일 ID와 공유 링크 유지. Google Docs의 Office 편집 화면에서 반영했다.
- 기존 테스트용 타 프로젝트 테이블 설명을 현 프로젝트의 5개 시트 양식으로 갱신했다. 이전 본문과 HTML은 `Temp/NotionSheetSync/reference-before.*`에 백업했다.
- 포함 데이터 ID: `data.characters`, `data.events`, `data.shelter.items`, `data.game-dev.endings`, `data.farming.pickups`. 도구 설명은 `editor.data-tool` 및 기존 `system.game-database`를 참조한다.
- 정본: DataRegistry에 연결된 기존 Google Sheets 5개. 이 문서는 설명이며 신규 데이터 저장소·CSV·SO·로더를 만들지 않았다. 게임 수치와 스키마를 변경하지 않았다.
- 현재 명세: [Characters](../DataSpecs/Characters.md), [Events](../DataSpecs/Events.md), [ShelterItems](../DataSpecs/ShelterItems.md), [GameDevEndings](../DataSpecs/GameDevEndings.md), [FarmingData](../DataSpecs/FarmingData.md), [DataToolSettings](../DataSpecs/DataToolSettings.md).
- 문서 구성: 편집 정본과 원본 링크, 설명/타입/필드명의 3행 헤더, 전체 30개 필드의 순서·타입·의미·단위·필수/빈 값·제약, ID 참조, 이벤트 효과 문법, 임시 엔딩 유지, 시트 가져오기 버튼, CSV/클래스/DB 생성 경로, 변경 절차.
- 중복 조사: 최신 registry의 planned/진행 작업과 해당 명세·실제 CSV·효과 파서를 대조했다. FarmingData 전용 경로는 유지하며 공용 DB에 중복 등록하지 않는다. ExampleItems는 폐기 상태로만 설명한다.
- 작업 중 `ending-popup-20261118`의 최소 점수 비율 단위 변경 계획을 확인했다. 기존 0/100/200을 현행 확정 기준으로 안내하지 않고 최신 평가식과 0/0.3/0.5 전환 진행 상태를 명시했다. 해당 담당의 시트·코드는 수정하지 않았다.
- 검증: 원격 문서의 저장·재열기를 확인하고 DOCX 내보내기의 전체 본문을 작성 내용과 공백 정규화 후 비교했다. 5개 스키마의 30개 필드명·타입·순서 및 5개 원본 시트 링크를 검사했다. 상세 결과는 `Temp/NotionSheetSync/document-verified.json`.
- 화면 검수: 브라우저와 Google의 PDF 내보내기 페이지 이미지를 사용했다. 로컬 render_docx.py는 LibreOffice가 설치되지 않아 실행 불가였으며 Google 자체 렌더 결과로 대체했다. 로컬 PDF/PNG는 검수용으로만 보관하고 전달물은 기존 문서 링크 하나다.
- 이번 작업은 문서 갱신이다. Unity 게임 실행·테스트·콘텐츠 빌드는 다시 수행하지 않았다. 기존 버튼 구현 검증은 [시트 버튼 검증](20261118-sheet-button-02-verified.md)을 참고한다.
