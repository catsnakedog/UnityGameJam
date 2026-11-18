# Farming HUD v1

파밍 HUD의 제목과 시작 안내는 로드된 FarmingRules.Duration을 표시한다. 우측 카운트다운은 해당 회차의 남은 시간을 표시한다. 사람·물자 운반은 4칸이며 E로 상호작용한다.

- uGUI + TMP, 사용자 확정 1920×1080(16:9) 기준. CanvasScaler ScaleWithScreenSize + Expand, 내부 `Viewport 1920x1080`은 AspectRatioFitter FitInParent + RectMask2D로 게임 화면과 같은 영역에 맞춘다.
- 16:9보다 넓으면 좌우, 좁으면 상하 검은 여백. 화면을 늘리거나 자르지 않으며 HUD와 버튼은 여백 안으로 나가지 않는다. 창 크기 변경 시에도 같은 구도와 UI 배치를 유지한다.
- 좌상단: `01 / FARMING`, 제목, 이동 안내. 우상단: 남은 초(올림), 제한시간 게이지.
- 좌하단: 점유 칸 4개, 점유/최대 텍스트. 사람과 물자는 서로 다른 Sprite로 표시.
- 우하단: 투입한 사람/물자 개수. 하단 중앙: E 동작과 대상 무게 또는 공간 부족 안내.
- 준비 패널: 시작 전 시간 정지, E 시작. 진행 중: A/D 좌우 이동, E 문/줍기/내려놓기, E 홀드로 연속 행동하지 않음.
- 운반 중 가까운 벙커가 최우선. 그 외 문 이동, 가까운 대상 줍기, 빈 곳에서는 마지막 대상을 던져 내려놓기. 벙커는 운반 중인 대상을 모두 수용.
- 종료 패널: 집계/미투입 운반분 표시. **벙커로 이동** 버튼은 ShelterPrototype의 게임 개발 단계로 연결하며 R은 파밍을 재시작한다. [게임 개발 연결](../GameDev/README.md).
- 로드 실패: 원인을 표시하고 입력 차단. 설정값 임의 대체 없음.
- 프리팹: Assets/Prefabs/Farming/FarmingHud.prefab, CarrySlot.prefab.
- 이미지: Assets/Art/UI/Farming/panel.png(64²), slot_empty/person/item.png(64²), button_normal/hover/pressed.png(64²), gauge.png(64×16). 모두 6px slice.
- PNG는 제작 도구에서 그린 임시 도형 아트. Sprite Single, PPU100, Bilinear, 무압축, Clamp, mipmap off. 같은 경로 PNG 바이트만 교체하고 .meta 유지.
- 패널 및 버튼 배경은 실제 Sprite, 텍스트는 편집 가능한 TMP. 버튼은 SpriteSwap. 동적 슬롯은 저장된 CarrySlot 프리팹 참조로 생성.

검수 결과는 Docs/Farming/Verification.md에 기록한다.
# 사이드뷰 입력 전환 (2026-11-18)

사용자 결정에 따라 파밍 맵은 사이드뷰다. 기존 HUD 프리팹·이미지·앵커·슬롯은 재사용하며 안내 텍스트만 갱신한다. A/D 좌우 이동, E 문 이동/들기/내려놓기/투입. 문 근처에서는 `[ E ] {목적지} 이동`을 표시하고 E를 한 번 소비한다. 우선순위는 운반 중 벙커 투입 → 문 이동 → 대상 들기 → 마지막 대상 내려놓기다. Ready 안내에 입실/복귀를 모두 표시한다. 이동 후 운반 칸/제한시간/물자 상태는 유지한다. 기존 16:9 화면 대응을 유지한다.
