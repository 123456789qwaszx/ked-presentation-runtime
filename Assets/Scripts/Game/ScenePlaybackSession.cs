using System;
using System.Threading.Tasks;

public enum NodePlayOutcome
{
    Completed = 0,
    ReplayRequested = 1,
}

// 한 Scene 동안 Yarn / Presentation playback의 수명을 관리한다.
//
// SceneRunner는 "어디로 진행할지"를 결정하고,
// 이 클래스는 "현재 Story node를 어떤 playback 환경에서 실행할지"를 책임진다.
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

    private bool _replayRequested;
    private Task _replayStop;

    public bool IsReplayPending => _replayRequested;

    // Replay 요청을 상위 Scene flow에 알린다.
    // 상위에서 기다리는 것이 없다면 subscriber의 Cancel은 no-op이어야 한다.
    public event Action ReplayRequested;

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

    // 새로운 Scene playback 기준선을 만든다.
    //
    // 이 순간의 Yarn 변수가 rollback replay checkpoint가 된다.
    // Yarn 선택 기록은 Scene scope이므로 새 Scene마다 초기화한다.
    public async Task BeginSceneAsync()
    {
        await AwaitReplayStopAsync();
        await StopPlaybackAsync();

        _replayRequested = false;

        _variableCheckpoint.Capture();
        _choiceHistory.ClearChoiceRecords();

        BeginPlayback();
    }

    public async Task<NodePlayOutcome> PlayNodeAsync(string nodeName)
    {
        if (_replayRequested)
            return NodePlayOutcome.ReplayRequested;

        await _nodeRunner.StartAsync(nodeName);

        return _replayRequested
            ? NodePlayOutcome.ReplayRequested
            : NodePlayOutcome.Completed;
    }

    // Replay 요청으로 중단된 playback이 완전히 끝난 뒤
    // Scene 진입 checkpoint를 복원하고 새 playback 기준선을 만든다.
    public async Task PrepareReplayAsync()
    {
        await AwaitReplayStopAsync();

        _replayRequested = false;

        _variableCheckpoint.Restore();

        BeginPlayback();
    }

    // 현재 playback을 중단하고 replay 요청을 남긴다.
    //
    // progression history rewind와 Scene root 복귀는 SceneRunner가 한다.
    public async Task RequestReplayAsync()
    {
        if (_replayRequested)
        {
            await AwaitReplayStopAsync();
            return;
        }

        _replayRequested = true;

        // 반드시 event보다 먼저 Stop task를 등록한다.
        //
        // event가 progression option을 즉시 취소하면
        // SceneRunner가 곧바로 PrepareReplayAsync에 들어올 수 있기 때문이다.
        _replayStop = StopPlaybackAsync();

        ReplayRequested?.Invoke();

        await _replayStop;
    }

    // 타이틀 이동 등 외부 중단에서 현재 playback을 정리한다.
    public async Task StopAsync()
    {
        await AwaitReplayStopAsync();
        await StopPlaybackAsync();
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

    // 이전 Stop과 다음 Start가 겹치지 않게 한다.
    //
    // 이를 보장하지 않으면 이전 Stop 정리가 새 Node의 첫 Line을
    // 뒤늦게 소비하는 race가 생길 수 있다.
    private async Task AwaitReplayStopAsync()
    {
        Task stop = _replayStop;

        if (stop == null)
            return;

        await stop;

        if (ReferenceEquals(_replayStop, stop))
            _replayStop = null;
    }
}