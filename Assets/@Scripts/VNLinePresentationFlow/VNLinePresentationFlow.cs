using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
// This class owns the transaction order, seek decision flow, but not the domain commit rules or presenter lifetime.
// Domain commits are handled by VNLineEntryCommitter.
// CustomLinePresenter remains the owner of presenter lifetime, generation, and cancellation tokens.
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

        // 자동 sub advance: 수집 spec으로 emit하므로 OnRollbackSeek 롤백 시 서브 재동기화. RunAsync(메인 전용)에서만 emit → runaway 없음.
        int subAdvanceCount = _advanceState.IsSeekingActive
            ? _sideRunnerSyncHub.ConsumePresentationSeekResyncCount()   // 시크: base 재동기화
            : _sideRunnerSyncHub.ConsumePresentationAutoAdvanceCount(); // 정방향: hold/extra/suppress 적용

        for (int i = 0; i < subAdvanceCount; i++)
            _playbackDriver.Enqueue(new SubPresentationAdvanceCommandSpec());

        _playbackDriver.PlayCollected();
        
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

            presentationSeekDecision = seekKind == VNSeekKind.Rollback
                ? VNSeekLineDecision.TargetLineVisualResumeImmediate(seekKind)
                : VNSeekLineDecision.TargetLineVisualResumeNormal(seekKind);

            if (seekKind == VNSeekKind.Load)
                _loadSeekDriver?.Complete();
        }
        else presentationSeekDecision = VNSeekLineDecision.NotSeeking();

        ctx.SeekDecision = presentationSeekDecision;
        SetPhase(ctx, VNLinePresentationPhase.ResumePolicyResolved);
        
        // Phase: VisualRunStarted
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

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