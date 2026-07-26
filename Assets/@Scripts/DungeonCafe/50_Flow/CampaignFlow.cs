using Yarn.Unity;

/// <summary>
/// 캠페인 진행. 3일 × 3접객을 소화한 뒤 엔딩을 확정한다.
/// </summary>
public sealed class CampaignFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly DayCycleFlow _dayFlow;
    private readonly EndingResolver _endingResolver;
    private readonly IServicePresentationPort _presentation;

    public CampaignState State { get; private set; }
    public CampaignEndingResult Ending { get; private set; }

    public CampaignFlow(
        CampaignState campaignState,
        DayCycleFlow dayFlow,
        IServicePresentationPort presentation,
        EndingResolver endingResolver = null)
    {
        State = campaignState;
        _dayFlow = dayFlow;
        _presentation = presentation;
        _endingResolver = endingResolver;
    }

    public CampaignState BeginCampaign()
    {
        State = new CampaignState(_content.Tuning, _content.Maids);
        Ending = null;

        return State;
    }

    public async YarnTask<CampaignEndingResult> RunAsync()
    {
        if (State == null)
            BeginCampaign();

        _presentation.ShowHud();
        _presentation.NotifyHud(
            GuesthouseHudSnapshot.ForDay(State, State.CurrentDay, "영업 준비"));

        while (!State.IsFinished)
            await _dayFlow.RunDayAsync(State);

        Ending = _endingResolver.Resolve(State);

        _presentation.NotifyHud(
            GuesthouseHudSnapshot.ForDay(State, State.CurrentDay, Ending.Title));

        // 엔딩 화면을 먼저 띄운 뒤 노드를 재생한다. 순서를 바꾸면 노드가 끝날 때까지 화면이 빈다.
        _presentation.PresentEnding(Ending, State);

        await _presentation.PlayNodeAsync(Ending.NodeName);

        await _presentation.WaitEndingDismissAsync();

        return Ending;
    }
}
