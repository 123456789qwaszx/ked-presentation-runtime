using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private YarnBridgePlaybackDriver _playbackDriver;
    private VNSideRunnerSyncHub _syncHub;

    private CancellationTokenSource _presenterLifetimeCts = new ();

    public void Initialize(YarnBridgePlaybackDriver playbackDriver, VNSideRunnerSyncHub syncHub)
    {
        _playbackDriver = playbackDriver;
        _syncHub = syncHub;
    }

    public override YarnTask OnDialogueStartedAsync() { return YarnTask.CompletedTask; }

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

    private async YarnTask WaitUntilCommandEntryClosedAsync(CommandRunTicket ticket, LineCancellationToken token)
    {
        while (!ticket.EntryClosed)
        {
            if (token.NextContentToken.IsCancellationRequested)
                return;

            await YarnTask.Yield();
        }

        if (!ticket.EntrySatisfied)
            Debug.LogWarning("[SubPresentationPresenter] Command entry failed or interrupted. Wait = true 인 커맨드 실행 중 Rollback");
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
}