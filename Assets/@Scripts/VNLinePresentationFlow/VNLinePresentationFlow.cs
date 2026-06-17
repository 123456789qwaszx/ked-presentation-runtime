using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public sealed class VNLinePresentationFlow
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
        Func<LinePresentationRun> beginRun)
    {
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);

        _advanceState.MarkLineEntered();
        ctx.Meta = _vnYarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        _vnYarnLineBoundary.CommitLineEntered(ctx.Meta);

        // Capture the forward-settle baseline BEFORE dispatching,
        // then expect exactly subAdvanceCount more sub-beat settles.
        // Main waits for those before any visual, so sub holds (wait=true beats) are respected.
        int forwardSettleBaseline = _sideRunnerSyncHub.ForwardSettleEpoch;

        // int subAdvanceCount = _advanceState.IsSeekingActive
        //     ? _sideRunnerSyncHub.ConsumePresentationSeekResyncCount()   // 시크: base 재동기화
        //     : _sideRunnerSyncHub.ConsumePresentationAutoAdvanceCount(); // 정방향: hold/extra/suppress 적용
        
        int subAdvanceCount = _sideRunnerSyncHub.ConsumePresentationAutoAdvanceCount();

        for (int i = 0; i < subAdvanceCount; i++)
            _playbackDriver.Enqueue(new SubPresentationAdvanceCommandSpec());

        ctx.CommandTicket = _playbackDriver.PlayCollected();

        int forwardSettleTarget = forwardSettleBaseline + subAdvanceCount;

        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);

        // Phase: LineRuntimeStateResolved (seek decision)
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
            return LineEntryOutcome.PassedThrough;
        }

        // Phase: ResumePolicyResolved
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
        else presentationSeekDecision = VNSeekLineDecision.NotSeeking();

        ctx.SeekDecision = presentationSeekDecision;
        SetPhase(ctx, VNLinePresentationPhase.ResumePolicyResolved);

        // Forward path: respect sub holds before any visual work. Breaks early if the sub
        // lane cannot produce the expected settles (completed / paused / line cancelled).
        await _sideRunnerSyncHub.WaitUntilForwardSettledAsync(
            forwardSettleTarget,
            ctx.Token.NextContentToken);

        // Phase: VisualRunStarted
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
        LineEntryOutcome outcome = await EnterLineAndResolveSeekAsync(ctx, beginRun);
        if (outcome == LineEntryOutcome.PassedThrough)
            return;

        // Phase: BoxTransitioning -> BoxReady
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);
        bool useImmediateTransition = ctx.ShouldUseImmediateTransition || shouldFastForward();

        ctx.BoxResult = await _boxPresentation.ShowLineAsync(
            VNDialogueLine.FromLocalizedLine(ctx.Line),
            new DialogueBoxPresentationOptions {
                IsSeekTargetLine = ctx.IsPendingSeekTargetLine,
                UseImmediateTransition = useImmediateTransition,
                Run = ctx.Run,
            });
        SetPhase(ctx, VNLinePresentationPhase.BoxReady);

        if (!ctx.Run.IsValid || !ctx.BoxResult.IsValid) {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: TypewriterReady
        ctx.LineText = ctx.BoxResult?.LineText;
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

    // ---- Staging-only beat ----
    // No dialogue box / typewriter. The line still commits meta (backlog + rollback) and
    // consumes a sub advance via the shared front-matter. It auto-advances once its own
    // staging (this line's wait=true commands) and the dispatched sub beat have settled.
    // A #stay marker keeps it on screen waiting for player advance instead of auto-advancing.
    public async YarnTask RunPresentationBeatAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        LineEntryOutcome outcome = await EnterLineAndResolveSeekAsync(ctx, beginRun);
        if (outcome == LineEntryOutcome.PassedThrough)
            return;

        // Box stays hidden for a beat. Hide everything coherently (resets the controller's
        // box state), so the next real dialogue line fades a box back in cleanly.
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);
        _boxPresentation.CloseAll();
        _typewriter.SetTextView(null);
        SetPhase(ctx, VNLinePresentationPhase.BoxReady);

        // Wait for this line's own staging to finish. For wait=true commands, entry-close
        // == completion; for fire-and-forget commands this returns ~immediately.
        await WaitUntilCommandTicketSettledAsync(ctx);

        if (!ctx.Run.IsValid) {
            _advanceState.MarkLineDisplayCompleted(ctx.Meta, "beatStale");
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            SetPhase(ctx, VNLinePresentationPhase.Completed);
            return;
        }

        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "beat");
        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        if (DialogueBoxMetadataResolver.IsBeatStay(ctx.Line.Metadata))
            await waitForAdvance(ctx.Token);
        // else: auto-advance by simply returning (the runner proceeds to the next content).

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask WaitUntilCommandTicketSettledAsync(VNLinePresentationContext ctx)
    {
        CommandRunTicket ticket = ctx.CommandTicket;
        if (ticket == null)
            return;

        while (!ticket.EntryClosed)
        {
            if (ctx.Token.NextContentToken.IsCancellationRequested)
                return;

            if (ctx.Run != null && !ctx.Run.IsValid)
                return;

            await YarnTask.Yield();
        }
    }

    private async YarnTask RunSeekPassThroughAsync(
        VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.HideAllForSeek();
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await _sideRunnerSyncHub.WaitUntilPresentationLaneReadyAsync();

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