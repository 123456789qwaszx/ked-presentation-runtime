# 커맨드 · 무대 시스템을 새로 만들 때 (2026-11-09)

> 컷인 · 그림 팝업을 처음 짤 때(`89a0d471` · `86c1e377`) 커맨드가 트윈을 쥐는 틀을 새로 만들었다가,
> 소유자가 tx와 같은 틀로 다시 짰다(`a48bb540` · `c41cd312`). 이어서 그 코드의 집도 무대 리그에서 무대 틀로 옮겼다(§6). 같은 실수를 되풀이하지 않으려고 남긴다.
> 사람이든 에이전트든, 새 커맨드를 쓰기 전에 이 문서를 먼저 읽는다.

## 1. 먼저 읽는다 — 결정은 이미 내려져 있다

| 읽을 것 | 무엇이 정해져 있나 |
|---|---|
| [analysis-2026-11-03-command-system.md](analysis-2026-11-03-command-system.md) E2 | **커맨드가 트윈을 쥐지 않는다(컨트롤러 소유).** `ClaimTweenCommandBase`의 클레임 틀은 같은 줄에서 두 번 걸면 앞 것이 되살아나는 문제로 걷어냈다(57곳) |
| [PLAN_stage-seam.md](PLAN_stage-seam.md) | 실행기 · 무대 · 코어의 경계 |
| 가장 가까운 본보기 코드 | 아래 2절 |

걷어낸 틀을 되살리지 않는다. 본보기와 다르게 할 곳이 있으면 설계 문서에 이유를 적고 소유자에게 먼저 묻는다.

## 2. 본보기 — 무엇을 닮을지 먼저 정한다

| 만들려는 것 | 본보기 |
|---|---|
| 무대 노드에 모션 · 페이드를 거는 커맨드 | tx — `StageMaskMotionCommand` + `StageMaskSlot`. 컷인(`StageFrame/CutIn`) · 그림 팝업(`StageFrame/Popup`)도 이 틀이다 |
| 무대 루트 바깥에 무대를 띄우는 것 | 무대 틀 — `StageFrameBuilder` · `StageFrameRefs` · `StageFrameRegistry`(`stage-frame.md`) |
| 리그 시각 효과 · 블러 | `RigVisualEffectController` + `IntentTransition` |
| 원본 픽셀 크기로 그리는 것 | 배경 — `NodeTargets.BackgroundSizing` · `background-dimensions.json`(`cc927f38`) |

설계 문서(`docs/PLAN_*.md`)의 첫머리에 "무엇을 본보기로 따른다"를 적는다.

## 3. 틀 — 커맨드는 걸기만 한다

```
XxxCommand : CommandBase (sealed, 파일 하나)
  ExecuteInner:  if (!_resolveAttempted) ResolveRefs();  _slot.PlayXxx(...);  if (!_spec.wait) yield break;  while (_slot.IsPlaying...) yield return null;
  OnSkip:        if (!_resolveAttempted) ResolveRefs();  _slot.SnapXxx(...);
  ResolveRefs:   _resolveAttempted = true; 슬롯 · 프리셋 값 · 그림을 찾는다(못 찾으면 경고)

XxxSlot (무대 노드의 주인)
  트윈 하나(_motion · _fade)를 혼자 갖는다
  PlayXxx / SnapXxx / IsPlaying... / Reset
  새 Play는 앞 트윈을 Kill(true)로 끝값까지 확정하고 시작한다. Reset은 Kill(false)로 끝값 없이 끊는다
```

- **트윈 · 진행 상태는 슬롯(또는 컨트롤러)이 혼자 갖는다.** 커맨드가 자기 트윈이나 확정 콜백을 슬롯에 맡기는 순간(`SetActive(tween, Finish)` 같은 것) 주인이 둘이 되고, 끝나는 길마다 플래그가 붙는다.
- 커맨드의 플래그는 `_resolveAttempted` 하나다.
- 끝나는 길은 넷뿐이다: 자연 종료(슬롯 트윈의 OnComplete) · 스킵(Snap) · 다음 커맨드(슬롯이 앞 트윈을 확정) · 장면 경계 · tx(Reset).
- **스텝 경계에서 손대지 않는다.** tx처럼 제 끝까지 돈다. 스킵 · 되감기만 스냅한다.

## 4. 레포 전체의 관례 — 새로 정하지 않고 옮긴다

| 관례 | 어떻게 |
|---|---|
| 시간축 | 무대 트윈은 `SetUpdate(true)`(실시간). tx · 컷인 · 팝업 · 플래시 · `IntentTransition` 모두 같다 |
| 주입 | 커맨드 생성자는 `(spec, 프리셋, 레지스트리 …)`를 받아 그대로 둔다 — `?? throw` 같은 방어 코드를 붙이지 않는다. 연결 확인은 조립(팩토리)이 한다 |
| 검사 자리 | 어휘 검사는 파서, 끝 상태 검사는 폴드. 실행 층에서 같은 것을 다시 막지 않는다 |
| 파일 | 커맨드마다 파일 하나(스펙은 그 커맨드 파일 위에). 이름은 `XxxCommand` · `XxxCommandSpec` |
| 스펙 | `[Header("Target")]` 등으로 칸을 묶는다 |
| 이름 | `ResolveRefs` · `_resolveAttempted`(2026-11-06 `ResolveTargets`에서 바꿨다) |

## 5. 쓰기 전에 보여 준다

채우기 전에 뼈대를 먼저 내보인다 — 클래스 · 메서드 이름 · 누가 무엇을 갖는지. 본보기와의 대조표를 같이 낸다.
설명이 길어야 하는 코드는 틀이 잘못됐다는 신호다.

## 6. 어디에 둘지 — 재료가 아니라 질문으로 정한다

> 컷인 창 · 그림 팝업을 처음에 무대 리그(`StageRig/`)에 넣었다가, 소유자가 무대 틀(`StageFrame/`)로 떼어 냈다(2026-11-09, `Stage/StageFrame/Documentation~/stage-frame.md`).
> 동작은 같았고, 틀린 것은 "어디에 무엇이 사는가"였다.

- **그 시스템이 답하는 질문으로 집을 정한다.** 무대 리그는 "무대 안의 세상을 어떤 구도로 볼까"(카메라 · 깊이 층 · 마스크), 무대 틀은 "그 무대를 화면 위에 어떻게 띄울까"(창 · 판 · 팝업)다. 세울 때 같은 재료(무대 루트)가 필요하다는 것은 같은 시스템이라는 뜻이 아니다.
- **설계의 경계를 코드 소유까지 밀고 간다.** 노드를 "무대 루트 바깥"에 두기로 했으면 그 코드도 무대 루트 안쪽을 맡는 쪽(리그 빌더 · refs · 스키마) 바깥에 둔다.
- **의존은 한 방향.** 새로 얹는 층이 기존 층을 알고, 기존 층은 새 층을 모른다(틀 → 리그). 조립이 둘을 차례로 세운다.
- **이름이 하는 일을 숨기지 않는다.** `ResetAllMasksToBaseline`이 컷인 · 팝업까지 닫으면 규칙이 마스크 메서드 안에 숨는다. 다른 시스템을 함께 건드리는 규칙은 부르는 쪽에 드러낸다(`ScenePresentation`이 `_frames.ResetAll()`을 직접 부른다).
- **남의 속성을 빌려 서지 않는다.** 팝업이 `refs.CutIn.Window`로 자리를 잡았던 것처럼 상관없는 기능의 속성을 빌려야 한다면, 그 속성의 주인이 잘못 정해진 것이다(창은 컷인 것이 아니라 틀 것).

**멈출 신호 — 하나라도 보이면 구조를 다시 본다**

| 신호 | 그때 있었던 것 |
|---|---|
| 기존 시스템의 설명(문서 첫 문장)과 맞지 않는 노드 · 메서드가 그 안에 생긴다 | 리그에 "카메라 · 깊이와 상관없는" 창 · 팝업 |
| 스키마가 "계층을 소유한다"는데 새 노드는 표에 없고 이름 함수만 있다 | `WindowName` · `PopupName`만 있고 `Nodes`에 없음 |
| 한 빌더 안에 다시 세우기 규칙이 두 벌이다 | 안쪽 "부수고 다시", 바깥쪽 "어긋난 자식만 치움" |
| 같은 레지스트리가 서로 다른 말을 한다 | `StageOf`는 창을 "어느 무대에도 없다"는데 refs는 창을 든다 |
| 새 기능 때문에 **기존 테스트의 첫 줄**이 바뀐다 | "무대 루트의 부모는 mount" → "컷인 창" |
