using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
// This class owns the transaction order, seek decision flow, but not the domain commit rules or presenter lifetime.
// Domain commits are handled by VNLinePresentationCommitter.
// CustomLinePresenter remains the owner of presenter lifetime, generation, and cancellation tokens.
public sealed class VNLinePresentationStateMachine
{
    private readonly VNLineEntryCommitter _vnLineEntryCommitter;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly VNLoadSeekDriver _loadSeekDriver;
    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;

    public VNLinePresentationPhase CurrentPhase { get; private set; } = VNLinePresentationPhase.None;

    public VNLinePresentationStateMachine(
        VNLineEntryCommitter vnLineEntryCommitter,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        VNLoadSeekDriver loadSeekDriver,
        VNSideRunnerSyncHub vnSideRunnerSyncHub)
    {
        _vnLineEntryCommitter = vnLineEntryCommitter;
        _advanceState = advanceState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _loadSeekDriver = loadSeekDriver;
        _sideRunnerSyncHub = vnSideRunnerSyncHub;
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
        ctx.Meta = _vnLineEntryCommitter.BuildLineMeta(ctx.Line, ctx.NodeName);
        _vnLineEntryCommitter.CommitLineEntered(ctx.Meta);
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
            await RunSeekPassThroughAsync(ctx, waitForAdvance);
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

        if (!ctx.HasValidRun) {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        // Phase: TypewriterRunning
        ctx.LineText = ctx.BoxResult?.LineText;
        _typewriter.SetTextView(ctx.LineText);

        ctx.Text = ctx.Line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(ctx.Text);
        
        SetPhase(ctx, VNLinePresentationPhase.TypewriterRunning);

        await _typewriter
            .RunTypewriter(ctx.Text, ctx.Token.HurryUpToken)
            .SuppressCancellationThrow();

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
        VNLinePresentationContext ctx, 
        Func<LineCancellationToken, YarnTask> waitForAdvance)
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