using System.Threading.Tasks;

// 재생 단위(노드 하나 / 챕터의 장면) 하나에 필요한 Yarn / Presentation 수명을 관리.
// 두 호스트가 함께 쓴다 — 진행 층의 IScenePresentation은 ScenePresentationProgressionAdapter가 잇는다.
public sealed class ScenePresentation
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly IPresentationView _presentationView;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    private readonly PresentationStage _presentationStage;
    private readonly PresentationScopeSession _presentationScope;
    private readonly ChoiceHistory _choiceHistory;
    private readonly EpisodeSkipController _episodeSkipController;

    public ScenePresentation(
        IEpisodeNodeRunner nodeRunner,
        IPresentationView presentationView,
        RollbackHistory rollbackHistory,
        IVNLineAborter linePresentationAborter,
        PresentationShotResponseSystem shotResponseSystem,
        PresentationStage presentationStage,
        PresentationScopeSession presentationScope,
        ChoiceHistory choiceHistory,
        EpisodeSkipController episodeSkipController)
    {
        _nodeRunner = nodeRunner;
        _presentationView = presentationView;
        _rollbackHistory = rollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _shotResponseSystem = shotResponseSystem;
        _presentationStage = presentationStage;
        _presentationScope = presentationScope;
        _choiceHistory = choiceHistory;
        _episodeSkipController = episodeSkipController;
    }

    public void BeginScene()
    {
        _choiceHistory.ClearChoiceRecords();

        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();
    }
    
    public async Task PlayEpisodeAsync(string nodeName)
    {
        try
        {
            await _nodeRunner.RunNodeAsync(nodeName);
        }
        finally
        {
            // 현재 Yarn node의 수명이 끝났으므로
            // one-shot Episode Skip도 여기서 끝난다.
            _episodeSkipController.Reset();
        }
    }
    
    public void PrepareReplay()
    {
        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();
    }

    public async Task StopAsync()
    {
        _episodeSkipController.Reset();

        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        _linePresentationAborter.AbortCurrentVNLine();
        _rollbackHistory.ClearRollbackPoints();
        _shotResponseSystem.Clear();
        _presentationScope.End();
    }
}
