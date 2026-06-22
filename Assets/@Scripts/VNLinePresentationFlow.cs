using System;
using System.Threading;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public partial class VNLinePresentationFlow : IInlinePresentationAdvanceHost
{
    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly VNLoadSeekDriver _loadSeekDriver;
    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;
    private readonly YarnBridgePlaybackDriver _playbackDriver;

    public VNLinePresentationPhase CurrentPhase { get; private set; } =
        VNLinePresentationPhase.None;

    // ---- inline [advance/] host ----
    private LinePresentationRun _inlineAdvanceRun;
    private CancellationToken _inlineAdvanceCancel;
    private bool _inlineAdvanceRunValid;

    public IInlinePresentationAdvanceHost InlineAdvanceHost => this;

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        VNLoadSeekDriver loadSeekDriver,
        VNSideRunnerSyncHub vnSideRunnerSyncHub,
        YarnBridgePlaybackDriver playbackDriver)
    {
        _vnYarnLineBoundary = vnYarnLineBoundary;
        _advanceState = advanceState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _loadSeekDriver = loadSeekDriver;
        _sideRunnerSyncHub = vnSideRunnerSyncHub;
        _playbackDriver = playbackDriver;
    }

    private async YarnTask<bool> TryEnterLineAndResolveSeekAsync(
        VNLinePresentationContext ctx,
        bool recordToHistory)
    {
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);

        _advanceState.MarkLineEntered();
        ctx.Meta = _vnYarnLineBoundary.BuildLineMeta(ctx.Line, ctx.NodeName);
        _vnYarnLineBoundary.CommitLineEntered(ctx.Meta, recordToHistory);

        ctx.CommandTicket = _playbackDriver.PlayCollected();
        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);

        bool isSeekingActive = _advanceState.IsSeekingActive;
        VNSeekKind seekKind = default;

        if (isSeekingActive)
            seekKind = _advanceState.SeekKind;

        if (isSeekingActive)
        {
            ctx.SeekDecision = _advanceState.IsSeekTargetLine(ctx.Meta)
                ? VNSeekLineDecision.TargetLineReachedAndResumePresentation(seekKind)
                : VNSeekLineDecision.SkipVisualAndDispatchSeekNext(seekKind);
        }
        else
        {
            ctx.SeekDecision = VNSeekLineDecision.NotSeeking();
        }

        SetPhase(ctx, VNLinePresentationPhase.LineRuntimeStateResolved);

        SyncGateRunResult syncResult;
        if (isSeekingActive)
        {
            syncResult = await _sideRunnerSyncHub
                .RunSeekResyncGatePlanAsync(ctx.Token.NextContentToken);
        }
        else
        {
            syncResult = await _sideRunnerSyncHub
                .RunForwardSyncGatePlanAsync(ctx.Token.NextContentToken);
        }

        if (syncResult == SyncGateRunResult.Cancelled)
        {
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            return false;
        }

        if (ctx.ShouldSkipVisual)
        {
            await RunSeekPassThroughAsync(ctx);
            return false;
        }

        if (ctx.IsPendingSeekTargetLine)
        {
            _advanceState.ClearSeek();

            ctx.SeekDecision = seekKind == VNSeekKind.Rollback
                ? VNSeekLineDecision.TargetLineVisualResumeImmediate(seekKind)
                : VNSeekLineDecision.TargetLineVisualResumeNormal(seekKind);

            if (seekKind == VNSeekKind.Load)
                _loadSeekDriver?.Complete();
        }
        else
        {
            ctx.SeekDecision = VNSeekLineDecision.NotSeeking();
        }

        SetPhase(ctx, VNLinePresentationPhase.ResumePolicyResolved);

        return true;
    }

    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance,
        Func<bool> shouldFastForward)
    {
        if (!await TryEnterLineAndResolveSeekAsync(ctx, recordToHistory: true))
            return;

        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        DialogueBoxPresentationContext boxCtx = new(
            ctx.Line,
            ctx.Run,
            useImmediateTransition:
                ctx.ShouldUseImmediateTransition || shouldFastForward());

        ctx.BoxResult = await _boxPresentation.ShowLineAsync(boxCtx);
        SetPhase(ctx, VNLinePresentationPhase.DialogueSurfaceResolved);

        if (!ctx.Run.IsValid)
        {
            await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
            return;
        }

        ctx.LineText = ctx.BoxResult.NextBox.GetLineText();
        _typewriter.SetTextView(ctx.LineText);

        ctx.Text = ctx.Line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(ctx.Text);
        SetPhase(ctx, VNLinePresentationPhase.TypewriterReady);

        BeginInlineAdvance(ctx.Run, ctx.Token.NextContentToken);
        try
        {
            await _typewriter
                .RunTypewriter(ctx.Text, ctx.Token.HurryUpToken, ctx.Token.NextContentToken)
                .SuppressCancellationThrow();
        }
        finally
        {
            EndInlineAdvance();
        }

        SetPhase(ctx, VNLinePresentationPhase.TypewriterCompleted);

        if (!ctx.Run.IsValid)
        {
            await CompleteStaleAfterTypewriterAsync(ctx, waitForAdvance);
            return;
        }

        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "normal");
        _typewriter.ContentWillDismiss();
        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask RunSeekPassThroughAsync(VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();

        SyncGateRunResult inlineResult =
            await DrainInlineAdvancesForSeekPassThroughAsync(ctx);

        if (inlineResult == SyncGateRunResult.Cancelled)
        {
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            return;
        }

        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask<SyncGateRunResult> DrainInlineAdvancesForSeekPassThroughAsync(
        VNLinePresentationContext ctx)
    {
        InlineAdvanceManifest manifest =
            InlineAdvanceManifest.Build(ctx.Line.TextWithoutCharacterName);

        int count = manifest.DrainRemaining();

        for (int i = 0; i < count; i++)
        {
            if (ctx.Token.NextContentToken.IsCancellationRequested)
                return SyncGateRunResult.Cancelled;

            SyncGateRunResult result =
                await _sideRunnerSyncHub.RunInlineScriptedAdvanceAsync(
                    1,
                    ctx.Token.NextContentToken);

            if (result == SyncGateRunResult.Cancelled)
                return SyncGateRunResult.Cancelled;
        }

        return SyncGateRunResult.Completed;
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

    // ──────────────────────────────────────────────────────────────────────
    // inline [advance/] host
    // ──────────────────────────────────────────────────────────────────────

    private void BeginInlineAdvance(
        LinePresentationRun run,
        CancellationToken cancel)
    {
        _inlineAdvanceRun = run;
        _inlineAdvanceCancel = cancel;
        _inlineAdvanceRunValid = true;
    }

    private void EndInlineAdvance()
    {
        _inlineAdvanceRunValid = false;
        _inlineAdvanceRun = default;
        _inlineAdvanceCancel = default;
    }

    async YarnTask IInlinePresentationAdvanceHost.AdvanceOneAsync(int ordinal)
    {
        if (!_inlineAdvanceRunValid)
            return;

        LinePresentationRun run = _inlineAdvanceRun;
        CancellationToken cancel = _inlineAdvanceCancel;

        if (run == null)
            return;

        if (!run.IsValid)
            return;

        if (cancel.IsCancellationRequested)
            return;

        await _sideRunnerSyncHub.RunInlineScriptedAdvanceAsync(1, cancel);
    }
}