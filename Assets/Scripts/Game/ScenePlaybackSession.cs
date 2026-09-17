using System.Threading.Tasks;
using Ked.Progression;

// 한 Scene 동안 Yarn / Presentation playback의 수명을 관리한다.
//
// SceneRunner는 어디로 진행할지를 결정하고,
// 이 클래스는 현재 Yarn node의 실행 / 중단 / 복원을 책임진다.
public sealed class ScenePlaybackSession : IScenePlayback
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly VNScreenBindings _vnScreenBindings;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    private readonly PresentationStage _presentationStage;
    private readonly PresentationScopeSession _presentationScope;
    private readonly ChoiceHistory _choiceHistory;
    private readonly EpisodeSkipController _episodeSkipController;

    public ScenePlaybackSession(
        IEpisodeNodeRunner nodeRunner,
        VNScreenBindings vnScreenBindings,
        RollbackHistory rollbackHistory,
        IVNLineAborter linePresentationAborter,
        PresentationShotResponseSystem shotResponseSystem,
        PresentationStage presentationStage,
        PresentationScopeSession presentationScope,
        ChoiceHistory choiceHistory,
        EpisodeSkipController episodeSkipController)
    {
        _nodeRunner = nodeRunner;
        _vnScreenBindings = vnScreenBindings;
        _rollbackHistory = rollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _shotResponseSystem = shotResponseSystem;
        _presentationStage = presentationStage;
        _presentationScope = presentationScope;
        _choiceHistory = choiceHistory;
        _episodeSkipController = episodeSkipController;
    }

    public Task BeginSceneAsync()
    {
        _choiceHistory.ClearChoiceRecords();

        _vnScreenBindings.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();

        return Task.CompletedTask;
    }

    public async Task PlayNodeAsync(string nodeName)
    {
        try
        {
            await _nodeRunner.StartAsync(nodeName);
        }
        finally
        {
            // 현재 Yarn node의 수명이 끝났으므로
            // one-shot Episode Skip도 여기서 끝난다.
            _episodeSkipController.CompleteEpisode();
        }
    }

    public Task PrepareReplayAsync()
    {
        _vnScreenBindings.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _episodeSkipController.Cancel();

        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        _linePresentationAborter.AbortCurrentVNLine();
        _rollbackHistory.ClearRollbackPoints();
        _shotResponseSystem.Clear();
        _presentationScope.End();
    }
}
