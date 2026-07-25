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
        GuesthouseContentDB content,
        DayCycleFlow dayFlow,
        IServicePresentationPort presentation,
        EndingResolver endingResolver = null)
    {
        _content = content;
        _dayFlow = dayFlow;
        _presentation = presentation;
        _endingResolver = endingResolver
                          ?? new EndingResolver(content.Tuning, content.ProtocolBySpecies);
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

        while (!State.IsFinished)
            await _dayFlow.RunDayAsync(State);

        Ending = _endingResolver.Resolve(State);

        await _presentation.PlayNodeAsync(Ending.NodeName);

        return Ending;
    }
}
