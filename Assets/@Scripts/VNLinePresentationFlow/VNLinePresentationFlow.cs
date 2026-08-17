using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public sealed class VNLinePresentationFlow
{
    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public VNLinePresentationPhase CurrentPhase { get; private set; } =
        VNLinePresentationPhase.None;

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        YarnBridgePlaybackDriver playbackDriver,
        LineHurrySpeedController lineHurrySpeed)
    {
        _vnYarnLineBoundary = vnYarnLineBoundary;
        _advanceState = advanceState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _playbackDriver = playbackDriver;
        _lineHurrySpeed = lineHurrySpeed;
    }

    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance,
        Func<bool> shouldFastForward)
    {
        // Phase: LineReceived -> LineEnteredCommitted
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);

        _advanceState.MarkLineEntered();
        ctx.Meta = _vnYarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        _vnYarnLineBoundary.CommitLineEntered(ctx.Meta);

        ctx.CommandTicket = _playbackDriver.PlayCollected();
        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);

        // Phase: LineRuntimeStateResolved
        VNSeekLineDecision enteredDecision;
        
        if (_advanceState.IsSeekingActive) {
            VNSeekKind seekKind = _advanceState.SeekKind;

            enteredDecision = _advanceState.IsSeekTargetLine(ctx.Meta)
                ? VNSeekLineDecision.TargetLineReachedAndResumePresentation(seekKind)
                : VNSeekLineDecision.SkipVisualAndDispatchSeekNext(seekKind);
        }
        else enteredDecision = VNSeekLineDecision.NotSeeking();
        
        ctx.SeekDecision = enteredDecision;
        SetPhase(ctx, VNLinePresentationPhase.LineRuntimeStateResolved);

        if (ctx.ShouldSkipVisual) {
            await RunSeekPassThroughAsync(ctx);
            return;
        }

        // Phase: ResumePolicyResolved
        VNSeekLineDecision presentationSeekDecision;
        
        if (ctx.IsPendingSeekTargetLine) {
            VNSeekKind seekKind = _advanceState.SeekKind;
            _advanceState.ClearSeek();

            presentationSeekDecision = shouldFastForward()
                ? VNSeekLineDecision.TargetLineVisualResumeImmediate(seekKind)
                : VNSeekLineDecision.TargetLineVisualResumeNormal(seekKind);
        }
        else presentationSeekDecision = VNSeekLineDecision.NotSeeking();
        
        ctx.SeekDecision = presentationSeekDecision;
        SetPhase(ctx, VNLinePresentationPhase.ResumePolicyResolved);

        // Phase: VisualRunStarted
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        DialogueBoxPresentationContext boxCtx = new(
            ctx.Line,
            ctx.Run,
            ctx.ShouldUseImmediateTransition);

        // Phase: BoxTransitioning -> BoxReady
        ctx.BoxResult = await _boxPresentation.ShowLineAsync(boxCtx);
        SetPhase(ctx, VNLinePresentationPhase.DialogueSurfaceResolved);

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: TypewriterReady
        ctx.LineText = ctx.BoxResult.NextBox.GetLineText();
        _typewriter.SetTextView(ctx.LineText);

        ctx.Text = ctx.Line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(ctx.Text);
        SetPhase(ctx, VNLinePresentationPhase.TypewriterReady);
        
        // Phase: TypewriterCompleted
        try
        {
            await _typewriter
                .RunTypewriter(
                    ctx.Text,
                    ctx.Token.HurryUpToken,
                    ctx.Token.NextContentToken,
                    _lineHurrySpeed.Enter)
                .SuppressCancellationThrow();
        }
        finally
        {
            _lineHurrySpeed.Exit();
        }

        SetPhase(ctx, VNLinePresentationPhase.TypewriterCompleted);

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterTypewriterAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: DisplayCommitted
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "normal");
        _typewriter.ContentWillDismiss();
        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        // Phase: WaitingForAdvance -> Completed
        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    // 통과 라인은 반드시 한 프레임을 양보.
    // 동기로 완료할 시, 버그 유발
    // (1) Yarn의 RunLocalisedLine이 WhenAll을 이미 완료된 것으로 처리,
    // (2) SignalContentComplete() 분기를 탄 뒤,
    // (3) OnLineReceivedAsync가 Dialogue.Continue() 호출.
    // (4) 그 Continue가 VM 실행 중에 재진입.
    // (5) 시크 전체가 한 프레임 안에서 재귀로 돌아감.
    private async YarnTask RunSeekPassThroughAsync(
        VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");

        await YarnTask.Yield();
    }

    private async YarnTask CompleteStaleAfterBoxAsync(
        VNLinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

        _boxPresentation.CleanupStale(ctx.BoxResult);
        _advanceState.MarkLineTornDown(ctx.Meta, "StaleAfterBox");

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask CompleteStaleAfterTypewriterAsync(
        VNLinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

        _typewriter.ContentWillDismiss();
        _advanceState.MarkLineTornDown(ctx.Meta, "StaleAfterTypewriter");

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private void SetPhase(VNLinePresentationContext ctx, VNLinePresentationPhase phase)
    {
        ctx.Phase = phase;
        CurrentPhase = phase;
    }
}