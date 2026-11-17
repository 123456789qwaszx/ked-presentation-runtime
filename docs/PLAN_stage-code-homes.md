# 무대 코드의 집 — 마스크 · tx · 어둡게는 무대 리그로, 컷인 · 팝업은 무대 틀로 (2026-11-17)

> 상태: R1–R4 끝(2026-11-17). 소유자가 1 · 2 · 3을 모두 하기로 했다.
> **§3 · §8과 R2(틀 되돌리기)는 [PLAN_stage-visibility.md](PLAN_stage-visibility.md)가 대신한다** — `tx`가 틀을 끄는 규칙 자체를 걷는다. R1 · R3(폴더 · 팩토리)은 그대로다.
> 남은 것: 블러의 집(§5)과 명령 이름(§6)은 소유자가 정한다.
> **동작은 바꾸지 않는다.** 코드가 사는 폴더와, 커맨드를 만드는 팩토리만 옮긴다.
> 본보기: [command-authoring.md](command-authoring.md) §6 — 컷인 창 · 그림 팝업을 무대 리그에서 무대 틀로 떼어 낸 일(2026-11-09). 그때도 동작은 같았고 틀린 것은 "어디에 무엇이 사는가"였다.

## 1. 왜

`tx`가 답하는 질문은 "이 무대를 어디까지 보여 줄까"다. 무대 리그 문서의 첫 문장이 무대 리그를 "카메라 · 깊이 층 · **마스크**"라고 한다.

| | 지금 어디에 있나 |
|---|---|
| 노드(`Mask_Root` · `MaskEdge_Root`) | 무대 리그 스키마 |
| 컴포넌트를 붙이고 묶는 일 | `StageRigBuilder.EnsureMask`, 참조는 `StageRigRefs.Mask` |
| 정지 때 기준으로 되돌리기 | `StageRigRegistry.ResetAllMasksToBaseline` |
| 문서 | `stage-rig.md` §2.5 |
| **마스크 코드**(그래픽 · 슬롯 · 테두리 · 모양 종류 · 프리셋 DB) | `Stage/ScreenEffect/StageMask/` |
| **명령**(`tx` · `tx_stage_mask_clear`)과 그 팩토리 | `Stage/ScreenEffect/Commands/` · `ScreenEffectCommandFactory` |

`Stage/ScreenEffect/`는 "화면 전체에 얹는 효과"(화면 효과 리그)의 집이다. 마스크는 노드 · 빌더 · 레지스트리 · 문서가 모두 무대 리그인데 코드만 그 폴더에 있다.
어둡게(`screen_dim`)도 이 어긋남을 따라갔다. 판의 컨트롤러는 `Stage/StageRig/`에 두었지만 명령은 `tx`를 본떠 화면 효과 폴더와 팩토리에 두었다.

`ScreenEffectCommandFactory` 하나가 네 시스템의 커맨드를 만든다.

| 시스템 | 커맨드 |
|---|---|
| 화면 효과 리그 | 덮개 · 플래시 · 노이즈 · 비네트 |
| 무대 리그 | `tx` · `tx_stage_mask_clear` · `screen_dim` |
| 무대 틀 | 컷인 열기 · 닫기, 그림 팝업 열기 · 닫기 |
| 블러 시스템(`RigBlurIntentSystem`) | `screen_blur` |

## 2. 지금 → 옮긴 뒤

| 코드 | 지금 | 옮긴 뒤 |
|---|---|---|
| `StageMaskGraphic` · `StageMaskSlot` · `StageMaskEdgeGraphic` · `StageMaskKind` · `StageMaskMotionPresetDBSO` | `Stage/ScreenEffect/StageMask/` | `Stage/StageRig/Mask/` |
| `StageMaskMotionCommand` · `StageMaskClearCommand` · `StageLayerDimCommand` | `Stage/ScreenEffect/Commands/` | `Stage/StageRig/Commands/` |
| 그 셋을 만드는 곳 | `ScreenEffectCommandFactory` | `StageRigCommandFactory`(새) |
| 컷인 · 팝업 커맨드를 만드는 곳, 프리셋이 없을 때의 경고 · 기본값 | `ScreenEffectCommandFactory` | `StageFrameCommandFactory`(새, `Stage/StageFrame/`) |
| `ScreenEffectCommandFactory` | 아홉 가지 | 덮개 · 플래시 · 노이즈 · 비네트 + `screen_blur`(§5) |

- `.meta`를 함께 옮긴다. GUID가 그대로라 씬이 든 `StageMaskMotionPresetDB` 참조와, 씬 · 프리팹에 붙은 마스크 컴포넌트가 깨지지 않는다.
- 두 폴더 모두 어셈블리 `Ked.Presentation.Stage`다 — asmdef는 바뀌지 않는다.
- 클래스 이름 · 직렬화 필드 · 대본 어휘는 그대로다.
- 조립(`SampleBootstrap.Presentation.cs`)은 팩토리 셋을 차례로 만들어 `CompositeCommandFactory`에 넣는다.

## 3. tx가 틀을 되돌리는 규칙 — 어디에 드러낼까

지금 `tx`와 마스크 지우기 커맨드는 틀 레지스트리를 받아 **직접 부른다**. 무대를 어디까지 보여 줄지를 새로 정하면 그 무대의 컷인 · 팝업은 끝난다는 규칙이다([stage-frame.md](../Assets/Ked.Presentation/Stage/StageFrame/Documentation~/stage-frame.md) §4: "이벤트 뒤로 숨기지 않고 부르는 쪽에 드러낸다").
이 커맨드를 `Stage/StageRig/`로 그대로 옮기면 리그 폴더의 코드가 틀을 알게 된다. 그것은 "틀은 리그를 알고, 리그는 틀을 모른다"(stage-frame.md 첫머리, command-authoring §6)와 어긋난다.

| 후보 | 어떻게 | 판단 |
|---|---|---|
| **A. 파서가 둘로 낸다** | `tx` → [틀 되돌리기(그 무대), 마스크 모션]. `tx_stage_mask_clear` → [틀 전부, 마스크 전부] | 커맨드마다 시스템이 하나다. 규칙은 대본 어휘를 커맨드로 푸는 자리(문법)에 드러난다. **이것으로 한다** |
| B. 리그 쪽 인터페이스 | 리그가 "마스크를 새로 정했다"를 듣는 인터페이스를 정하고, 틀 레지스트리가 구현하고, 조립이 꽂는다 | 방향은 맞다. 다만 커맨드만 봐서는 누가 듣는지 보이지 않는다 |
| C. 이벤트 | 마스크 슬롯이 알리고 틀이 듣는다 | stage-frame.md §4가 피한 것 |
| D. 그대로 둔다 | 리그 폴더의 커맨드가 틀을 안다 | 옮기는 까닭과 어긋난다 |

A의 새 커맨드 하나: `StageFrameResetCommand`(`Stage/StageFrame/`). 스펙은 무대(또는 전부)와 까닭을 갖는다. **대본 어휘가 아니다** — 표에 이름이 없고, `tx` · `tx_stage_mask_clear`가 낼 뿐이다.

동작이 같은 까닭:

| | 지금 | A |
|---|---|---|
| 순서 · 때 | `tx`가 처음 닿을 때(`ResolveRefs`) 틀을 되돌리고 마스크 모션을 건다 | 틀 되돌리기가 바로 앞 커맨드다. 같은 줄 · 같은 자리에서 그 순서로 돈다 |
| 압축 · 시크 · 되감기 | 틀 되돌리기는 즉시다 | 같다. 즉시라 압축과 상관없다 |
| 경고 | `tx`가 열린 컷인 · 팝업을 덮으면 경고, 마스크 지우기는 조용히 | 틀 되돌리기가 스펙의 까닭(`tx '프리셋'`)을 적어 경고한다. 까닭이 비면 조용히 |
| 프리셋이 빈 `tx` | 경고하고 아무것도 내지 않는다 | 같다. 틀 되돌리기도 내지 않는다 |
| 폴드 | 이 규칙을 모른다 | 같다 |

## 4. 단계

틀 의존을 먼저 떼고 나서 옮긴다. 단계마다 Core 테스트와 Stage · EditMode 컴파일을 지나고 커밋한다.

| 단계 | 일 | 확인 |
|---|---|---|
| R1 | 마스크 코드를 `Stage/StageRig/Mask/`로(`git mv`, `.meta` 함께) | 컴파일. GUID가 같다 |
| R2 | 무대 틀 팩토리와 틀 되돌리기 커맨드. 파서가 `tx` · `tx_stage_mask_clear`를 둘로 낸다. 마스크 커맨드에서 틀 레지스트리가 빠진다 | 파서 테스트(무엇을 어떤 순서로 내나), 틀 되돌리기 테스트 |
| R3 | 무대 리그 커맨드 셋을 `Stage/StageRig/Commands/`로, `StageRigCommandFactory`. 화면 효과 팩토리를 줄인다. 조립 | 컴파일, 전체 테스트 |
| R4 | 문서 — Stage README의 폴더 표 · stage-rig.md(§2.5 코드의 집 · §6 흐름) · stage-frame.md(§1 · §4) · command-authoring.md §6 · PLAN_cutin.md 파일 표 · PLAN_screen-cover.md 파일 표 | |

**Unity에서 소유자가 볼 것** — `PresentationSample`을 열어 Missing Script가 없는지, 데모 대본의 `tx` 절 · 컷인 뒤 `tx` · 어둡게 절이 전과 같은지.

### 진행

| 단계 | 커밋 | 상태 |
|---|---|---|
| R1 | `0e002827` | 끝 — 마스크 코드 다섯 파일과 그 `.meta`, 폴더 `.meta`를 `Stage/StageRig/Mask/`로. 코드 변경 없음 |
| R2 | `5d63377e` | 끝 — `StageFrameResetCommand` · `StageFrameCommandFactory`, 파서가 `tx` · `tx_stage_mask_clear`를 둘로. 테스트 `StageFrameResetTests`(6), 파서 테스트의 tx 자리를 `[1]`로 |
| R3 | `e3a3c46e` | 끝 — 커맨드 셋을 `Stage/StageRig/Commands/`로, `StageRigCommandFactory`. 화면 효과 팩토리는 덮개 · 플래시 · 노이즈 · 비네트 · `screen_blur` |
| R4 | 문서 커밋 | 끝. `PLAN_stage-seam.md`의 팩토리 목록은 그때(④) 일의 기록이라 고치지 않았다 |

유니티 밖 검사: Core 테스트 487 통과, Stage · EditMode 테스트 컴파일 통과(`LogAssert`처럼 유니티 테스트 프레임워크에만 있는 것은 이 검사가 보지 못한다). 조립(`Samples/`)과 재생(`Playback/`)은 이 검사가 컴파일하지 않는다 — Unity에서 처음 컴파일된다. EditMode 테스트 실행도 Unity에서.

## 5. 이번에 하지 않는 것

- **블러.** `screen_blur` · `focus_on`과 `RigBlurIntentSystem`도 `Stage/ScreenEffect/`에 산다. 이것은 화면 효과 리그가 아니라 캐릭터 리그마다 블러를 맞추는 시스템이다 — 같은 어긋남이다. 이번에 옮기지 않으므로 `ScreenEffectCommandFactory`에 `screen_blur`가 남는다. 옮길지와 어디로 옮길지는 소유자가 정한다.
- **명령 이름.** 이번 정리는 이름을 바꾸지 않는다(§6).

## 6. 정할 것 — 명령 이름

`tx`는 "transition"의 줄임이지만, 이 명령은 전환만 하지 않는다. 반으로 나눈 화면 · 띠처럼 모양을 세워 두고 머무는 데도 쓴다. 하는 일은 "그 무대의 마스크 모양을 움직인다"다.

| 지금 | 후보 | 까닭 |
|---|---|---|
| `tx <프리셋> [무대] [길이]` | **`stage_mask <프리셋> [무대] [길이]`** | 코드(`StageMask*`) · 문서(무대 리그의 마스크)와 같은 말이다 |
| `tx_stage_mask_clear` | **`stage_mask_clear`** | 위와 짝 |
| `screen_dim` · `screen_dim_reset` | `stage_dim` · `stage_dim_reset` | 화면 전체가 아니라 무대 · 층 단위로 작동하고, 그 무대의 마스크에 잘린다 |
| `screen_blur` · `screen_blur_reset` | (`stage_blur` · `stage_blur_reset`) | 같은 결이다. 블러를 옮길 때 함께 정한다 |

이름을 바꾸면 고칠 곳이 런타임 밖에도 있다.
- 런타임: 문법 표 · Yarn 브리지 등록 · 데모 대본 · 문서 · 테스트, 그리고 프리셋 DB가 `tx_` 접두사를 걷어 내는 코드.
- 대본을 내는 쪽: 저작 툴(ked-story-compositor — 문법 표 주석이 "툴은 정본만 낸다"고 한다) · 작업공간의 변환기와 카드 · story-director의 계약.
- 문법 표는 별칭을 두지 않는다. 그래서 옛 이름은 같은 날 함께 걷어야 한다.

## 7. tx로 좁힌 영역 안만 어둡게 — 된다

- `screen_cover`로는 안 된다. 덮개는 화면 효과 리그의 것이라 모든 무대 위에서 화면 전체를 덮는다.
- `screen_dim`으로 된다. 어둡기 판은 층마다 서고, 층은 `Mask_Root` 안(카메라 리그 아래)에 있다. 그래서 판은 그 무대의 마스크에 잘린다.

```
<<tx hstrip_open stage01>>                       stage01을 가로 띠로 연다
<<screen_dim all 0.6 0.3s Linear stage01>>       띠 안만 어두워진다 — 아래 stage00은 그대로
<<screen_dim all 1 0.3s Linear stage01>>         세기 1 = 띠 안을 검게
<<screen_dim all 0 0.3s Linear stage01>>         걷는다
```

- 마스크 테두리(`MaskEdge_Root`)는 `Mask_Root`의 형제라 판 밖이다 — 테두리 선은 밝은 채 남는다.
- 컷인 창의 판 그림(`Panel`)은 무대 루트 바깥(틀)이라 어두워지지 않는다. S5 확인 때 넘어간 그 모습과 같다.
- 판은 검정뿐이다. 흰색으로 띠 안을 덮는 일은 지금 없다.

## 8. 열린 틀 위의 tx — Unity에서 보고 정한다

소유자의 물음(2026-11-17): tx가 그 무대의 컷인 · 팝업을 왜 닫는가. tx가 다 가리면 어차피 안 보이고, 닫히는 동작 전에 먼저 사라지면 어색하다.

**구조에서 확인한 것**
- 캐릭터 리그는 tx가 건드리지 않는다. 무대 안이라 마스크 모양대로 잘린다.
- 컷인 판 그림(`{stage}_Panel`)은 창 아래 · 무대 루트 밖, 그림 팝업은 창 밖이다 — 둘 다 무대 마스크 밖이라 **tx로는 가려지지 않는다**.
- 마스크는 무대마다 하나다. 컷인도 네 점 모양을 쓰므로 tx가 시작하는 순간 컷인의 모양은 어차피 바뀐다.

**되돌리기가 하는 일은 두 가지가 섞여 있다**

| 경우 | 지금 | 판단 |
|---|---|---|
| 가. 닫힌 채 남은 창 — `cutin_close` 뒤 창은 배율 0이라 그 무대가 통째로 안 보인다 | 창을 되돌린다. 경고도 낸다 | 되돌리기는 필요하다(안 하면 다음 tx가 열어도 안 보인다). 이미 안 보여 티가 나지 않는다. 경고는 정상 순서인데도 나서 소음이다 |
| 나. 열린 컷인 · 팝업 위의 tx | 판 · 팝업을 퇴장 없이 바로 끄고, 경고 | 컷인 계획서는 이것을 대본 실수로 정했다(PLAN_cutin.md §5 "같은 무대에 tx와 함께 쓰지 않는다"). 소유자가 말한 어색함이 여기서 난다. 폴드는 tx가 팝업을 닫는 것을 몰라 실제 화면과 어긋난다 |

**견주어 볼 것** — 조립(`SampleBootstrap`)의 `txKeepsOpenFrames`(인스펙터 "실험 — tx와 열린 틀")가 `StageFrameCommandFactory` → `StageFrameResetCommand`로 간다.

| | 끔(지금 규칙) | 켬(실험 A) |
|---|---|---|
| 가 | 되돌린다, 경고 | 되돌린다, 조용히 |
| 나 | 바로 끈다, 경고 | 그대로 둔다, 경고 |

데모 대본 끝의 "tx와 열린 틀" 절(①–④)이 두 경우를 모두 지난다. 이 토글과 절은 정하면 지운다.

**정할 것**
- A — 나는 건드리지 않고 경고만, 가만 조용히 되돌린다. 폴드와 화면이 맞는다. 잘못 쓴 대본은 판 · 팝업이 남아 실수가 보인다.
- B — 지금처럼 둘 다 되돌리고, 경고는 나에만 낸다. 화면은 정돈되지만 튀는 순간은 남는다.
- C — 열린 컷인을 tx 모양으로 닫는 것이 필요한 연출이면 새 기능이다(마스크 하나를 컷인과 tx가 같이 써서 지금 구조로는 안 된다).
