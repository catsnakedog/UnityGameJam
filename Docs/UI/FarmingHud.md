# Farming HUD v1

사용자 요청: 파밍 60초, 사람/물자 운반 4칸, E 상호작용. UI 배치/문구/아트는 프로토타입 제안.

- uGUI + TMP, 사용자 확정 1920×1080(16:9) 기준. CanvasScaler ScaleWithScreenSize + Expand, 내부 `Viewport 1920x1080`은 AspectRatioFitter FitInParent + RectMask2D로 게임 화면과 같은 영역에 맞춘다.
- 16:9보다 넓으면 좌우, 좁으면 상하 검은 여백. 화면을 늘리거나 자르지 않으며 HUD와 버튼은 여백 안으로 나가지 않는다. 창 크기 변경 시에도 같은 구도와 UI 배치를 유지한다.
- 좌상단: `01 / FARMING`, 제목, 이동 안내. 우상단: 남은 초(올림), 제한시간 게이지.
- 좌하단: 점유 칸 4개, 점유/최대 텍스트. 사람과 물자는 서로 다른 Sprite로 표시.
- 우하단: 투입한 사람/물자 개수. 하단 중앙: E 동작과 대상 무게 또는 공간 부족 안내.
- 준비 패널: 시작 전 시간 정지, E 시작. 진행 중: WASD 화면 기준 이동, E 줍기/던지기, E 홀드로 연속 행동하지 않음.
- 가까운 벙커가 최우선. 그 외 가까운 대상 줍기, 빈 곳에서는 마지막 대상을 던져 내려놓기. 벙커는 운반 중인 대상을 모두 수용.
- 종료 패널: 집계/미투입 운반분 표시. **벙커로 이동** 버튼은 ShelterPrototype의 게임 개발 단계로 연결하며 R은 파밍을 재시작한다. [게임 개발 연결](../GameDev/README.md).
- 로드 실패: 원인을 표시하고 입력 차단. 설정값 임의 대체 없음.
- 프리팹: Assets/Prefabs/Farming/FarmingHud.prefab, CarrySlot.prefab.
- 이미지: Assets/Art/UI/Farming/panel.png(64²), slot_empty/person/item.png(64²), button_normal/hover/pressed.png(64²), gauge.png(64×16). 모두 6px slice.
- PNG는 제작 도구에서 그린 임시 도형 아트. Sprite Single, PPU100, Bilinear, 무압축, Clamp, mipmap off. 같은 경로 PNG 바이트만 교체하고 .meta 유지.
- 패널 및 버튼 배경은 실제 Sprite, 텍스트는 편집 가능한 TMP. 버튼은 SpriteSwap. 동적 슬롯은 저장된 CarrySlot 프리팹 참조로 생성.

검수 결과는 Docs/Farming/Verification.md에 기록한다.
