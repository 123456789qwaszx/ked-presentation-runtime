using Ked.Progression;

public sealed class ProgressionReplayState : ISceneReplayState
{
    private readonly VNLinePresentationState _lineState;
    private readonly ChoiceHistory _choiceHistory;

    private SavedLoadPlan _stagedPlan;

    public ProgressionReplayState(
        VNLinePresentationState lineState,
        ChoiceHistory choiceHistory)
    {
        _lineState = lineState;
        _choiceHistory = choiceHistory;
    }

    public void PrepareLoad(SavedLoadPlan plan)
    {
        _stagedPlan = plan;
    }

    public bool IsSeekingActive => _lineState.IsSeekingActive;

    public void BeginLoadReplay()
    {
        _choiceHistory.RestoreChoices(_stagedPlan.YarnChoices);

        SaveLineTarget target = _stagedPlan.Target;

        _lineState.BeginLoadSeek(
            target.NodeName,
            target.LineId,
            target.Occurrence);

        _stagedPlan = null;
    }

    public void ClearSeek() =>
        _lineState.ClearSeek();
}