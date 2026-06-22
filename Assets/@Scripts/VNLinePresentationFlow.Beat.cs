using System;
using Yarn.Unity;

public partial class VNLinePresentationFlow
{
    // ---- Staging-only beat ----
    // No dialogue box / typewriter. 대사가 없는 연출 전용 라인이므로 backlog/rollback에는
    // 기록하지 않는다(recordToHistory: false). 다만 shared front-matter를 통해 sub advance는
    // 그대로 소비하고, 자신의 staging(this line's wait=true commands)과 dispatch된 sub beat가
    // settle되면 auto-advance한다. #stay 마커가 있으면 auto-advance 대신 플레이어 입력을 기다린다.
    public async YarnTask RunPresentationBeatAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        // 연출 비트: backlog/rollback에 기록하지 않는다.
        LineEntryOutcome outcome = await EnterLineAndResolveSeekAsync(
            ctx, beginRun, recordToHistory: false);
        if (outcome == LineEntryOutcome.PassedThrough)
            return;

        // Box stays hidden for a beat. Hide everything coherently (resets the controller's
        // box state), so the next real dialogue line fades a box back in cleanly.
        SetPhase(ctx, VNLinePresentationPhase.BoxTransitioning);
        _boxPresentation.CloseAll();
        _typewriter.SetTextView(null);
        SetPhase(ctx, VNLinePresentationPhase.BoxReady);

        // Wait for this line's own staging to finish. For wait=true commands, entry-close
        // == completion; for fire-and-forget commands this returns ~immediately.
        await WaitUntilCommandTicketSettledAsync(ctx);

        if (!ctx.Run.IsValid) {
            _advanceState.MarkLineDisplayCompleted(ctx.Meta, "beatStale");
            SetPhase(ctx, VNLinePresentationPhase.Stale);
            SetPhase(ctx, VNLinePresentationPhase.Completed);
            return;
        }

        _advanceState.MarkLineDisplayCompleted(ctx.Meta, "beat");
        SetPhase(ctx, VNLinePresentationPhase.DisplayCommitted);

        SetPhase(ctx, VNLinePresentationPhase.WaitingForAdvance);
        if (DialogueBoxMetadataResolver.IsBeatStay(ctx.Line.Metadata))
            await waitForAdvance(ctx.Token);
        // else: auto-advance by simply returning (the runner proceeds to the next content).

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    private async YarnTask WaitUntilCommandTicketSettledAsync(VNLinePresentationContext ctx)
    {
        CommandRunTicket ticket = ctx.CommandTicket;
        if (ticket == null)
            return;

        while (!ticket.EntryClosed)
        {
            if (ctx.Token.NextContentToken.IsCancellationRequested)
                return;

            if (ctx.Run != null && !ctx.Run.IsValid)
                return;

            await YarnTask.Yield();
        }
    }
}