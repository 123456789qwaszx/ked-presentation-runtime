using System;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
// This class owns the execution order, but not the domain commit rules or seek decision rules.
// Domain commits are handled by VNLinePresentationCommitter.
// Seek decisions are handled by VNSeekLineResolver.
// CustomLinePresenter remains the owner of presenter lifetime, generation, and cancellation tokens.
public sealed class VNLinePresentationStateMachine
{
    private readonly VNLinePresentationCommitter _committer;
    private readonly VNSeekLineResolver _seekResolver;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly VNLoadSeekDriver _loadSeekDriver;

    public VNLinePresentationPhase CurrentPhase { get; private set; } = VNLinePresentationPhase.None;

    public VNLinePresentationStateMachine(
        VNLinePresentationCommitter committer,
        VNSeekLineResolver seekResolver,
        DialogueAdvanceDispatcher dispatcher,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        VNLoadSeekDriver loadSeekDriver = null)
    {
        _committer = committer;
        _seekResolver = seekResolver;
        _dispatcher = dispatcher;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _loadSeekDriver = loadSeekDriver;
    }

    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance,
        Func<bool> shouldFastForward)
    {
        // Phase: LineReceived -> LineEnteredCommitted
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);
        
        ctx.Meta = _committer.CommitLineEntered(ctx.Line, ctx.NodeName);
        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);
        
        // Phase: SeekResolved
        VNSeekLineDecision enteredDecision = _seekResolver.ResolveOnLineEntered(ctx.Meta);
        ctx.SeekDecision = enteredDecision;
        SetPhase(ctx, VNLinePresentationPhase.SeekResolved);
        
        if (enteredDecision.ShouldDispatchSeekNext) {
            await RunSeekPassThroughAsync(ctx, waitForAdvance);
            return;
        }
        
        VNSeekLineDecision presentationSeekDecision = _seekResolver.ResolveBeforePresentation(ctx.Line.TextID);
        ctx.SeekDecision = presentationSeekDecision;
        if (presentationSeekDecision.ShouldConsumeTargetLine) {
            _seekResolver.ConsumeTargetLine(ctx.Line.TextID);
            SetPhase(ctx, VNLinePresentationPhase.SeekTargetConsumed);

            if (presentationSeekDecision.SeekKind == VNSeekKind.Load)
                _loadSeekDriver?.Complete();
        }
        
        // Phase: VisualRunStarted
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);
        
        // Phase: BoxTransitioning -> BoxReady
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);
        bool useImmediateTransition = ctx.ShouldUseImmediateTransition || shouldFastForward();

        ctx.BoxResult = await _boxPresentation.ShowLineAsync(
            VNDialogueLineFactory.FromLocalizedLine(ctx.Line),
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
            .RunTypewriter(ctx.Text, ctx.Token.HurryUpToken).SuppressCancellationThrow();

        if (!ctx.Run.IsValid) {
            await CompleteStaleAfterTypewriterAsync(ctx, waitForAdvance); 
            return;
        }
        
        // Phase: DisplayCommitted
        _committer.CommitLineProcessingCompleted();
        _typewriter.ContentWillDismiss();

        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        // Phase: WaitingForAdvance -> Completed
        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }
    
    private async YarnTask RunSeekPassThroughAsync(VNLinePresentationContext ctx, Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.HideAllForSeek();
        _committer.CommitLineProcessingCompleted();

        _dispatcher.DispatchSeekNext();

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }
    
    private async YarnTask CompleteStaleAfterBoxAsync(VNLinePresentationContext ctx, Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

        _boxPresentation.CleanupStale(ctx.BoxResult);

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }
    
    private async YarnTask CompleteStaleAfterTypewriterAsync(VNLinePresentationContext ctx, Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, VNLinePresentationPhase.Stale);

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