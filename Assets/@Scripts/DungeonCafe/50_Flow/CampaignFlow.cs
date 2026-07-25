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
        EndingResolver endingResolver)
    {
        State = campaignState;
        Ending = null;
        
        _dayFlow = dayFlow;
        _presentation = presentation;
        _endingResolver = endingResolver;
    }

    public async YarnTask<CampaignEndingResult> RunAsync()
    {
        while (!State.IsFinished)
            await _dayFlow.RunDayAsync(State);

        Ending = _endingResolver.Resolve(State);

        await _presentation.PlayNodeAsync(Ending.NodeName);

        return Ending;
    }
}
