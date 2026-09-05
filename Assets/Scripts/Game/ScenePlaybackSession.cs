using System.Threading.Tasks;

// 한 Scene 동안 Yarn / Presentation playback의 수명을 관리한다.
//
// SceneRunner는 어디로 진행할지를 결정하고,
// 이 클래스는 현재 Yarn node의 실행 / 중단 / 복원을 책임진다.
public sealed class ScenePlaybackSession
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly VNScreenBindings _vnScreenBindings;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    private readonly PresentationStage _presentationStage;
    private readonly PresentationScopeSession _presentationScope;
    private readonly YarnVariableCheckpoint _variableCheckpoint;
    private readonly ChoiceHistory _choiceHistory;

    private Task _stopTask;

    public ScenePlaybackSession(
        IEpisodeNodeRunner nodeRunner,
        VNScreenBindings vnScreenBindings,
        RollbackHistory rollbackHistory,
        IVNLineAborter linePresentationAborter,
        PresentationShotResponseSystem shotResponseSystem,
        PresentationStage presentationStage,
        PresentationScopeSession presentationScope,
        YarnVariableCheckpoint variableCheckpoint,
        ChoiceHistory choiceHistory)
    {
        _nodeRunner = nodeRunner;
        _vnScreenBindings = vnScreenBindings;
        _rollbackHistory = rollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _shotResponseSystem = shotResponseSystem;
        _presentationStage = presentationStage;
        _presentationScope = presentationScope;
        _variableCheckpoint = variableCheckpoint;
        _choiceHistory = choiceHistory;
    }

    public async Task BeginSceneAsync()
    {
        await AwaitStopAsync();
        await StopAsync();

        _variableCheckpoint.Capture();
        _choiceHistory.ClearChoiceRecords();

        BeginPlayback();
    }

    public Task PlayNodeAsync(string nodeName) =>
        _nodeRunner.StartAsync(nodeName);

    public async Task PrepareReplayAsync()
    {
        // Replay 요청 측 Stop과 RunAsync 측 Replay 준비가
        // 동시에 진행되더라도 이전 Stop이 끝난 뒤 다시 시작한다.
        await AwaitStopAsync();

        _variableCheckpoint.Restore();

        BeginPlayback();
    }

    public async Task StopAsync()
    {
        Task stop = _stopTask;

        if (stop == null)
        {
            stop = StopPlaybackAsync();
            _stopTask = stop;
        }

        try
        {
            await stop;
        }
        finally
        {
            if (ReferenceEquals(_stopTask, stop))
                _stopTask = null;
        }
    }

    private void BeginPlayback()
    {
        _vnScreenBindings.GoToPresentationView();

        _presentationStage.Clear();
        _presentationScope.Start();
    }

    private async Task StopPlaybackAsync()
    {
        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        _linePresentationAborter.AbortCurrentVNLine();
        _rollbackHistory.ClearRollbackPoints();
        _shotResponseSystem.Clear();
        _presentationScope.End();
    }

    private async Task AwaitStopAsync()
    {
        Task stop = _stopTask;

        if (stop == null)
            return;

        await stop;

        if (ReferenceEquals(_stopTask, stop))
            _stopTask = null;
    }
}