using System;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private const string MainFree = "main_free";
    
    private YarnBridgePlaybackDriver _playbackDriver;
    private VNSideRunnerSyncHub _syncHub;
    private IYarnLaneDebugSink _debugSink;
    
    private string _currentNodeName;
    private PresentationLaneRunToken _currentRun;
    
    private CancellationTokenSource _presenterLifetimeCts = new();

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnBridgePlaybackDriver playbackDriver,
        VNSideRunnerSyncHub syncHub,
        IYarnLaneDebugSink debugSink = null)
    {
        if (dialogueRunner != null)
            dialogueRunner.onNodeStart?.AddListener(nodeName => _currentNodeName = nodeName);

        _playbackDriver = playbackDriver;
        _syncHub = syncHub;
        _debugSink = debugSink;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CancelPresenterLifetimeWaiters();

        _syncHub.NotifyLaneCompleted(_currentRun);
        
        _debugSink?.ClearPresentation();
        
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(
        LocalizedLine line,
        LineCancellationToken token)
    {
        _currentRun = _syncHub.CurrentPresentationRun;

        _debugSink?.SetPresentation(
            _currentNodeName,
            line.TextWithoutCharacterName.Text);

        bool blockMain = ShouldBlockMain(line);

        CommandRunTicket ticket = _playbackDriver.PlayCollected();

        if (!blockMain)
            _syncHub.NotifyForwardSettled(_currentRun);
        
        bool cancelledDuringEntry = await WaitUntilCommandEntryClosedAsync(
            ticket,
            token);

        if (blockMain)
            _syncHub.NotifyForwardSettled(_currentRun);

        bool tornDown =
            cancelledDuringEntry ||
            token.NextContentToken.IsCancellationRequested;

        if (tornDown)
            _syncHub.NotifyLaneReleased(_currentRun);
        else
            _syncHub.NotifyLaneReady(_currentRun);

        try
        {
            await WaitForLineAdvanceAsync(token);
        }
        finally
        {
            _syncHub.NotifyLaneNotReady(_currentRun);
        }
    }

    private async YarnTask<bool> WaitUntilCommandEntryClosedAsync(
        CommandRunTicket ticket,
        LineCancellationToken token)
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
            return;
        
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
    
    private static bool ShouldBlockMain(LocalizedLine line)
    {
        foreach (string metadata in line.Metadata)
        {
            if (string.Equals(metadata, MainFree, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}