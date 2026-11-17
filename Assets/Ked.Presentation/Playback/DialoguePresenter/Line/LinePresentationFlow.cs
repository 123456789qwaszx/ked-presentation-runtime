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
    private readonly StepHead _stepHead;
    private readonly InlineCommandTrack _inlineCommands;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public LinePresentationFlow(
        YarnLineBoundary yarnLineBoundary,
        LineDisplayState lineDisplayState,
        SeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        StepHead stepHead,
        InlineCommandTrack inlineCommands,
        LineHurrySpeedController lineHurrySpeed)
    {
        _yarnLineBoundary = yarnLineBoundary;
        _lineDisplayState = lineDisplayState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _stepHead = stepHead;
        _inlineCommands = inlineCommands;
        _lineHurrySpeed = lineHurrySpeed;
    }

    public async YarnTask RunAsync(
        LinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        SetPhase(ctx, LinePresentationPhase.LineReceived);
        
        _lineDisplayState.MarkLineEntered();
        YarnLineMeta meta = _yarnLineBoundary.BuildLineMeta(ctx.Line);
        
        // Phase: LineRuntimeStateResolved
        ctx.EntrySeekKind = _seekState.Kind;
        _seekState.ObserveLine(meta.lineId);

        ctx.SeekDecision   = ResolveSeekDecision(ctx.EntrySeekKind);
        ctx.RecordDecision = LineRecordPolicy.Resolve(ctx.EntrySeekKind);
        ctx.AdvanceMode    = LineAdvancePolicy.Resolve(ctx.EntrySeekKind, ctx.SeekDecision, ctx.Line.Metadata);
        SetPhase(ctx, LinePresentationPhase.LineRuntimeStateResolved);
        
        // Phase: LineEnteredCommitted
        string voiceKey = _stepHead.FindVoiceKey();
        _yarnLineBoundary.CommitLineEntered(meta, voiceKey, ctx.RecordDecision);
        bool autoAdvance = ctx.AdvanceMode == LineAdvanceMode.AutoWhenShown;
        float autoHold = 0f;

        if (autoAdvance)
            LineAdvanceTag.TryGetAuto(ctx.Line.Metadata, out autoHold);

        _lineDisplayState.SetLineAutoAdvance(autoAdvance, autoHold);

        if (ctx.SeekDecision == SeekLineDecision.TargetLineReached)
            _seekState.End(SeekEndReason.Arrived);

        bool seekPassThrough = ctx.SeekDecision == SeekLineDecision.SkipVisualAndDispatchSeekNext;
        _stepHead.Play(snap: ctx.IsRapidSkip || seekPassThrough, seekPassThrough: seekPassThrough);

        // 인라인 명령은 이 줄의 스텝에 덧붙는다. 글자 자리에서 타자기가 실행한다.
        _inlineCommands.Begin(ctx.Line.TextWithoutCharacterName);
        SetPhase(ctx, LinePresentationPhase.LineEnteredCommitted);
        
        if (seekPassThrough) {
            RunSeekPassThrough(ctx);
            return;
        }

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

        // 타자기가 닿지 않은 인라인 명령 — 글 끝에 둔 것, 다 보이기 전에 넘어간 줄, 글 칸이 없는 줄.
        // 넘어간 줄의 끝 상태는 접힘과 같아야 한다. 끊긴 줄(위)은 실행하지 않는다 — 장면을 다시 접는다.
        _inlineCommands.PlayRemaining();

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

        // 타자기가 돌지 않는다 — 인라인 명령은 줄 머리 명령 바로 뒤에 글 순서대로 압축 실행된다.
        _inlineCommands.PlayRemaining();

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