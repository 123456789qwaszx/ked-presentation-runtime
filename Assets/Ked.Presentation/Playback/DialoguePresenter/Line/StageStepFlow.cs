using Yarn.Unity;

// 글 없이 명령만 도는 구간 — 대사창을 닫고, 묶음마다 그 명령이 다 시작될 때까지(전경 — pause가 끝날 때까지) 기다린다.
//   줄의 앞 앵커(docs/PLAN_line-anchor.md A5 · A6)   줄이 도착한 뒤, 그 줄의 것으로. 옛 표기의 <<step>>이 닫은 구간과
//                                                     첫 글자 앞의 표지([beat] + <<at>>).
//   노드 끝의 암묵 스텝                               붙을 줄이 없이 남은 명령. 줄이 아니다 — 백로그 · 되감기 기준점 · 줄 좌표에 들지 않는다.
// PLAN_stage-step-inline-markup.md §3의 무대 스텝에서 왔다 — <<step>>은 이제 실행하지 않고 구간을 닫기만 한다.
public sealed class StageStepFlow
{
    private readonly StepHead _stepHead;
    private readonly InlineCommandTrack _inlineCommands;
    private readonly LineDisplayState _lineDisplayState;
    private readonly SeekState _seekState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly PlaybackRuntimeState _playbackState;
    private readonly ICommandRunScopeProvider _scopeProvider;

    public StageStepFlow(
        StepHead stepHead,
        InlineCommandTrack inlineCommands,
        LineDisplayState lineDisplayState,
        SeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        PlaybackRuntimeState playbackState,
        ICommandRunScopeProvider scopeProvider)
    {
        _stepHead = stepHead;
        _inlineCommands = inlineCommands;
        _lineDisplayState = lineDisplayState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _playbackState = playbackState;
        _scopeProvider = scopeProvider;
    }

    // ── 줄의 앞 앵커 ─────────────────────────────────────────────────────────

    // 이 줄에 앞 앵커가 있나. 줄에 들어선 뒤(StepHead.Enter · InlineCommandTrack.Begin)에 묻는다.
    public bool HasLinePrelude => _stepHead.HasPreBatch || _inlineCommands.HasPreAnchors;

    // 앞 앵커를 기다리지 않고 끝값으로 놓는다 — 시크가 지나가는 줄과 시크로 닿은 줄(A11).
    // 시크가 살아 있는 동안 부른다(명령이 압축된다). 장면 BGM은 의도만 쌓는다 — 머리가 맞춘다.
    public void PlayLinePreludeSnapped()
    {
        while (TryPlayNextPreAnchor(snap: true, seekPassThrough: true))
        {
        }
    }

    // 앞 앵커를 차례로 열고, 앵커마다 그 명령이 다 시작될 때까지 기다린다(AR3 ①). 대사창은 닫는다. 넘김 입력은 필요 없다.
    // 입력은 남은 앞 앵커를 서두른다(A12) — 끝값으로 놓이고, 그 줄의 도는 전환도 함께 마무리된다(A13). start부터는 제 속도다.
    // run이 무효가 되면(정지 · 되감기 · 대화 끝) 기다림을 놓는다 — 남은 것은 실행하지 않는다(정지가 버린다).
    public async YarnTask RunLinePreludeAsync(LinePresentationRun run)
    {
        _lineDisplayState.MarkLinePreludeEntered();

        try
        {
            CommandRunScope scope = _scopeProvider.CurrentScope;

            while (run.IsValid)
            {
                bool rapidSkip = _playbackState.IsRapidSkipMode;

                if (!TryPlayNextPreAnchor(snap: rapidSkip || _playbackState.IsStepHurried, seekPassThrough: false))
                    break;

                await _boxPresentation.HideForStageStepAsync(
                    run,
                    immediate: rapidSkip || _playbackState.IsSpeedUpMode);

                while (run.IsValid && IsForegroundPlaying(scope))
                    await YarnTask.Yield();
            }

            if (run.IsValid && _playbackState.IsStepHurried)
            {
                // 지금까지는 다음 스텝의 경계가 남은 전환을 마무리했다 — 줄 안에는 그 경계가 없으므로 여기서 한다.
                scope?.CleanupStep(CleanupPolicy.Finish);
                _playbackState.EndStepHurry();
            }
        }
        finally
        {
            _lineDisplayState.MarkLinePreludeExited();
        }
    }

    // 다음 앞 앵커 — 옛 표기의 <<step>>이 닫은 구간(쓴 순서), 그 뒤에 첫 글자 앞의 표지(글의 표지 순서).
    // 접힘과 같은 차례다(LineBeatSchedule.BuildAnchors).
    private bool TryPlayNextPreAnchor(bool snap, bool seekPassThrough)
    {
        if (_stepHead.TryPlayNextPreBatch(snap, seekPassThrough))
            return true;

        if (!_inlineCommands.TryPlayNextPreAnchor())
            return false;

        _stepHead.Settle(snap, seekPassThrough);
        return true;
    }

    // ── 노드 끝의 암묵 스텝 ─────────────────────────────────────────────────

    // 기다릴 것이 없는 스텝은 그 자리에서 끝낸다. 끝냈으면 true.
    // - 쌓인 명령이 없다: 스텝이 아니다.
    // - 시크 중: 표적은 언제나 줄이므로 지나가는 것이다 — 명령만 압축 실행한다(시크 통과 줄과 같다).
    public bool TryPlayWithoutWaiting()
    {
        if (!_stepHead.HasPending)
            return true;

        if (!_seekState.IsSeeking)
            return false;

        while (TryPlayNextTailBatch(snap: true, seekPassThrough: true))
        {
        }

        return true;
    }

    // 남은 명령을 실행하고 대사창을 닫은 뒤 전경(pause)이 끝날 때까지 기다린다. 넘김 입력은 필요 없다.
    // <<step>>이 닫은 구간이 있으면 구간마다 따로 — 줄이 아니라서 구간마다 실행기의 스텝이다(남은 전환을 마무리한다).
    // run이 무효가 되면(정지 · 되감기 · 대화 끝) 기다림을 놓는다.
    public async YarnTask RunAsync(LinePresentationRun run)
    {
        _lineDisplayState.MarkStageStepEntered();

        try
        {
            CommandRunScope scope = _scopeProvider.CurrentScope;

            while (run.IsValid)
            {
                bool rapidSkip = _playbackState.IsRapidSkipMode;

                if (!TryPlayNextTailBatch(snap: rapidSkip, seekPassThrough: false))
                    break;

                await _boxPresentation.HideForStageStepAsync(
                    run,
                    immediate: rapidSkip || _playbackState.IsSpeedUpMode);

                while (run.IsValid && IsForegroundPlaying(scope))
                    await YarnTask.Yield();
            }

            if (!run.IsValid)
                _boxPresentation.CleanupStale();
        }
        finally
        {
            _lineDisplayState.MarkStageStepExited();
        }
    }

    // 다음 묶음 — <<step>>이 닫은 구간을 쓴 순서로 하나씩, 마지막에 남은 명령과 붙을 줄이 없던 <<at>>(AR10).
    private bool TryPlayNextTailBatch(bool snap, bool seekPassThrough)
    {
        if (!_stepHead.HasPending)
            return false;

        _stepHead.Enter();

        if (_stepHead.TryPlayNextPreBatch(snap, seekPassThrough))
            return true;

        _stepHead.Play(snap, seekPassThrough);
        _inlineCommands.PlayStrayAt();
        return true;
    }

    // 이 구간을 연 세션이 살아 있고 그 전경이 아직 도는가. 세션이 끝났거나 바뀌었으면 기다릴 것이 없다.
    private bool IsForegroundPlaying(CommandRunScope scope)
    {
        return scope != null
               && ReferenceEquals(scope, _scopeProvider.CurrentScope)
               && !scope.IsCancelled
               && scope.CommandIsPlaying;
    }
}
