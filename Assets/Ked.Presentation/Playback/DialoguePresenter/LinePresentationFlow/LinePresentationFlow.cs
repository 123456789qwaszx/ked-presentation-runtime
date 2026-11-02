using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public sealed class LinePresentationFlow
{
    public event Action LineDisplayed;

    private readonly YarnLineBoundary _yarnLineBoundary;
    private readonly LineDisplayState _lineDisplayState;
    private readonly SeekState _seekState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public LinePresentationFlow(
        YarnLineBoundary yarnLineBoundary,
        LineDisplayState lineDisplayState,
        SeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        YarnPlaybackDriver playbackDriver,
        LineHurrySpeedController lineHurrySpeed)
    {
        _yarnLineBoundary = yarnLineBoundary;
        _lineDisplayState = lineDisplayState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _playbackDriver = playbackDriver;
        _lineHurrySpeed = lineHurrySpeed;
    }

    public async YarnTask RunAsync(
        LinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, LinePresentationPhase.LineReceived);
        
        _lineDisplayState.MarkLineEntered();
        YarnLineMeta meta = _yarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        
        // Phase: LineRuntimeStateResolved
        ctx.EntrySeekKind = _seekState.Kind;
        _seekState.ObserveLine(meta);

        ctx.SeekDecision   = ResolveSeekDecision(ctx.EntrySeekKind);
        ctx.RecordDecision = LineRecordPolicy.Resolve(ctx.EntrySeekKind);
        SetPhase(ctx, LinePresentationPhase.LineRuntimeStateResolved);
        
        // Phase: LineEnteredCommitted
        _yarnLineBoundary.CommitLineEntered(meta, ctx.RecordDecision);
        _playbackDriver.PlayCollected();
        SetPhase(ctx, LinePresentationPhase.LineEnteredCommitted);
        
        if (ctx.SeekDecision == SeekLineDecision.SkipVisualAndDispatchSeekNext) {
            RunSeekPassThrough(ctx);
            return;
        }

        // #auto — 다 보이면 입력 없이 넘긴다(LineAdvanceTag). 되감기로 닿은 줄은 다시 보려고 온 것이라 넘기지 않는다.
        bool rollbackTarget = 
            ctx.SeekDecision == SeekLineDecision.TargetLineReached
            && ctx.EntrySeekKind == SeekKind.Rollback;

        if (ctx.SeekDecision == SeekLineDecision.TargetLineReached)
            _seekState.End(SeekEndReason.Arrived);

        _lineDisplayState.SetLineAutoAdvance(
            !rollbackTarget && LineAdvanceTag.IsAuto(ctx.Line.Metadata));

        // Phase: VisualRunStarted
        ctx.Run = beginRun();
        SetPhase(ctx, LinePresentationPhase.VisualRunStarted);

        DialogueBoxPresentationContext boxCtx = new(
            ctx.Line,
            ctx.Run,
            ctx.ShouldUseImmediateTransition);

        // Phase: BoxTransitioning -> BoxReady
        await _boxPresentation.ShowLineAsync(boxCtx);

        SetPhase(ctx, LinePresentationPhase.DialogueSurfaceResolved);

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: TypewriterReady
        ctx.LineText = _boxPresentation.LineTextTarget;
        _typewriter.SetTextView(ctx.LineText);

        ctx.Text = ctx.Line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(ctx.Text);
        SetPhase(ctx, LinePresentationPhase.TypewriterReady);
        
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

        SetPhase(ctx, LinePresentationPhase.TypewriterCompleted);

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterTypewriterAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: DisplayCommitted
        _lineDisplayState.MarkLineDisplayCompleted("normal");
        _typewriter.ContentWillDismiss();
        SetPhase(ctx, LinePresentationPhase.DisplayCommitted);

        LineDisplayed?.Invoke();

        // Phase: WaitingForAdvance -> Completed
        SetPhase(ctx, LinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, LinePresentationPhase.Completed);
    }
    
    private void RunSeekPassThrough(LinePresentationContext ctx)
    {
        SetPhase(ctx, LinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();
        _lineDisplayState.MarkLineDisplayCompleted("passThrough");
    }

    private async YarnTask CompleteStaleAfterBoxAsync(
        LinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, LinePresentationPhase.Stale);

        _boxPresentation.CleanupStale();
        _lineDisplayState.MarkLineTornDown("StaleAfterBox");

        SetPhase(ctx, LinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, LinePresentationPhase.Completed);
    }

    private async YarnTask CompleteStaleAfterTypewriterAsync(
        LinePresentationContext ctx,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, LinePresentationPhase.Stale);

        _typewriter.ContentWillDismiss();
        _lineDisplayState.MarkLineTornDown("StaleAfterTypewriter");

        SetPhase(ctx, LinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, LinePresentationPhase.Completed);
    }
    
    private SeekLineDecision ResolveSeekDecision(SeekKind entrySeekKind)
    {
        if (entrySeekKind == SeekKind.None)
            return SeekLineDecision.NotSeeking;

        return _seekState.LastObservedIsTarget
            ? SeekLineDecision.TargetLineReached
            : SeekLineDecision.SkipVisualAndDispatchSeekNext;
    }

    private void SetPhase(LinePresentationContext ctx, LinePresentationPhase phase)
    {
        ctx.Phase = phase;
    }
}