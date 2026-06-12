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
    
    private CancellationTokenSource _presenterLifetimeCts = new ();

    
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

        _syncHub.NotifyPresentationLaneCompleted();
        
        _debugSink?.ClearPresentation();
        
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        _debugSink?.SetPresentation(_currentNodeName, line.TextWithoutCharacterName.Text);

        bool blockMain = ShouldBlockMain(line);

        CommandRunTicket ticket = _playbackDriver.PlayCollected();

        if (!blockMain)
            _syncHub.NotifyPresentationForwardSettled();
        
        bool cancelledDuringEntry = await WaitUntilCommandEntryClosedAsync(ticket, token);

        // For wait=true commands this is the completion point, so a held beat naturally delays this signal.
        // Raise it once per beat — before the ready/released branch.
        // so main forward flow can wait for sub holds without coupling to the seek-only ready signal.
        if (blockMain)
            _syncHub.NotifyPresentationForwardSettled();

        bool tornDown = cancelledDuringEntry || token.NextContentToken.IsCancellationRequested;

        // 취소(rollback / stop / 직전 라인의 RequestNextLine)로 무너진 라인은
        // "완료된 advance"로 취급하면 안 된다. 그러면 pending을 소모하고
        // RequestNextLine을 한 번 더 쳐서 질주한다.
        // 대신 main 대기만 풀어준다.
        if (tornDown) 
            _syncHub.NotifyPresentationLaneReleased();
        else
            _syncHub.NotifyPresentationLaneReady();

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

        // Normal case
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