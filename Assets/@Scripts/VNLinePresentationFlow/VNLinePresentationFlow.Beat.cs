using System;
using Yarn.Unity;

public partial class VNLinePresentationFlow
{
    // ---- Staging-only beat ----
    // No dialogue box / typewriter. The line still commits meta (backlog + rollback) and
    // consumes a sub advance via the shared front-matter. It auto-advances once its own
    // staging (this line's wait=true commands) and the dispatched sub beat have settled.
    // A #stay marker keeps it on screen waiting for player advance instead of auto-advancing.
    public async YarnTask RunPresentationBeatAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance)
    {
        LineEntryOutcome outcome = await EnterLineAndResolveSeekAsync(ctx, beginRun);
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
