using System;
using Yarn.Unity;

public partial class VNLinePresentationFlow
{
    // ---- Staging-only beat ----
    // Runs presentation commands without showing dialogue text.
    // This line is not recorded to backlog/rollback,
    // but still consumes the shared line-entry flow, including sub-runner sync.
    // Once its command ticket settles, it auto-advances unless the line has #stay metadata.
    public async YarnTask RunPresentationBeatAsync(
        VNLinePresentationContext ctx,
        Func<LinePresentationRun> beginRun,
        Func<LineCancellationToken, YarnTask> waitForAdvance,
        Func<bool> shouldFastForward)
    {
        if (!TryEnterLineAndResolveSeek(ctx, recordToHistory: false))
            return;
    
        ctx.Run = beginRun();
        SetPhase(ctx, VNLinePresentationPhase.VisualRunStarted);

        _boxPresentation.CloseAll();
        _typewriter.SetTextView(null);
        SetPhase(ctx, VNLinePresentationPhase.DialogueSurfaceResolved);

        await WaitUntilBeatStagingSettledAsync(
            ctx,
            shouldFastForward != null && shouldFastForward());

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

        SetPhase(ctx, VNLinePresentationPhase.Completed);
    }

    // Waits until this beat's staging commands stop blocking the line.
    // Ends early if the line is cancelled or the current run becomes stale.
    private async YarnTask WaitUntilBeatStagingSettledAsync(
        VNLinePresentationContext ctx,
        bool startFast)
    {
        if (startFast)
            EnterLineHurrySpeed();

        try
        {
            while (!ctx.CommandTicket.EntryClosed)
            {
                if (ctx.Token.NextContentToken.IsCancellationRequested)
                    return;

                if (!ctx.Run.IsValid)
                    return;

                if (ctx.Token.HurryUpToken.IsCancellationRequested)
                    EnterLineHurrySpeed();

                await YarnTask.Yield();
            }
        }
        finally
        {
            ExitLineHurrySpeed();
        }
    }
}