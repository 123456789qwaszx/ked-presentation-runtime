# 무대 틀 — 무대 루트 바깥 (2026-11-09)

무대 리그([stage-rig.md](../../StageRig/Documentation~/stage-rig.md))는 **무대 안의 세상**을 어떤 구도로 볼지 맡는다 — 카메라 · 깊이 층 · 마스크.
무대 틀은 **그 무대를 화면 위에 어떤 자세로 띄울지** 맡는다 — 창(컷인의 팝 · 회전).
컷인 판 그림과 그림 팝업은 틀이 아니라 무대 안의 리그다(2026-11-17, [docs/PLAN_stage-visibility.md](../../../../../docs/PLAN_stage-visibility.md)) — 보이기 · 숨기기는 마스크 하나가 정한다.

| | 무대 리그 | 무대 틀 |
|---|---|---|
| 노드 | `Stage_Root` 아래 | `Stage_Root` 바깥 |
| 좌표 계산 | 측정 체인 · 샷 식이 쓴다 | 아무도 재지 않는다 |
| 폴드 | 리그 노드 트리 · 샷 | 끝 상태 표(`CutIns`) |
| 다시 세우기 | 어긋나면 부수고 다시 | 어긋난 자식만 치우고 다시 묶는다 |

의존은 한 방향이다. **틀은 리그를 알고**(무대 루트를 감싸고, 컷인이 마스크에 모양을 쓴다), **리그는 틀을 모른다**.
조립은 리그를 먼저 세우고 무대마다 틀을 세운다(`StageFrameRegistry.Build(StageRigRefs)`).

## 1. 계층

```
(mount)
  {stage}_Window              무대 전체를 들고 팝 · 회전한다. 무대 루트와 같은 rect. 피벗 = 판 아래 가운데(컷인을 열 때). 중립 항등
    {stage}_Stage_Root        (무대 리그) — 컷인 판(far 층) · 팝업 그림(close 층)은 이 안의 리그다(§2 · §3)
```

- 세우기는 `StageFrameBuilder`(`EnsureWindow` · `EnsureCutIn`), 꺼내기는 `StageFrameRegistry`(`CutIn` · `Reset` · `ResetAll`), 커맨드를 만드는 것은 `StageFrameCommandFactory`.
- 틀에는 스키마 · 참조 묶음을 두지 않는다. 그것은 바깥(대본 · 연출 에디터 · 폴드)이 노드를 **이름으로** 부르고 여러 시스템이 같은 노드에 쓸 때 값을 한다(캐릭터 리그 · 무대 리그).
  틀의 노드는 각 슬롯이 혼자 쓰고 폴드는 끝 상태 표만 안다 — **슬롯이 단위**다. 노드 이름은 씬에 저장된 것을 다시 찾기 위한 빌더 안의 사정이다.
- 모든 무대에 선다. 컷인을 쓰지 않는 무대에서 창은 항등이다 — 화면은 틀이 없을 때와 같다.
- 옛 씬(무대 루트가 mount 바로 아래)은 그 자리에 창을 끼워 넣는다 — 무대 계층을 부수지 않고 무대끼리의 그리기 순서를 지킨다.
- 창이 **무대 루트 바깥**인 것이 요점이다. 측정 체인 · 샷 식은 무대 루트에서 멈추므로 창이 팝 · 회전하는 도중에도 `place` · `size`가 같다.

## 2. 컷인 — 창 (`CutIn/`)

`<<cutin_open 무대 판 x y [배율]>>`은 덮는 무대 하나를 판 모양으로 띄운다 — 창 피벗을 판 아래 가운데로, 마스크를 네 점(판 아래 변으로 자르고 위는 열림)으로, 그리고 창에 팝 인 모션을 건다. `<<cutin_close 무대>>`는 팝 아웃이 끝나면 창을 항등으로 돌리고 그 무대의 마스크를 숨김으로 둔다(닫힌 동안은 그 무대의 내용도 안 보인다). 창은 무대를 숨기지 않는다 — 보이기 · 숨기기는 마스크 하나가 정한다([docs/PLAN_stage-visibility.md](../../../../../docs/PLAN_stage-visibility.md)).

- 창 모션은 `StageCutInSlot`이 하나만 소유한다(stage_mask의 `StageMaskSlot`과 같은 규율). 커맨드는 프리셋을 골라 걸기만 한다.
- **판 그림은 틀이 아니라 무대 안의 리그다.** 파서가 `cutin_open`을 [판 리그, 창]으로 푼다 — 판 리그는 배경처럼 그 무대의 far 층에 서고(슬롯 키 `{무대}_cutin_panel`, 원본 픽셀 × 배율 ÷ 그림 해상도 2), 무대 안이라 마스크가 판까지 함께 자른다. 열린 컷인 위의 `stage_mask`는 판과 인물을 함께 닫는다.
- 판도 마스크 안이라 판 아래 외곽선 띠가 같이 잘린다 — 마스크 테두리가 그 띠를 판 너비만큼 다시 그린다(`CutInPresetDBSO.outlineColor` · `maskInsetPixels`). 테두리는 마스크의 형제라 잘리지 않고, 창 안이라 팝 · 회전을 같이 탄다.
- 판 리그는 닫아도 남는다(마스크가 숨긴다). 다음 `cutin_open`이 다시 세운다. 같은 무대를 `stage_mask` 인서트로 쓰기 전에는 대본이 인물처럼 치운다(`<<bg_fade_out stage01_cutin_panel 0fr>>`).
- 판 리그는 폴드 밖이다 — 코어는 컷인 표(`CutIns`)로만 알고, 등가성 하네스는 캡처하지 않는다(`SetupCharRigCommandSpec.outsideFold`).
- 판 모양 · 기본 크기 · 모션은 게임 프리셋(`CutInPresetDBSO`), 설계와 실측은 [docs/PLAN_cutin.md](../../../../../docs/PLAN_cutin.md).

## 3. 그림 팝업 — 틀이 아니다

`<<popup 무대 그림 [x y] [배율]>>`은 파서가 그 무대의 close 층에 그림 리그를 세워 페이드 인하는 것으로 푼다(슬롯 키 `{무대}_popup`, 원본 픽셀 × 프리셋 배율 0.91 × 배율). `<<popup_close 무대>>`는 그 리그를 페이드 아웃한다(`StageCommandParser.Popup.cs`, 프리셋 `PopupPresetDBSO` — 배율 · 페이드 길이).

- 팝업은 그림 하나다. 뒤를 어둡게 까는 것은 대본(연출 카드)이 `stage_dim front … 무대`로 한다 — 그림 바로 뒤의 판이 그 무대의 far ~ front와 아래 무대를 덮는다.
- 무대 안이라 그 무대의 마스크 · 창을 따른다. 한 무대에 컷인과 함께 둘지는 대본의 선택이다 — 무대는 각자 독립된 화면이다.
- 폴드 밖이다 — 코어는 팝업 표(`Popups`)로만 알고, 등가성 하네스는 캡처하지 않는다. 설계와 실측은 [docs/PLAN_popup.md](../../../../../docs/PLAN_popup.md)(2026-11-09의 틀 구조 — 지금은 [PLAN_stage-visibility.md](../../../../../docs/PLAN_stage-visibility.md) §3.3).

## 4. 중립으로 돌리는 쪽

틀을 중립으로 돌리는 것은 **장면 경계 하나**다 — `ScenePresentation`이 `StageFrameRegistry.ResetAll`을 직접 부른다(이벤트 뒤로 숨기지 않는다).

- `stage_mask` · `stage_mask_reset`는 틀을 모른다 — 마스크만 움직인다. 컷인 판 · 팝업 그림은 무대 안의 리그라 마스크가 함께 자른다.
- 열린 컷인 위의 `stage_mask`는 컷인을 연 채로 둔다(창 상태) — 뒤의 `cutin_close`가 팝 아웃으로 정리한다.
- 컷인을 닫으면 창은 항등으로 돌아오고 그 무대는 마스크가 숨긴다 — 다음 `stage_mask`가 되돌릴 것이 없다.
- 2026-11-17까지는 `stage_mask`가 그 무대의 컷인 · 팝업을 먼저 닫았다(마스크 밖이라 가려지지 않아서). 판 · 그림을 무대 안으로 들이면서 걷었다([docs/PLAN_stage-visibility.md](../../../../../docs/PLAN_stage-visibility.md)).
