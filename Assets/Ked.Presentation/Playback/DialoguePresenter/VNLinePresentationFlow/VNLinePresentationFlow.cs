using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public sealed class VNLinePresentationFlow
{
    public event Action LineDisplayed;

    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly VNSeekState _seekState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        VNSeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        YarnPlaybackDriver playbackDriver,
        LineHurrySpeedController lineHurrySpeed)
    {
        _vnYarnLineBoundary = vnYarnLineBoundary;
        _advanceState = advanceState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _playbackDriver = playbackDriver;
        _lineHurrySpeed = lineHurrySpeed;
    }

    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);
        
        _advanceState.MarkLineEntered();
        YarnLineMeta meta = _vnYarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        
        // Phase: LineRuntimeStateResolved
        ctx.EntrySeekKind = _seekState.Kind;
        _seekState.ObserveLine(meta);

        ctx.SeekDecision   = ResolveSeekDecision(ctx.EntrySeekKind);
        ctx.RecordDecision = VNLineRecordPolicy.Resolve(ctx.EntrySeekKind);
        SetPhase(ctx, VNLinePresentationPhase.LineRuntimeStateResolved);
        
        // Phase: LineEnteredCommitted
        _vnYarnLineBoundary.CommitLineEntered(meta, ctx.RecordDecision);
        _playbackDriver.PlayCollected();
        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);
        
        if (ctx.SeekDecision == VNSeekLineDecision.SkipVisualAndDispatchSeekNext) {
            RunSeekPassThrough(ctx);
            return;
        }

        // #auto — 다 보이면 입력 없이 넘긴다(VNLineAdvanceTag). 되감기로 닿은 줄은 다시 보려고 온 것이라 넘기지 않는다.
        bool rollbackTarget = 
            ctx.SeekDecision == VNSeekLineDecision.TargetLineReached
            && ctx.EntrySeekKind == VNSeekKind.Rollback;

        if (ctx.SeekDecision == VNSeekLineDecision.TargetLineReached)
            _seekState.End(VNSeekEndReason.Arrived);

        _advanceState.SetLineAutoAdvance(
            !rollbackTarget && VNLineAdvanceTag.IsAuto(ctx.Line.Metadata));

        // Phase: VisualRunStarted
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        DialogueBoxPresentationContext boxCtx = new(
            ctx.Line,
            ctx.Run,
            ctx.ShouldUseImmediateTransition);

        // Phase: BoxTransitioning -> BoxReady
        await _boxPresentation.ShowLineAsync(boxCtx);

        SetPhase(ctx, VNLinePresentationPhase.DialogueSurfaceResolved);

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: TypewriterReady
        ctx.LineText = _boxPresentation.LineTextTarget;
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
        _advanceState.MarkLineDisplayCompleted("normal");
        _typewriter.ContentWillDismiss();
        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        LineDisplayed?.Invoke();

        // Phase: WaitingForAdvance -> Completed
        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }
    
    private void RunSeekPassThrough(VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();
        _advanceState.MarkLineDisplayCompleted("passThrough");
    }

    private async YarnTask CompleteStaleAfterBoxAsync(
        VNLinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

        _boxPresentation.CleanupStale();
        _advanceState.MarkLineTornDown("StaleAfterBox");

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
        _advanceState.MarkLineTornDown("StaleAfterTypewriter");

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }
    
    private VNSeekLineDecision ResolveSeekDecision(VNSeekKind entrySeekKind)
    {
        if (entrySeekKind == VNSeekKind.None)
            return VNSeekLineDecision.NotSeeking;

        return _seekState.LastObservedIsTarget
            ? VNSeekLineDecision.TargetLineReached
            : VNSeekLineDecision.SkipVisualAndDispatchSeekNext;
    }

    private void SetPhase(VNLinePresentationContext ctx, VNLinePresentationPhase phase)
    {
        ctx.Phase = phase;
    }
}