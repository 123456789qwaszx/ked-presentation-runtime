using Yarn.Unity;

/// <summary>
/// 캠페인 진행. 3일 x 3접객을 소화한 뒤 엔딩을 확정한다.
///
/// 화면 요청은 VnScreenBindings 로, 노드 재생은 ScenarioNodeRunner 로 직접 나간다.
/// 표현 계층 포트는 두지 않는다. 두 번째 구현을 굴릴 계획이 없으므로 홉만 늘 뿐이다.
/// </summary>
public sealed class CampaignFlow
{
    private readonly DayCycleFlow _dayFlow;
    private readonly EndingResolver _endingResolver;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    private CampaignState State { get; }
    private CampaignEndingResult Ending { get; set; }

    public CampaignFlow(
        CampaignState campaignState,
        DayCycleFlow dayFlow,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes,
        EndingResolver endingResolver)
    {
        State = campaignState;
        _dayFlow = dayFlow;
        _screens = screens;
        _nodes = nodes;
        _endingResolver = endingResolver;
    }

    public async YarnTask<CampaignEndingResult> RunAsync()
    {
        _screens.ShowGuesthouseHud();
        _screens.UpdateGuesthouseHud(GuesthouseHudSnapshot.ForDay(State, State.CurrentDay, "영업 준비"));

        while (!State.IsFinished)
            await _dayFlow.RunDayAsync(State);

        Ending = _endingResolver.Resolve(State);

        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.ForDay(State, State.CurrentDay, Ending.Title));

        // 엔딩 화면을 먼저 띄운 뒤 노드를 재생한다.
        _screens.PresentEnding(Ending, State);
        await _nodes.PlayNodeAsync(Ending.NodeName);
        await _screens.WaitEndingDismissAsync();

        return Ending;
    }
}
