using System.Threading;
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
        int generation = _syncHub.GetLaneGeneration(PresentationLaneKey);

        _playbackDriver.PlayCollected();

        _syncHub.NotifyLaneReady(PresentationLaneKey, generation);

        try
        {
            await WaitForLineAdvanceAsync(token);
        }
        finally
        {
            _syncHub.NotifyLaneNotReady(PresentationLaneKey, generation);
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