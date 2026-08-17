using System.Threading.Tasks;

public sealed class EpisodePlayer
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly VnScreenBindings _vnScreenBindings;
    private readonly RollbackHistory _nodeRollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly BacklogRecorder _backlogRecorder;
    private readonly PresentationShotResponseSystem _presentationResponseRig;
    private readonly PresentationScopeSession _presentationScopeSession;

    public EpisodePlayer(
        IEpisodeNodeRunner nodeRunner,
        VnScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        PresentationShotResponseSystem presentationResponseRig,
        PresentationScopeSession presentationScopeSession)
    {
        _nodeRunner = nodeRunner;
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _presentationResponseRig = presentationResponseRig;
        _presentationScopeSession = presentationScopeSession;
    }

    public async Task StartGameAsync(string nodeName)
    {
        await StopDialogueAsync();

        _vnScreenBindings.GoToPresentationView();
        _presentationScopeSession.ClearStage();
        _presentationScopeSession.Start();

        await _nodeRunner.StartAsync(nodeName);
    }

    private async Task StopDialogueAsync()
    {
        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        _linePresentationAborter.AbortCurrentVnLine();

        _nodeRollbackHistory.ClearRollbackPoints();
        _backlogRecorder.ClearBacklog();

        _presentationResponseRig.Clear();
        _presentationScopeSession.End();
    }
}