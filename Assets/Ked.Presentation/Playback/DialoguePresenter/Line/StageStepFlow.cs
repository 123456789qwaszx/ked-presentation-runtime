using Yarn.Unity;

// 무대 스텝 — 대사 없이 쌓인 명령만 실행하는 스텝(<<step>> · 노드 끝). PLAN_stage-step-inline-markup.md §3.
// 줄이 아니다: 백로그 · 되감기 기준점 · 줄 좌표에 들지 않는다. 시크는 지나가는 줄의 일부로 압축 재생한다.
public sealed class StageStepFlow
{
    private readonly StepHead _stepHead;
    private readonly LineDisplayState _lineDisplayState;
    private readonly SeekState _seekState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly PlaybackRuntimeState _playbackState;
    private readonly ICommandRunScopeProvider _scopeProvider;

    public StageStepFlow(
        StepHead stepHead,
        LineDisplayState lineDisplayState,
        SeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        PlaybackRuntimeState playbackState,
        ICommandRunScopeProvider scopeProvider)
    {
        _stepHead = stepHead;
        _lineDisplayState = lineDisplayState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _playbackState = playbackState;
        _scopeProvider = scopeProvider;
    }

    // 기다릴 것이 없는 스텝은 그 자리에서 끝낸다. 끝냈으면 true.
    // - 쌓인 명령이 없다: 스텝이 아니다.
    // - 시크 중: 표적은 언제나 줄이므로 무대 스텝은 지나가는 것이다 — 명령만 압축 실행한다(시크 통과 줄과 같다).
    public bool TryPlayWithoutWaiting()
    {
        if (!_stepHead.HasPending)
            return true;

        if (!_seekState.IsSeeking)
            return false;

        _stepHead.Play(snap: true, seekPassThrough: true);
        return true;
    }

    // 명령을 실행하고 대사창을 닫은 뒤 전경(pause)이 끝날 때까지 기다린다. 넘김 입력은 필요 없다.
    // run이 무효가 되면(정지 · 되감기 · 대화 끝) 기다림을 놓는다.
    public async YarnTask RunAsync(LinePresentationRun run)
    {
        _lineDisplayState.MarkStageStepEntered();

        try
        {
            CommandRunScope scope = _scopeProvider.CurrentScope;
            bool rapidSkip = _playbackState.IsRapidSkipMode;

            _stepHead.Play(snap: rapidSkip, seekPassThrough: false);

            await _boxPresentation.HideForStageStepAsync(
                run,
                immediate: rapidSkip || _playbackState.IsSpeedUpMode);

            while (run.IsValid && IsForegroundPlaying(scope))
                await YarnTask.Yield();

            if (!run.IsValid)
                _boxPresentation.CleanupStale();
        }
        finally
        {
            _lineDisplayState.MarkStageStepExited();
        }
    }

    // 이 스텝을 연 세션이 살아 있고 그 전경이 아직 도는가. 세션이 끝났거나 바뀌었으면 기다릴 것이 없다.
    private bool IsForegroundPlaying(CommandRunScope scope)
    {
        return scope != null
               && ReferenceEquals(scope, _scopeProvider.CurrentScope)
               && !scope.IsCancelled
               && scope.CommandIsPlaying;
    }
}
