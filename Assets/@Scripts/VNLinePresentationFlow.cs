using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public partial class VNLinePresentationFlow
{
    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly VNLoadSeekDriver _loadSeekDriver;
    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly YarnBridgePlaybackDriver _playbackDriver;

    public VNLinePresentationPhase CurrentPhase { get; private set; } = VNLinePresentationPhase.None;

    private enum LineEntryOutcome
    {
        Proceed,
        PassedThrough,
    }

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        VNLoadSeekDriver loadSeekDriver,
        VNSideRunnerSyncHub vnSideRunnerSyncHub,
        YarnBridgePlaybackDriver playbackDriver
        )
    {
        _vnYarnLineBoundary = vnYarnLineBoundary;
        _advanceState = advanceState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _loadSeekDriver = loadSeekDriver;
        _sideRunnerSyncHub = vnSideRunnerSyncHub;
        _playbackDriver = playbackDriver;
    }

    private async YarnTask<LineEntryOutcome> EnterLineAndResolveSeekAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        bool recordToHistory)
    {
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);

        _advanceState.MarkLineEntered();
        ctx.Meta = _vnYarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        _vnYarnLineBoundary.CommitLineEntered(ctx.Meta, recordToHistory);

        bool isSeekResync = _advanceState.IsSeekingActive;

        ctx.CommandTicket = _playbackDriver.PlayCollected();

        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);

        VNSeekLineDecision enteredDecision;

        if (_advanceState.IsSeekingActive) {
            VNSeekKind seekKind = _advanceState.SeekKind;

            enteredDecision = _advanceState.IsSeekTargetLine(ctx.Meta)
                ? VNSeekLineDecision.TargetLineReachedAndResumePresentation(seekKind)
                : VNSeekLineDecision.SkipVisualAndDispatchSeekNext(seekKind);
        }
        else {
            enteredDecision = VNSeekLineDecision.NotSeeking();
        }

        ctx.SeekDecision = enteredDecision;
        SetPhase(ctx, VNLinePresentationPhase.LineRuntimeStateResolved);

        SyncGateRunResult syncResult = isSeekResync
            ? await _sideRunnerSyncHub.RunSeekResyncGatePlanAsync(ctx.Token.NextContentToken)
            : await _sideRunnerSyncHub.RunForwardSyncGatePlanAsync(ctx.Token.NextContentToken);

        if (syncResult == SyncGateRunResult.Cancelled ||
            syncResult == SyncGateRunResult.Superseded) {
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            return LineEntryOutcome.PassedThrough;
        }

        if (syncResult == SyncGateRunResult.LanePaused ||
            syncResult == SyncGateRunResult.LaneCompleted ||
            syncResult == SyncGateRunResult.LaneUnavailable) {
            // WaitUntilForwardSettledAsync의 "기다릴 수 없으면 빠져나온다"
        }

        if (ctx.ShouldSkipVisual) {
            await RunSeekPassThroughAsync(ctx);
            return LineEntryOutcome.PassedThrough;
        }

        VNSeekLineDecision presentationSeekDecision;

        if (ctx.IsPendingSeekTargetLine) {
            VNSeekKind seekKind = _advanceState.SeekKind;
            _advanceState.ClearSeek();

            presentationSeekDecision = seekKind == VNSeekKind.Rollback
                ? VNSeekLineDecision.TargetLineVisualResumeImmediate(seekKind)
                : VNSeekLineDecision.TargetLineVisualResumeNormal(seekKind);

            if (seekKind == VNSeekKind.Load)
                _loadSeekDriver?.Complete();
        }
        else {
            presentationSeekDecision = VNSeekLineDecision.NotSeeking();
        }

        ctx.SeekDecision = presentationSeekDecision;
        SetPhase(ctx, VNLinePresentationPhase.ResumePolicyResolved);

        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        return LineEntryOutcome.Proceed;
    }

    // ---- Normal dialogue line ----
    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance,
        Func<bool> shouldFastForward)
    {
        // 일반 대사 라인: backlog + rollback에 기록한다.
        LineEntryOutcome outcome = await EnterLineAndResolveSeekAsync(
            ctx, beginRun, recordToHistory: true);
        if (outcome == LineEntryOutcome.PassedThrough)
            return;

        // Phase: BoxTransitioning -> BoxReady
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);

        DialogueBoxPresentationContext boxCtx = new(
            ctx.Line,
            ctx.Run,
            useImmediateTransition: ctx.ShouldUseImmediateTransition || shouldFastForward());

        ctx.BoxResult = await _boxPresentation.ShowLineAsync(boxCtx);
        SetPhase(ctx, VNLinePresentationPhase.BoxReady);

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
        await _typewriter
            .RunTypewriter(ctx.Text, ctx.Token.HurryUpToken)
            .SuppressCancellationThrow();
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

    private async YarnTask RunSeekPassThroughAsync(
        VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);

        // EnterLineAndResolveSeekAsync에서 이미 seek resync plan을 실행함.
        // 여기서는 별도 ready wait를 반복하지 않음.
        await YarnTask.Yield();

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask CompleteStaleAfterBoxAsync(
        VNLinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

        _boxPresentation.CleanupStale(ctx.BoxResult);
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "StaleAfterBox");

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
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "StaleAfterTypewriter");

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