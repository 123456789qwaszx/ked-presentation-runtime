// 로드 리플레이의 연출 쪽 상태. 런처가 재생 계획을 올려 두면(PrepareLoad),
// SceneRunner가 복원 장면에 들어설 때 그것으로 시크를 연다(BeginLoadReplay).
public sealed class ProgressionReplayState
{
    private readonly VNLinePresentationState _lineState;
    private readonly ChoiceHistory _choiceHistory;

    private PresentationResumePoint _stagedPlan;

    public ProgressionReplayState(
        VNLinePresentationState lineState,
        ChoiceHistory choiceHistory)
    {
        _lineState = lineState;
        _choiceHistory = choiceHistory;
    }

    public void PrepareLoad(PresentationResumePoint plan)
    {
        _stagedPlan = plan;
    }

    public bool IsSeekingActive => _lineState.IsSeekingActive;

    public void BeginLoadReplay()
    {
        _choiceHistory.RestoreChoices(_stagedPlan.YarnChoices);

        LineTarget target = _stagedPlan.Target;

        _lineState.BeginLoadSeek(
            target.NodeName,
            target.LineId,
            target.Occurrence);

        _stagedPlan = null;
    }

    public void ClearSeek() =>
        _lineState.ClearSeek();
}
