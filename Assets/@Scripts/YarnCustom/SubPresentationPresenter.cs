using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private YarnBridgePlaybackDriver _playbackDriver;
    private VNSideRunnerSyncHub _syncHub;

    private CancellationTokenSource _presenterLifetimeCts = new();

    public void Initialize(YarnBridgePlaybackDriver playbackDriver, VNSideRunnerSyncHub syncHub)
    {
        _playbackDriver = playbackDriver;
        _syncHub = syncHub;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CancelPresenterLifetimeWaiters();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        CommandRunTicket ticket = _playbackDriver.PlayCollected();

        await WaitUntilCommandEntryClosedAsync(ticket, token);

        // This means "the sub presentation lane no longer blocks the main runner."
        // It does not necessarily mean every command completed normally.
        //
        // During rollback/load/stop, a wait=true command may be interrupted before
        // later commands enter. That is a normal lane release path.
        _syncHub.NotifyPresentationLaneReady();

        try
        {
            await WaitForLineAdvanceAsync(token);
        }
        finally
        {
            _syncHub.NotifyPresentationLaneNotReady();
        }
    }

    private async YarnTask WaitUntilCommandEntryClosedAsync(CommandRunTicket ticket, LineCancellationToken token)
    {
        if (ticket == null)
            return;

        while (!ticket.EntryClosed)
        {
            if (token.NextContentToken.IsCancellationRequested)
                return;

            await YarnTask.Yield();
        }

        ReportTicketIfNeeded(ticket);
    }

    private void ReportTicketIfNeeded(CommandRunTicket ticket)
    {
        if (ticket.EntryCompletedSuccessfully)
            return;

        if (ticket.EntryInterruptedNormally)
        {
            // Normal case:
            // Rollback/load/stop interrupted the batch while a wait=true command
            // was still holding the SequencePlayer entry loop.
            //
            // Do not warn here. This is not a command failure.
            return;
        }

        if (ticket.EntryFailed)
        {
            Debug.LogWarning(
                "[SubPresentationPresenter] Command entry failed. " +
                ticket.ToDebugString());
            return;
        }

        if (ticket.EntryClosedUnexpectedly)
        {
            Debug.LogWarning(
                "[SubPresentationPresenter] Command entry closed unexpectedly. " +
                ticket.ToDebugString());
        }
    }

    // A sub-presentation line does not complete by itself.
    // After its command batch has entered or has been normally interrupted,
    // it marks the lane as ready/released and waits until the hub/main runner
    // requests the next sub line, which cancels the current Yarn line token.
    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource lineWaitCts = null;

        try
        {
            lineWaitCts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask
                .WaitUntilCanceled(lineWaitCts.Token)
                .SuppressCancellationThrow();
        }
        finally
        {
            if (lineWaitCts != null)
                lineWaitCts.Dispose();
        }
    }

    private void CancelPresenterLifetimeWaiters()
    {
        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
        }

        _presenterLifetimeCts = new CancellationTokenSource();
    }
}