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
    private readonly StageStepFlow _stageStep;
    private readonly InlineCommandTrack _inlineCommands;
    private readonly LineHurrySpeedController _lineHurrySpeed;

    public LinePresentationFlow(
        YarnLineBoundary yarnLineBoundary,
        LineDisplayState lineDisplayState,
        SeekState seekState,
        DialogueBoxPresentationController boxPresentation,
        EllipsisBreathTypewriter typewriter,
        StepHead stepHead,
        StageStepFlow stageStep,
        InlineCommandTrack inlineCommands,
        LineHurrySpeedController lineHurrySpeed)
    {
        _yarnLineBoundary = yarnLineBoundary;
        _lineDisplayState = lineDisplayState;
        _seekState = seekState;
        _boxPresentation = boxPresentation;
        _typewriter = typewriter;
        _stepHead = stepHead;
        _stageStep = stageStep;
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

        bool seekPassThrough = ctx.SeekDecision == SeekLineDecision.SkipVisualAndDispatchSeekNext;
        bool seekArrived = ctx.SeekDecision == SeekLineDecision.TargetLineReached;

        // 줄에 들어선다 — 앞 줄의 서두르기와 보이스가 끝나고, 글의 표지와 쌓인 <<at>>이 이 줄의 앵커가 된다.
        // 여기서부터 이 줄이 제 연출을 모두 실행한다: 앞 앵커 → start(머리) → 글 표지 → end(docs/PLAN_line-anchor.md §4).
        _stepHead.Enter();
        _inlineCommands.Begin(ctx.Line.TextWithoutCharacterName);

        // 앞 앵커 — 대사창이 열리기 전. 시크가 지나가는 줄과 시크로 닿은 줄은 기다리지 않고 끝값으로 놓는다(A11).
        // 시크가 살아 있는 동안이라야 명령이 압축된다 — 시크를 끝내기 전에 한다.
        bool hasPrelude = _stageStep.HasLinePrelude;

        if (hasPrelude && (seekPassThrough || seekArrived))
        {
            _stageStep.PlayLinePreludeSnapped();
            hasPrelude = false;
        }

        if (seekArrived)
            _seekState.End(SeekEndReason.Arrived);

        if (hasPrelude)
        {
            // Phase: Prelude — 대사창을 닫고 앞 앵커를 차례로 연다. 이 줄의 화면 수명은 여기서 시작한다.
            ctx.Run = beginRun();
            SetPhase(ctx, LinePresentationPhase.Prelude);

            await _stageStep.RunLinePreludeAsync(ctx.Run);

            if (!ctx.Run.IsValid) {
                await CompleteStaleAfterBoxAsync(ctx, waitForAdvance);
                return;
            }
        }

        // start — 대사창이 열리는 때. #auto의 머무름은 여기서부터 잰다(AR11).
        _lineDisplayState.MarkLineStarted();
        _lineDisplayState.SetLineAutoAdvance(autoAdvance, autoHold);

        // 머리(at 없는 명령) 바로 뒤에 start의 <<at>>. 글 표지 · end는 타자기가 글자 자리에서 연다.
        _stepHead.ReleaseLineWaits(meta.lineId);
        _stepHead.Play(snap: ctx.IsRapidSkip || seekPassThrough, seekPassThrough: seekPassThrough);
        _inlineCommands.PlayStart();
        SetPhase(ctx, LinePresentationPhase.LineEnteredCommitted);
        
        if (seekPassThrough) {
            RunSeekPassThrough(ctx);
            return;
        }

        // Phase: VisualRunStarted — 앞 앵커가 열어 둔 화면 수명이 있으면 그대로 쓴다.
        ctx.Run ??= beginRun();
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

        // 타자기가 닿지 않은 자리 — <<at end>>, 표지를 잃은 것, 글 끝의 표지, 다 보이기 전에 넘어간 줄, 글 칸이 없는 줄.
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

        // 타자기가 돌지 않는다 — 글 표지 · end의 명령은 start 바로 뒤에 앵커의 차례대로 압축 실행된다.
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