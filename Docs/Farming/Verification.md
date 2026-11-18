# 파밍 검증 · 2026-11-18

Unity 6000.3.28f1 / Addressables 4.2.0. Editor Play에서 실제 로컬 콘텐츠 빌드(`Use Existing Build`)를 로드했습니다. 실행파일 빌드는 수행하지 않았습니다.

- `Verification-edit-passed.txt`: CSV BOM/CRLF/따옴표·쉼표·이스케이프 처리, 중복 ID·타입·무게 범위·비정수·미완성 따옴표·공백 ID 거절, 6명 및 60초/4칸 설정 검증.
- `Verification-play-passed.txt`: 실제 60초 동안 WASD 입력, 테이블·맵 경계 충돌, E 누름당 한 번, 2+2 및 1+2+1 운반, 용량 초과 거절, 사람 수평 운반, 일괄 투입/중복 집계 방지, 던지기·중간 알파·페이드 완료, 바닥 드롭과 재습득, 미투입분 제외, 종료 이벤트 1회/입력 차단, SO 원본 보존 검증.
- 최초 자동 입력 검사는 에디터 백그라운드 키 입력 차단으로 실패했습니다(`Verification-play-failed.txt`). 테스트 동안만 입력 설정을 변경하여 통과했고, 원래 ResetAndDisableNonBackgroundDevices/PointersAndKeyboardsRespectGameViewFocus 설정으로 복원했습니다.
- 이후 운반 자세를 얼굴이 위로 향하도록 개선했습니다. 최종 Play에서 2칸 사람 픽업 재확인, 사람 forward·world up 내적=1, `CarryPreview.png` 시각 검수 완료. 게임 규칙·타이머는 변경하지 않았습니다.
- 재시작 API와 실제 HUD 버튼 연결을 각각 호출해 Ready / 60초 / 사용 0칸 / 투입 0 / available 18을 확인했습니다. 확대 검수 카메라는 씬 재로드/Play 종료로 원상복구했습니다.
- 종료 후 안내를 R 재시작으로 변경하고 실제 텍스트 갱신을 확인했습니다.
- 1600×900, 1440×1080(4:3), 2560×1080에서 캡처 검수. 공용 16:9 구도, 검은 여백, HUD 정렬/잘림 없음 확인. 4:3에서 UI 영역은 (0,135)~(1440,945), 논리 영역 1920×1080.
- UI 패널 PNG를 같은 경로에 다른 색으로 덮어쓴 뒤 캡처로 갱신 확인. GUID/local file ID와 6개 HUD 패널 참조 유지, 원래 PNG 복원 및 재임포트 완료.
- 저장된 씬/프리팹 누락 스크립트·UI Sprite 및 Addressables 주소 중복 검사, 생성기 재실행 시 기존 씬 바이트 보존 확인.
- `Preview.png`: 최종 파밍 기본 화면. `CarryPreview.png`: 운반 자세 확대 검수 화면. 기타 일시 검수 이미지는 `Temp/Farming`.

파밍만 구현한 프로토타입입니다. 3D 아트는 교체 가능한 기본 도형이며, 투입 명단은 현재 세션 메모리에 보관합니다. 후속 벙커/결과 씬 전환과 영구 저장은 포함하지 않았습니다.
