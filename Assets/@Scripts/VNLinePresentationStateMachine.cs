using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>
/// 라인 하나의 표시 트랜잭션을 Phase 순서대로 실행한다.
/// 도메인 커밋은 Committer, Seek 판정은 SeekResolver에 위임한다.
/// CustomLinePresenter의 lifetime 소유권(generation, CTS)은 건드리지 않는다.
/// </summary>
public sealed class VNLinePresentationStateMachine
{
    private readonly VNLinePresentationCommitter _committer;
    private readonly VNSeekLineResolver _seekResolver;
    private readonly DialogueBoxPresentationController _boxPresentation;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly LinePresentationAdvanceState _advanceState;
    private readonly VNTraceStream _trace;

    // Phase 추적 (외부 진단용)
    public VNLinePresentationPhase CurrentPhase { get; private set; }
        = VNLinePresentationPhase.None;

    public VNLinePresentationStateMachine(
        VNLinePresentationCommitter committer,
        VNSeekLineResolver seekResolver,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        LinePresentationAdvanceState advanceState,
        VNTraceStream trace = null)
    {
        _committer = committer;
        _seekResolver = seekResolver;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _advanceState = advanceState;
        _trace = trace;
    }

    public async YarnTask RunAsync(
        VNLinePresentationContext ctx,
        System.Func<LinePresentationRun> beginRun,
        System.Func<LineCancellationToken, YarnTask> waitForAdvance,
        System.Func<bool> shouldFastForward)
    {
        // ────────────────────────────────────────────────
        // Phase: LineReceived → LineEnteredCommitted
        // ────────────────────────────────────────────────
        SetPhase(ctx, VNLinePresentationPhase.LineReceived);

        ctx.Meta = _committer.CommitLineEntered(ctx.Line, ctx.NodeName);

        SetPhase(ctx, VNLinePresentationPhase.LineEnteredCommitted);

        // ────────────────────────────────────────────────
        // Phase: SeekResolved
        // ────────────────────────────────────────────────
        VNSeekLineResolver.Decision seekDecision = _seekResolver.Resolve(ctx.Line.TextID);

        ctx.IsPendingSeekTargetLine = seekDecision.IsPendingSeekTargetLine;
        ctx.ShouldPassThrough = seekDecision.ShouldPassThrough;

        SetPhase(ctx, VNLinePresentationPhase.SeekResolved);
        TraceSeekDecision(ctx);

        // ── Seek Pass-Through ───────────────────────────
        if (ctx.ShouldPassThrough)
        {
            SetPhase(ctx, VNLinePresentationPhase.SeekPassThrough);

            _boxPresentation.HideAllForSeek();
            _committer.CommitLineProcessingCompleted();

            SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
            await waitForAdvance(ctx.Token);

            SetPhase(ctx, VNLinePresentationPhase.Completed);
            return;
        }

        // ── Seek Target Consume ─────────────────────────
        if (ctx.IsPendingSeekTargetLine)
            _seekResolver.ConsumeTargetLine(ctx.Line.TextID);

        // ────────────────────────────────────────────────
        // Phase: VisualRunStarted
        // ────────────────────────────────────────────────
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        // ────────────────────────────────────────────────
        // Phase: BoxTransitioning → BoxReady
        // ────────────────────────────────────────────────
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);

        ctx.BoxResult = await _boxPresentation.ShowLineAsync(
            VNDialogueLineFactory.FromLocalizedLine(ctx.Line),
            new DialogueBoxPresentationOptions
            {
                IsSeekTargetLine = ctx.IsPendingSeekTargetLine,
                UseImmediateTransition = ctx.IsPendingSeekTargetLine || shouldFastForward(),
                Run = ctx.Run,
            });

        // ── Stale 검사: Box 표시 후 ──────────────────────
        if (!ctx.Run.IsValid)
        {
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            TraceStale(ctx, "AfterBoxPresentation");

            _boxPresentation.CleanupStale(ctx.BoxResult);

            SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
            await waitForAdvance(ctx.Token);

            SetPhase(ctx, VNLinePresentationPhase.Completed);
            return;
        }

        SetPhase(ctx, VNLinePresentationPhase.BoxReady);

        // ────────────────────────────────────────────────
        // Phase: TypewriterRunning
        // ────────────────────────────────────────────────
        TMP_Text lineText = ctx.BoxResult?.LineText;
        _typewriter.SetTextView(lineText);

        MarkupParseResult text = ctx.Line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(text);

        SetPhase(ctx, VNLinePresentationPhase.TypewriterRunning);

        await _typewriter
            .RunTypewriter(text, ctx.Token.HurryUpToken)
            .SuppressCancellationThrow();

        // ── Stale 검사: Typewriter 완료 후 ──────────────
        if (!ctx.Run.IsValid)
        {
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            TraceStale(ctx, "AfterTypewriter");

            SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
            await waitForAdvance(ctx.Token);

            SetPhase(ctx, VNLinePresentationPhase.Completed);
            return;
        }

        // ────────────────────────────────────────────────
        // Phase: DisplayCommitted
        // ────────────────────────────────────────────────
        _committer.CommitLineProcessingCompleted();
        _typewriter.ContentWillDismiss();

        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        // ────────────────────────────────────────────────
        // Phase: WaitingForAdvance → Completed
        // ────────────────────────────────────────────────
        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        await waitForAdvance(ctx.Token);

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    // ── Trace Helpers ───────────────────────────────────

    private void SetPhase(VNLinePresentationContext ctx, VNLinePresentationPhase phase)
    {
        ctx.Phase = phase;
        CurrentPhase = phase;

        if (_trace == null) return;
        _trace.Trace(
            nameof(VNLinePresentationStateMachine),
            phase.ToString(),
            _advanceState?.Snapshot(),
            $"line={ctx.Line?.TextID}");
    }

    private void TraceSeekDecision(VNLinePresentationContext ctx)
    {
        if (_trace == null) return;
        _trace.Trace(
            nameof(VNLinePresentationStateMachine),
            "SeekDecision",
            _advanceState?.Snapshot(),
            $"line={ctx.Line?.TextID}, passThrough={ctx.ShouldPassThrough}, pendingTarget={ctx.IsPendingSeekTargetLine}");
    }

    private void TraceStale(VNLinePresentationContext ctx, string after)
    {
        if (_trace == null) return;
        _trace.Trace(
            nameof(VNLinePresentationStateMachine),
            "RunBecameStale",
            _advanceState?.Snapshot(),
            $"line={ctx.Line?.TextID}, after={after}");
    }
}