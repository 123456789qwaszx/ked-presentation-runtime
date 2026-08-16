using System;
using DG.Tweening;
using Yarn.Unity;

// Runs one line presentation transaction through its explicit phase sequence.
public partial class VNLinePresentationFlow
{
    private readonly VNYarnLineBoundary _vnYarnLineBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly YarnBridgePlaybackDriver _playbackDriver;

    private const float LineHurrySpeedMultiplier = 30f;

    private bool _lineHurrySpeedActive;
    private float _lineHurryRestoreTypewriterMultiplier = 1f;
    private float _lineHurryRestoreDotweenUnscaledTimeScale = 1f;

    public VNLinePresentationPhase CurrentPhase { get; private set; } =
        VNLinePresentationPhase.None;

    public VNLinePresentationFlow(
        VNYarnLineBoundary vnYarnLineBoundary,
        VNLinePresentationState advanceState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        YarnBridgePlaybackDriver playbackDriver)
    {
        _vnYarnLineBoundary = vnYarnLineBoundary;
        _advanceState = advanceState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _playbackDriver = playbackDriver;
    }

    private bool TryEnterLineAndResolveSeek(
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
        {
            seekKind = _advanceState.SeekKind;

            ctx.SeekDecision = _advanceState.IsSeekTargetLine(ctx.Meta)
                ? VNSeekLineDecision.TargetLineReachedAndResumePresentation(seekKind)
                : VNSeekLineDecision.SkipVisualAndDispatchSeekNext(seekKind);
        }
        else
        {
            ctx.SeekDecision = VNSeekLineDecision.NotSeeking();
        }

        SetPhase(ctx, VNLinePresentationPhase.LineRuntimeStateResolved);

        if (ctx.ShouldSkipVisual)
        {
            RunSeekPassThrough(ctx);
            return false;
        }

        if (ctx.IsPendingSeekTargetLine)
        {
            _advanceState.ClearSeek();

            ctx.SeekDecision = seekKind == VNSeekKind.Rollback
                ? VNSeekLineDecision.TargetLineVisualResumeImmediate(seekKind)
                : VNSeekLineDecision.TargetLineVisualResumeNormal(seekKind);
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
        if (!TryEnterLineAndResolveSeek(ctx, recordToHistory: true))
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

        try
        {
            await _typewriter
                .RunTypewriter(
                    ctx.Text,
                    ctx.Token.HurryUpToken,
                    ctx.Token.NextContentToken,
                    EnterLineHurrySpeed)
                .SuppressCancellationThrow();
        }
        finally
        {
            ExitLineHurrySpeed();
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

    // 시크 통과 라인은 기다릴 것이 없다 — 대사창을 닫고 표시 완료만 찍는다.
    private void RunSeekPassThrough(VNLinePresentationContext ctx)
    {
        SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

        _boxPresentation.CloseAll();

        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "passThrough");

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

    private void EnterLineHurrySpeed()
    {
        if (_lineHurrySpeedActive)
            return;

        _lineHurryRestoreTypewriterMultiplier = _typewriter.SpeedMultiplier;
        _lineHurryRestoreDotweenUnscaledTimeScale = DOTween.unscaledTimeScale;

        _typewriter.SetSpeedMultiplier(
            _lineHurryRestoreTypewriterMultiplier * LineHurrySpeedMultiplier);

        DOTween.unscaledTimeScale = LineHurrySpeedMultiplier;

        _lineHurrySpeedActive = true;
    }

    private void ExitLineHurrySpeed()
    {
        if (!_lineHurrySpeedActive)
            return;

        _typewriter.SetSpeedMultiplier(_lineHurryRestoreTypewriterMultiplier);
        DOTween.unscaledTimeScale = _lineHurryRestoreDotweenUnscaledTimeScale;

        _lineHurrySpeedActive = false;
    }

    private void SetPhase(VNLinePresentationContext ctx, VNLinePresentationPhase phase)
    {
        ctx.Phase = phase;
        CurrentPhase = phase;
    }
}