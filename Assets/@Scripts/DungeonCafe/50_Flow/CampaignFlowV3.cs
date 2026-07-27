using Yarn.Unity;

// 캠페인 전체 진행.
// 현재 날짜의 하루 사이클을 반복하고, 즉시 엔딩 또는 15일 종료 엔딩을 확정한다.
// 게시판/접객/밤의 세부 순서는 DayCycleFlowV3 가 담당한다.
public sealed class CampaignFlowV3
{
    private readonly CampaignStateV3 _campaign;
    private readonly DayCycleFlowV3 _dayFlow;
    private readonly VnScreenBindings _screens;

    public CampaignFlowV3(
        CampaignStateV3 campaign,
        DayCycleFlowV3 dayFlow,
        VnScreenBindings screens)
    {
        _campaign = campaign;
        _dayFlow = dayFlow;
        _screens = screens;
    }

    public async YarnTask<EndingKindV3> RunAsync()
    {
        _screens.ShowGuesthouseHud();
        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.FromCampaign(_campaign, "개업 준비"));

        while (HasDayToRun() && _campaign.Ending == EndingKindV3.None)
        {
            await _dayFlow.RunDayAsync(_campaign);

            ResolveImmediateEnding();

            if (_campaign.Ending != EndingKindV3.None)
                break;

            CompleteDay();
        }

        ResolveCampaignEnding();

        _campaign.Phase = CampaignPhaseV3.Finished;
        await _screens.PresentEndingAsync(_campaign, _campaign.Ending);

        return _campaign.Ending;
    }

    private bool HasDayToRun()
        => _campaign.CurrentDayNumber <= _campaign.Content.CampaignDayCount;

    private void ResolveImmediateEnding()
    {
        EndingKindV3 ending = EndingResolverV3.ResolveImmediate(_campaign);

        if (ending != EndingKindV3.None)
            _campaign.Ending = ending;
    }

    private void ResolveCampaignEnding()
    {
        if (_campaign.Ending == EndingKindV3.None)
            _campaign.Ending = EndingResolverV3.ResolveCampaignEnd(_campaign);
    }

    // 오늘 장부와 일일 능력 사용 횟수를 정리하고 다음 날로 넘어간다.
    private void CompleteDay()
    {
        _campaign.Ledger.StartNewDay();
        _campaign.Abilities.StartNewDay();
        _campaign.CurrentDayNumber++;
    }
}