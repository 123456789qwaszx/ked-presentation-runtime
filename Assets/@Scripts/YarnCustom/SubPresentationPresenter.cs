using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private YarnBridgePlaybackDriver _playbackDriver;
    private VNSideRunnerSyncHub _syncHub;

    private CancellationTokenSource _presenterLifetimeCts = new CancellationTokenSource();

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

        if (_syncHub != null)
            _syncHub.NotifyPresentationLaneCompleted();

        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        CommandRunTicket ticket = _playbackDriver.PlayCollected();

        bool cancelledDuringEntry = await WaitUntilCommandEntryClosedAsync(ticket, token);

        // 취소(rollback / stop / 직전 라인의 RequestNextLine)로 무너진 라인은
        // "완료된 advance"로 취급하면 안 된다. 그러면 pending을 소모하고
        // RequestNextLine을 한 번 더 쳐서 질주한다.
        // 대신 main 대기만 풀어준다.
        bool tornDown = cancelledDuringEntry || token.NextContentToken.IsCancellationRequested;

        if (_syncHub != null)
        {
            if (tornDown)
                _syncHub.NotifyPresentationLaneReleased();
            else
                _syncHub.NotifyPresentationLaneReady();
        }

        try
        {
            await WaitForLineAdvanceAsync(token);
        }
        finally
        {
            if (_syncHub != null)
                _syncHub.NotifyPresentationLaneNotReady();
        }
    }

    // entry가 닫히기 전에 라인 취소로 빠져나왔으면 true.
    private async YarnTask<bool> WaitUntilCommandEntryClosedAsync(CommandRunTicket ticket, LineCancellationToken token)
    {
        if (ticket == null)
            return token.NextContentToken.IsCancellationRequested;

        while (!ticket.EntryClosed)
        {
            if (token.NextContentToken.IsCancellationRequested)
                return true;

            await YarnTask.Yield();
        }

        ReportTicketIfNeeded(ticket);
        return false;
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