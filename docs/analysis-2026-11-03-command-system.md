# 연출 커맨드 시스템 — 목적 · 흐름 · 규칙, 그리고 예외 (2026-11-03 개정)

기준: `dev` @ `00534410` (face_swap 수정 포함). 앞 판(`ec1a5d2` 기준)에서 바뀐 곳은 §7에 모았다.
실행기(`Assets/Ked.CommandExecutor`)는 `ked-command-executor`에서 들여온 반입본이다(`Documentation~/vendoring.md`).
코드로 닫지 못한 항목은 [추론]으로 표시했다.

## 1. 목적

대본의 `<<커맨드>>`를 Unity 화면 연출로 바꾸는 층이다.

- **줄이 실행 단위다.** 커맨드는 쓰인 자리가 아니라 다음 줄 머리에서 한꺼번에 실행된다.
- **뜻은 한 곳에만 둔다.** 이름 → 스펙은 `StageCommandParser`만 한다. 폴드(`StageReducer`)도 같은 `StageCommand`를 받는다.
- **실행기는 VN을 모른다.** 재생 정책에서 받는 값은 `ICommandPacePolicy`의 둘(압축 여부, 시간 배율)뿐이다.
  빨리 넘기기·시크는 "압축"으로, 속도 올리기는 "시간 배율"로 `PresentationSessionContext`가 접어서 넘긴다.

## 2. 구성 요소

| 타입 | 하는 일 |
|---|---|
| `CommandBridge` (Playback) | Yarn에 이름·인자 수를 등록한다. `Forward`가 파서를 부르고 결과를 드라이버에 쌓는다. 파서가 모르면 `LogError` |
| `StageCommandParser` (Runtime) | `Table`이 이름·인자 자리·생략값을 정한다. 커맨드 하나가 스펙 0..n개(`show`는 4개) |
| `YarnPlaybackDriver` | 줄 경계까지 스펙을 모은다(`Enqueue`). `PlayCollected`로 실행, `TryDiscardPending`으로 버림 |
| `CommandExecutor` | 스펙 → 커맨드(팩토리), 실행 세대 `_runId`, 토큰, `RunNode` 코루틴 |
| `SequencePlayer` | 커맨드마다 첫 `MoveNext`를 돌리고 전경·배경을 가른다. 배경이면 수명에 묶는다 |
| `CommandRunScope` | 토큰, `IsCancelled`, 압축, 시간 배율, Step·Run `LifetimeScope`, `CommandIsPlaying` |
| `PresentationScopeSession` | scope의 생애. `Start`로 만들고 `End`로 `Executor.Stop(Cancel)` |
| `CommandBase` 계열 | `CommandBase` → `ClaimTweenCommandBase` → `CanvasFadeCommandBase`·`SlideCommandBase`. 별도로 `ShotIntentCommandBase<T>` |

브리지 등록 이름 74개와 파서 표 74개는 지금 정확히 맞는다.

## 3. 흐름

```
[Yarn이 <<cmd>>를 만남]
  Bridge.Forward → Parser.TryParse → Spec 0..n → Driver.Enqueue          (쌓기만 한다)

[다음 줄 진입] LinePresentationFlow (PlayCollected를 부르는 유일한 곳)
  Driver.PlayCollected → Executor.PlaySpecs
    ① scope.CleanupStep(Finish)      ← 앞 줄 배경 커맨드의 마무리 = 스텝 경계
    ② factory로 Spec → ISequenceCommand
    ③ 새 토큰을 scope.Token에 넣고 RunNode 시작 (_runId는 올리지 않는다)

RunNode → SequencePlayer.PlayCommands   (CommandIsPlaying = true)
  커맨드마다 첫 MoveNext = CommandBase.Execute의 머리:
    IsCancelled면 끝
    압축이면 SkipPolicy: Ignore 끝 / CompleteImmediately → OnSkip 후 끝 / ExecuteEvenIfSkipping 통과
    ExecuteInner를 만들어 yield (본문은 아직 돌지 않는다)
  WaitForCompletion && !압축 → 전경: 끝날 때까지 기다린다 (AdvanceGate가 cps_node_busy로 넘김을 막음)
  그 밖                      → 배경: Step 수명 등록 + StartCoroutine (여기서 ExecuteInner 본문이 돈다)
  (CommandIsPlaying = false)

[정지] ScenePresentation.StopAsync
  Yarn 정지 → 남은 명령 버림 → 줄 표시 중단 → 롤백 기록 비움 → 샷 Clear → scope.End → Executor.Stop(Cancel)
[장면 시작] BeginScene : 남은 명령 버림 → PresentationStage.Clear(리그 파괴) → scope.Start
[리플레이]  PrepareReplay : PresentationStage.Clear → scope.Start   (롤백·로드는 장면을 처음부터 압축 재생)
```

큐를 거치지 않는 커맨드는 `surface_layout` · `surface_reset` 둘이다. 브리지에서 상태만 바꾸고,
적용은 다음 줄의 `ShowLineAsync`가 한다 — 효과가 나는 시점은 큐와 같다.
대사창 표시는 커맨드가 아니라 줄 태그(`#boxhide` · `#boxfadein` · `#auto` …)가 정한다.

## 4. 규칙

**R1 줄이 실행 단위다.**
- 쌓인 커맨드는 다음 줄 머리에서 실행된다. 선택지 앞에 쓴 커맨드는 선택 뒤 첫 줄에서 돈다.
- 줄 없이 노드가 끝나면 남은 커맨드는 경고와 함께 버린다(`DiscardPendingCommands(warn: true)`).

**R2 뜻은 파서 한 곳에 있다.** 브리지는 이름과 인자 수만 안다.

**R3 압축하면 끝 상태로 바로 간다.**
- 압축 = RapidSkip 모드 또는 시크 중.
- 롤백·로드는 장면을 처음부터 압축 재생한다. 그래서 **`OnSkip`은 자연 종료와 같은 종점을 써야 한다** — 재현의 근거다.
- 기본값 `CompleteImmediately`. 구조 커맨드(slot · cast · actor · sibling_* · char_to · stop_voice · stop_all_sfx)는 `ExecuteEvenIfSkipping`. 효과음·보이스는 `Ignore`.

**R4 전경만 줄 진행을 막는다.** `CommandIsPlaying`은 전경이 도는 동안만 true다. RapidSkip도 막힌다(2026-11-03, S2) —
대신 압축이 켜지면 `pause`가 그 자리에서 끝나므로 막힘은 한 프레임이다.

**R5 클레임 — 한 대상에 쓰는 것은 하나다.**
- 대상을 잡을 때 앞 트윈을 `DOKill(true)`로 **완주**시킨다 → 앞 소유자의 `OnComplete → Commit`이 먼저 돌고 claim을 내려놓는다.
- 그다음 목표 계산(코어 리덕션) → 필요하면 장부 게시 → 트윈 시작.

**R6 스텝 경계는 `OnStepLifetimeFinished` 하나로 정해진다.**
- 배경 커맨드는 전부 Step 수명에 등록된다(`CommandBase` 기본). 경계에서 `StopCoroutine` + 훅.
- 파서가 `wait`를 켜지 않으므로 `ExecuteInner`는 트윈만 걸고 끝난다. `StopCoroutine`은 멈출 것이 없고, 경계에서 할 일은 훅이 전부다.

| 경계 처리 | 커맨드 |
|---|---|
| 가속 ×30 | `ClaimTweenCommandBase` 계열 (fade · slide · move_by · rotate_to · scale_to · size · place) |
| 가속 ×1.5 | char_visual_* · focus_on/clear · screen_blur |
| 가속 ×20 | shot_* (시스템이 소유한 전환을 가속) |
| 즉시 확정 | gesture · mirror · rotate_by (`AccelerateOnStepFinish => false`), motion |
| **비워 둠 (의도)** | screen_flash · screen_vignette · screen_noise (+`_clear`) · tx · face_swap |
| 해당 없음 | 즉시 실행류(face · bg_sprite · set_anchor 계열 · pose · tx_stage_mask_clear), 오디오 |

**"비워 둠"은 화면상의 연출이라 일부러 관리하지 않는 부류다.** 트윈이 `OnComplete`까지 돌고, 끝나는 길은 셋이다:
자연 완료 / 압축 시 `OnSkip` / 뒤 커맨드의 클레임(`Kill(true)`) — 셋 다 같은 Commit에 닿는다.
안전하려면 둘이 필요하다:
1. 같은 대상에 쓰는 커맨드가 모두 같은 클레임을 거친다 → 나중 쓰기가 이긴다.
2. 종점이 `OnSkip`과 같다 → 리플레이가 같은 화면을 만든다.

| 커맨드 | 트윈 타깃 | 같은 대상의 다른 쓰기 | ① |
|---|---|---|---|
| screen_flash | 컨트롤러가 재생을 소유(이벤트) | `_clear`도 같은 클래스(preset `clear`) | ✅ 종점이 0 — 남는 상태 없음 |
| screen_vignette / noise | 컨트롤러가 전환을 소유(장면 전역 의도) | 같은 클래스뿐 | ✅ 새 의도는 보이는 값에서 재조준 |
| tx | 슬롯이 모션을 소유(장면 전역 의도) | `tx_stage_mask_clear` — 같은 슬롯의 `Clear`가 모션을 끊음 | ✅ 새 모션은 제 시작 모양부터 다시 세움 |
| face_swap | 위 겹 루트 시퀀스 | face · show · cast · bg_sprite — 모두 `CompleteInFlight`를 거친다 (`00534410`) | ✅ |

이력상 이 다섯은 스텝 훅을 가진 적이 없다. reduction-boundary.md도 화면 효과·tx를 "무대 상태 모델 밖"으로 둔다.
face_swap만 모델 안(폴드가 `ApplyFace`로 접음)이라 클레임을 맞춰야 했다(§6 E1).

**R7 런이 끝나면 Cancel한다.**
- `Executor.Stop(Cancel)`: `_runId++` → `MarkCancelled` → 토큰 취소 → Step·Run 정리의 cancel 쪽.
- cancel 람다는 `StopCoroutine`만 한다. **트윈은 죽이지 않는다.** 리그 트윈은 `PresentationStage.Clear`의 리그 파괴(`DOKill(false)`)에서, 샷은 `Clear`에서 정리된다.

**R8 취소는 세 축이다.** `_runId`(실행 세대) / `CancellationToken`(현재 run의 작업) / `scope.IsCancelled`(세션의 생사).

**실제 사용에서 드러나는 사실**
- 스펙의 `wait`를 켜는 코드가 없다. 대본에서 쓸 수 있는 전경은 `pause`/`Nfr`(`WaitCommand`)뿐이고, 트윈 커맨드는 전부 배경이다.
- 한 줄의 커맨드는 같은 프레임에 순서대로 시작한다. `pause`가 있으면 그 뒤는 `pause`가 끝난 뒤에 시작한다.

## 5. 커맨드 분류

| 무리 | 커맨드 | 압축 | 스텝 경계 | 클레임 |
|---|---|---|---|---|
| 클레임-트윈 | fade_in/out · slide_in/out · move_by · rotate_to · scale_to · size · place | OnSkip → Commit | 가속 ×30 | `DOKill(true)` |
| | gesture · mirror · rotate_by | 〃 | 즉시 확정 | `DOKill(true)` |
| | char_visual_* · focus_on/clear · screen_blur | 〃 | 가속 ×1.5 | **`Kill(false)`** |
| 샷 (장면 전역 의도) | shot_* | `Snap(다음 의도)` | 시스템 가속 ×20 | 없음 — 시스템이 전환 하나를 소유, 새 의도는 보이는 값에서 재조준 |
| 모션 | motion | OnSkip → Commit | 즉시 확정 | 노드마다 `DOKill(true)` |
| 비워 둠 | face_swap | OnSkip → 확정 | 없음(의도) | `CompleteInFlight` |
| 무대 마스크 (장면 전역 의도) | tx · tx_stage_mask_clear | `SnapMotionEnd` / 같은 `Clear` | 없음 — 슬롯의 모션이 끝까지 감 | 없음 — 슬롯이 모션 하나를 소유, 새 모션·clear가 앞 것을 끊음 |
| 화면 효과 (장면 전역 의도) | screen_vignette · screen_noise (+`_clear`) | `Snap(의도)` | 없음 — 컨트롤러의 전환이 끝까지 감 | 없음 — 컨트롤러가 전환 하나를 소유(`IntentTransition`), 재조준 |
| 화면 효과 (이벤트) | screen_flash | 재생 중인 플래시를 끊음 | 없음 — 끝까지 터짐 | 없음 — 컨트롤러가 재생을 소유, 새 플래시는 앞 것을 끊음 |
| 즉시 실행 | face · bg_sprite · set_anchor 계열 · ApplyTrackOffset · tx_stage_mask_clear · pose | OnSkip = 같은 적용 | 해당 없음 | face·bg_sprite는 `CompleteInFlight`, 나머지 대부분 `DOKill(true)` |
| 구조 | slot · cast · actor · sibling_* · char_to | 그대로 실행 | 해당 없음 | — |
| 시간 | pause · Nfr | OnSkip(기다리지 않음) | 전경 | — |
| 오디오 — BGM (장면 전역 의도) | bgm · stop_bgm(Complete) | 시크 중에는 의도만 쌓음(`Hold`) | scope 밖 | AudioSystem 소유. 장면을 넘어 이어 감, 시크 끝에 `Release`로 실제와 맞춤 |
| 오디오 — 이벤트 | sfx · voice(Ignore) · stop_voice · stop_all_sfx(EvenIfSkipping) | | scope 밖 | AudioSystem 소유. 정지에서 끊음 |
| 신호 | HoldSignal · RaiseSignal | | | 파서 표에 없음 |

screen_blur는 이름과 달리 화면 포스트 이펙트가 아니라 리그마다 거는 셰이더 블러다(`StageLayerBlurCommand` 머리 주석).
그래서 리그 상태를 쓰는 char_visual·focus_on과 같은 무리로 관리된다.

## 6. 예외 — 규칙대로 동작하지 않는 것

### 커맨드

**E1. face_swap — 해결됨 (`00534410`).**
- 앞 판의 지적(스프라이트 축: face가 클레임 없이 써서 face_swap이 옛 표정으로 덮음)에 더해, α 축도 깨져 있었다.
  face_swap이 초상 CG(fade_in/out의 표적)에 `alpha = 1`을 써서 fade_out으로 퇴장한 캐릭터가 다시 보였다. 예전 crossfade 틀에서 남은 쓰기였다.
- 수정: 초상 α를 쓰지도 클레임하지도 않는다. 위 겹은 `덮은 정도 × 초상 α`로 따라간다.
  밑장에 쓰는 커맨드는 모두 `CompleteInFlight`로 진행 중인 face_swap을 완주시킨다.
- 유니티 재생 확인은 소유자 몫으로 남아 있다.

**E2. `Kill(false)` 클레임 셋(char_visual_* · focus_on · screen_blur)은 같은 줄에서 두 번 걸면 앞 것이 되살아난다.**
- B가 A를 `Kill(false)`로 죽이면 A의 Commit이 불리지 않고 `HasClaimedTarget`이 true로 남는다. 둘 다 Step 수명에 있다.
- 경계에서 B가 아직 돌고 있으면: 둘이 같은 타깃에 가속 트윈을 만들고, 먼저 끝나는 쪽의 Commit이 `Kill(타깃, false)`로 상대를 죽인다 → **먼저 끝나는 쪽이 이긴다.**
- 경계 전에 B가 이미 끝났으면(줄을 B의 길이 이상 읽으면): B의 훅은 할 일이 없고, A의 훅이 B의 결과에서 A의 목표로 다시 트윈한다 → **앞 것(A)이 확정적으로 이긴다.**
  예: `<<char_visual_clear c1>> <<char_visual_dim c1>>` → 다음 줄에서 clear로 돌아간다. 폴드는 dim이다.
- 다른 줄에 나눠 쓰면 안전하다 — A의 훅은 앞 경계에서 이미 돌고 수명 목록에서 빠졌다.

**E3. 블러 채널(`RigVisualEffectController.BlurAmount`)에 쓰는 커맨드가 셋이고, 서로를 죽이지 않는다.**
- focus_on(타깃: 리그 레지스트리) · screen_blur(레이어 content root) · char_visual_*(리그 컨트롤러). `BLUR-DECISIONS.md`가 "동시에 걸면 싸운다, 한 번에 하나만 쓴다"로 인정해 둔 예외다.
- **동시가 아니어도 지운다.** char_visual은 프리셋의 `blur` 필드를 언제나 쓴다. 프리셋 DB에서 blur가 0이 아닌 것은 `defocus`(0.5)뿐이다.
  그래서 focus_on·screen_blur로 흐려 둔 리그에 dim · rim · silhouette · focus · clear 중 무엇을 걸어도 그 리그의 블러가 0이 된다.
  BLUR-DECISIONS.md는 이 순차 덮어쓰기를 다루지 않는다 — 의도인지 정해 둘 자리다.

**E4. motion은 노드 여러 개를 잡지만 확정은 한꺼번에 한다.**
- 다른 커맨드가 그중 한 노드를 `DOKill(true)`로 가져가도 motion의 `_claimed`는 true로 남는다.
- 나머지 노드가 끝날 때(또는 스텝 경계에서) `Commit`이 바인딩된 **모든** 노드에 종점을 다시 쓰고, 그 노드들의 장부 항목을 `Clear`한다.
- 가져간 커맨드가 먼저 끝났으면 그 최종값이 motion의 종점으로 덮이고, 아직 돌고 있으면 그 커맨드가 게시한 장부 항목이 지워진다. 노드가 하나인 motion은 문제없다.
- [추론] 실제로 겹치는지는 motions.json의 채널 노드와 다른 커맨드의 표적이 같은 노드인지에 달렸다.

**E5. HoldSignal과 RaiseSignal은 대본에서 쓸 수 없다.**
- 팩토리와 커맨드는 있지만 파서 표에 이름이 없다.
- Hold는 `CompleteImmediately`인데 `OnSkip`이 없다 → 압축할 때 신호를 소비하지 않는다.
- `SignalLatch.Clear`를 부르는 곳이 없다.

**E6. 쓰이지 않는 코드와 끊긴 포트.**
- sibling_* · char_to는 `ExecuteEvenIfSkipping`이라 `OnSkip`이 불리지 않는데 오버라이드하고 있다.
- `IRunScopedCommand` 구현이 0개다. 주석은 BGM을 예로 들지만 BGM은 AudioSystem을 거친다.
- `ui_patch` 커맨드는 지워졌는데(`bf42cdfe`) `IUIThemePatchPort` 인터페이스만 남아 있다. 구현도 사용처도 없다.
- face_swap `OnSkip`의 else 가지(`KillCurrentTween`)는 닿지 않는다 — `OnSkip`은 `ExecuteInner` 전에만 불려 `HasClaimedTarget`이 늘 false다.
- `StageMaskMotionCommand`: `_tween`은 대입만 되고 읽히지 않는다. `ExecuteInner` 안의 압축 검사는 `CommandBase`가 먼저 걸러 닿지 않는다.

**E7. 오디오는 scope 밖이다.** 보이스는 줄을 넘겨도 멈추지 않고 다음 보이스가 덮을 뿐이다.
정지(장면 경계·롤백)에서는 보이스·효과음을 끊는다(2026-11-03). 줄 경계에서 보이스를 끊을지는 따로 정할 일이다.

### 시스템

**S1. 무대 상태 모델 밖의 상태(화면 효과 · 스테이지 마스크 · 오디오)는 장면 경계에서 초기화되지 않는다.**
- `BeginScene`·`PrepareReplay`·`StopAsync`가 비우는 것은 리그·샷·scope뿐이다. `ScreenEffectRig.ResetToBaselineImmediate`는 `Initialize`에서 한 번만 불린다.
- 그래서 장면의 첫 vignette보다 앞 줄로 롤백하면 롤백 전의 vignette가 남는다. BGM·마스크도 같다. 진행 중이던 화면 효과 트윈도 정지에서 죽지 않는다.
- **뿌리는 "비워 둠"(R6)이 아니다.** 스텝 경계에서 가속하든 확정하든 종점은 같으니 상태는 남는다.
  스텝 경계에서 놔두는 것은 남은 트윈 시간이고, 장면 경계에서 놔두는 것은 종점 상태다.
- **화면 효과 — 해결.** vignette·noise는 장면 전역 의도, flash는 이벤트로 바꿨다. 정지(`StopAsync`)가 `ScreenEffectRig.ResetToBaselineImmediate`를 불러
  진행 중인 전환·플래시를 확정 없이 끊고 세기를 0으로 둔다. 리플레이가 화면 효과 커맨드를 다시 지나가며 표적 줄의 상태를 세운다.
- **스테이지 마스크 — 해결.** 무대(s1·s2·s3)를 어디까지·어떻게 보여줄지는 장면 전역 의도다. tx 모션은 `StageMaskSlot`이 소유하고,
  정지가 `IStageMaskProvider.ResetAllToBaseline`으로 모션을 끊고 기준 모양(FullRect 마스크 켜짐·오프셋 0 + 씬에 저장된 테두리)으로 돌린다.
- **오디오 — 해결.** BGM은 장면을 넘어 이어 가는 의도다(소유자 결정). `BeginScene`이 그때 흐르던 곡을 장면 머리 곡으로 기록하고,
  시크(롤백·로드) 동안은 `BgmPlayer.Hold`로 의도만 쌓았다가 `SeekState.Ended`에서 `Release`로 맞춘다.
  같은 곡이 흐르고 있으면 처음부터 다시 틀지 않는다. 보이스·효과음은 이벤트라 정지에서 끊는다.
  - 한계: 로드는 저장에 장면 머리 곡이 없어서, 로드 시점에 흐르던 곡을 출발점으로 삼는다. 앞 장면에서 이어 온 곡에 기대는 장면을 로드하면 다를 수 있다.
- 대사창 레이아웃(`surface_layout`의 `DialogueSurfaceState`)도 같은 부류였다 — 조립 때 한 번 만들어지고 `surface_reset`만 비웠다.
  **해결:** `BeginScene`·`PrepareReplay`가 무대와 함께 비운다. 리플레이 중에 Yarn이 `surface_*`를 다시 지나가며 표적 줄의 상태를 다시 세운다.

**S2. `pause` 중에 RapidSkip하면 앞 줄의 run이 다음 줄과 나란히 계속 돈다.**
- RapidSkip은 `cps_node_busy`를 통과한다. `PlaySpecs`는 `_runId`를 올리지 않고, 앞 토큰을 취소하지 않은 채 Dispose만 한 뒤 새 토큰을 같은 `scope.Token`에 넣는다.
  앞 run의 `isValid`(세대 비교)와 `WaitCommand`의 토큰 검사가 둘 다 통과한다.
- 결과:
  - 앞 줄 `pause` 뒤의 커맨드가 다음 줄의 커맨드보다 **나중에** 적용된다. 예: 앞 줄의 `fade_out c1`이 다음 줄의 `fade_in c1`을 덮는다.
  - 앞 run의 `finally`가 새 run의 `scope.Token`을 `None`으로, `_mainRoutine`을 null로 바꾸고 `CommandIsPlaying`을 false로 만든다.
    새 run에 전경 `pause`가 있으면 관문이 일찍 열린다.
- `Stop`은 `_runId`를 올리므로 이 문제가 없다. 줄 경계에만 세대 구분이 없다.
- **해결(2026-11-03)** — 셋을 함께:
  1. `WaitCommand`가 기다리는 도중 압축(RapidSkip·시크)이 켜지면 그 자리에서 끝난다. 줄의 남은 커맨드는 같은 프레임에 압축되어 끝 상태로 간다.
  2. 관문이 RapidSkip도 `cps_node_busy`로 막는다. 1 덕분에 기다림은 한 프레임이다 → 앞 줄의 실행이 끝난 뒤에야 다음 줄이 열린다.
  3. 실행기에 스텝 세대(`_stepId`)를 두어, `RunNode`의 `finally`는 마지막 스텝일 때만 Token·`_mainRoutine`·`CommandIsPlaying`을 정리한다(방어선).
  - 앞 실행을 취소하는 방식은 쓰지 않았다 — 앞 줄의 남은 커맨드가 아예 실행되지 않아 폴드·리플레이와 또 갈린다.

**S3. 시간 배율을 반영하는 트윈이 0개다.**
- `ScalePresentationDuration`을 부르는 곳이 없다. `scope.TimeScale`을 읽는 곳은 HoldSignal(대본에서 못 씀) 하나다.
- 속도 올리기가 실제로 빠르게 하는 것은 타자기뿐이다.

**S4. 서두르기는 실행기 밖에서 전역 시간을 바꾼다.**
- `LineHurrySpeedController.Enter`가 `DOTween.unscaledTimeScale = 30`을 걸고, 타자기가 끝나면 되돌린다.
- 그동안 앱 전체의 unscaled 트윈(UI 포함)이 30배로 돈다. 연출 트윈과 `pause`도 포함이다.
- 압축(R3)·스텝 경계(R6)와 별개인 네 번째 시간 경로다.

**S5. 해결 — 큐를 앞지르던 대사창 커맨드 셋(`box_hide` · `box_show` · `box_close`)을 지웠다.**
- 이 셋은 Yarn이 만나는 자리에서 바로 돌았고(`box_hide/show`는 `YarnTask`로 Yarn을 기다리게 함), 큐 커맨드는 다음 줄 머리에서 돈다.
  그래서 `<<fade_out c1>> <<box_hide>>`는 대사창이 먼저 사라졌다.
- 대사 없이 연출만 보이는 용도는 `#boxhide #auto` 줄이 맡는다. `box_close`는 `#boxfadein`, `box_show`는 다음 줄의 FadeIn이 대신한다.
- 잃은 것: 대사창을 다 걷은 뒤에 연출을 시작하는 순서(약 0.1초 차이), 줄 없이 대사창을 숨기는 것(예: 선택지 동안). 둘 다 쓰던 곳이 없었다.
- `surface_*`는 남는다 — 상태만 바꾸고 다음 줄에서 적용되므로 순서가 뒤집히지 않는다.

**S6. 잔재와 예외 경로.**
- `DecideCleanupPolicy`의 scope null 분기는 바로 다음 줄 `_activeScope.CleanupStep`에서 NRE가 난다. 방어가 아니다.
- 커맨드가 0개면 `PlayCollected`와 `PlaySpecs`가 둘 다 `CleanupStep`을 부른다(두 번째는 빈 목록이라 무해).
- `SequencePlayer` 머리 주석에 지금은 없는 `ticket.MarkCommandEntered()`가 남아 있다.
- 예외는 어디서도 고르게 잡히지 않는다:
  - 첫 `MoveNext`(= `Execute`의 머리, 압축 시 `OnSkip` 포함)는 `PlayCommands` 안에서 try 없이 돈다.
    [추론] 그래서 압축 중(RapidSkip · 시크 · 롤백/로드 재생) 한 커맨드의 `OnSkip`이 던지면 `RunNode`가 멈추고 그 줄의 남은 커맨드가 실행되지 않는다. 리플레이 상태가 갈리는 길이다.
  - `RunAfterFirstYield`의 try/catch는 바깥 `MoveNext`만 감싼다. `ExecuteInner` 본문은 Unity의 중첩 코루틴에서 돌아 여기서 잡히지 않는다.

## 7. 앞 판(`ec1a5d2`)과 달라진 곳

| 항목 | 앞 판 | 지금 |
|---|---|---|
| E1 face_swap | R5가 깨진 채 끝까지 둠 | 해결(`00534410`). α 축 충돌도 함께 닫음 |
| R6 "오버라이드하지 않음" | 규칙의 빈자리처럼 서술 | 화면상의 연출을 일부러 비워 둔 부류로 정리, 안전 조건 둘 |
| S1 | "E1과 같은 뿌리" | 별개. 모델 밖 상태를 리플레이가 초기화하지 않는 것이 뿌리 |
| E2 | 먼저 끝나는 쪽이 이김 | B가 이미 끝났으면 앞 것이 확정적으로 이김 |
| E3 | clear가 focus_on 블러를 지움 | defocus를 뺀 모든 char_visual 프리셋이 블러를 0으로 씀 |
| E4 | 종점을 다시 씀 | 장부 항목도 지움 |
| S2 | Token·CommandIsPlaying을 덮음 | `_mainRoutine`도 null로 덮음 → **해결**: pause가 압축에 반응, 관문이 RapidSkip도 막음, 스텝 세대 가드 |
| S5 | 즉시 실행 6개 | `debug_log` 삭제(`fc39ae36`), `box_hide/show/close` 삭제 — `surface_*` 둘만 남음(시점은 큐와 같음) |
| tx | `tx_<이름>` 18개, 커맨드가 그래픽 트윈을 쥠, 장면 경계 초기화 없음 | `tx <preset> [stage]` 하나(`StageCommandParser.Transition.cs:8`). 무대 마스크 = 장면 전역 의도 — 슬롯이 모션 소유, 정지에서 기준 모양으로 |
| ui_patch | 포트가 경고만 냄 | 커맨드 삭제(`bf42cdfe`), 인터페이스만 남음 |
| screen_blur | — | `reduction-boundary.md` 표가 "화면 효과(포스트 이펙트 축)"로 분류 — 지금은 리그 블러라 낡음 |
| 샷 | `Kill(rig, true)` 클레임, 정지의 `Clear`가 진행 중 트윈을 남김 | **장면 전역 의도 시범.** 시스템이 Intent·Presented를 나눠 들고 전환을 소유한다. 커맨드는 `Reduce(Intent)`를 넘기기만 한다. `Clear`가 전환까지 멈춘다 |
| 화면 효과 | 커맨드마다 자기 트윈, `KillTween(true)` 클레임, 장면 경계 초기화 없음 | vignette·noise = 장면 전역 의도(`IntentTransition`, 재조준), flash = 이벤트(컨트롤러가 재생 소유). 정지에서 기준으로 |
