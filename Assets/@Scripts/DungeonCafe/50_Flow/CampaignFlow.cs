using Yarn.Unity;

// 캠페인 전체 진행.
// 하루 사이클을 반복, 엔딩 체크.
public sealed class CampaignFlow
{
    private readonly CampaignStateV3 _campaign;
    private readonly DayCycleFlowV3 _dayFlow;
    private readonly VnScreenBindings _screens;

    public CampaignFlow(
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
        _screens.UpdateGuesthouseHud(GuesthouseHudSnapshot.FromCampaign(_campaign, "개업 준비"));
        
        while (_campaign.CurrentDayNumber <= _campaign.Content.CampaignDayCount 
               && _campaign.Ending == EndingKindV3.None)
        {
            await _dayFlow.RunDayAsync(_campaign);

            EndingKindV3 ending = EndingResolverV3.ResolveImmediate(_campaign);
            _campaign.Ending = ending;

            if (_campaign.Ending != EndingKindV3.None)
                break;
            
            _campaign.Ledger.StartNewDay();
            _campaign.Abilities.StartNewDay();
            _campaign.CurrentDayNumber++;
        }

        if (_campaign.Ending == EndingKindV3.None)
            _campaign.Ending = EndingResolverV3.ResolveCampaignEnd(_campaign);

        _campaign.Phase = CampaignPhaseV3.Finished;
        await _screens.PresentEndingAsync(_campaign, _campaign.Ending);

        return _campaign.Ending;
    }
}