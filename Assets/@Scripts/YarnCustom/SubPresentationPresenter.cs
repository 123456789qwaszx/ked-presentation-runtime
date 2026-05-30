using System.Threading;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private YarnBridgePlaybackDriver _playbackDriver;
    private DialogueAdvanceDispatcher _advanceDispatcher;

    private CancellationTokenSource _presenterLifetimeCts = new();

    public void Initialize(YarnBridgePlaybackDriver playbackDriver, DialogueAdvanceDispatcher advanceDispatcher)
    {
        _playbackDriver = playbackDriver;
        _advanceDispatcher = advanceDispatcher;
    }
    
    public override YarnTask OnDialogueStartedAsync() { return YarnTask.CompletedTask; }
    public override YarnTask OnDialogueCompleteAsync()
    {
        CancelPresenterLifetimeWaiters();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        _playbackDriver.PlayCollected();
        _advanceDispatcher.NotifySubReadyForAdvance();

        await WaitForLineAdvanceAsync(token);
        _advanceDispatcher.NotifySubNotReadyForAdvance();
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