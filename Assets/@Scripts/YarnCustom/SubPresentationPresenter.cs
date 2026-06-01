using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private const string PresentationLaneKey = VNSideRunnerLaneKeys.Presentation;

    private YarnBridgePlaybackDriver _playbackDriver;
    private VNSideRunnerSyncHub _syncHub;

    private CancellationTokenSource _presenterLifetimeCts = new CancellationTokenSource();

    public void Initialize(
        YarnBridgePlaybackDriver playbackDriver,
        VNSideRunnerSyncHub syncHub)
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

    private async YarnTask WaitUntilCommandEntryClosedAsync(
        CommandRunTicket ticket,
        LineCancellationToken token)
    {
        if (ticket == null)
            return;

        while (!ticket.EntryClosed)
        {
            if (token.NextContentToken.IsCancellationRequested)
                return;

            await YarnTask.Yield();
        }

        if (!ticket.EntrySatisfied)
        {
            if (ticket.HasFailures)
            {
                Debug.LogWarning(
                    "[SubPresentationPresenter] Command entry failed.\n" +
                    ticket.UnsatisfiedCommandSnapshot());
            }
            else if (ticket.WasInterrupted)
            {
                // Debug.Log(
                //     "[SubPresentationPresenter] Command entry was interrupted by an expected route change.\n" +
                //     "This can happen when rollback/load/stop is requested while a wait=true command is running. " +
                //     "Remaining commands from the previous run are intentionally skipped.\n" +
                //     ticket.UnsatisfiedCommandSnapshot());
            }
            else
            {
                Debug.LogWarning(
                    "[SubPresentationPresenter] Command entry was closed before all commands entered, but no failure or interrupt reason was recorded.\n" +
                    ticket.UnsatisfiedCommandSnapshot());
            }
        }
    }

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

    private void OnDestroy()
    {
        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
            _presenterLifetimeCts = null;
        }
    }
}