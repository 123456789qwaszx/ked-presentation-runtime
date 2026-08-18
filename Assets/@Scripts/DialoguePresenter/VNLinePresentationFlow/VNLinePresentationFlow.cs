using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public sealed class VNLinePresentationFlow
{
    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public VNLinePresentationPhase CurrentPhase { get; private set; } =
        VNLinePresentationPhase.None;

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        YarnPlaybackDriver playbackDriver,
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
            RunSeekPassThroughAsync(ctx);
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

    private void RunSeekPassThroughAsync(VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();
        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");
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