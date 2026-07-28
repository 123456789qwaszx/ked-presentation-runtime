using Yarn.Unity;

// 캠페인 전체 진행.
// 하루 사이클을 반복, 엔딩 체크.
public sealed class CampaignFlow
{
    private readonly CampaignState _campaign;
    private readonly DayCycleFlow _dayFlow;
    private readonly VnScreenBindings _screens;

    public CampaignFlow(
        CampaignState campaign,
        DayCycleFlow dayFlow,
        VnScreenBindings screens)
    {
        _campaign = campaign;
        _dayFlow = dayFlow;
        _screens = screens;
    }

    public async YarnTask<EndingKind> RunAsync()
    {
        _screens.DungeonCafeHud();
        _screens.UpdateDungeonCafeHud(DungeonCafeHudSnapshot.FromCampaign(_campaign, "개업 준비"));
        
        while (_campaign.CurrentDayNumber <= _campaign.Content.CampaignDayCount 
               && _campaign.Ending == EndingKind.None)
        {
            await _dayFlow.RunDayAsync(_campaign);

            EndingKind ending = EndingResolver.ResolveImmediate(_campaign);
            _campaign.Ending = ending;

            if (_campaign.Ending != EndingKind.None)
                break;
            
            _campaign.Ledger.StartNewDay();
            _campaign.Abilities.StartNewDay();
            _campaign.CurrentDayNumber++;
        }

        if (_campaign.Ending == EndingKind.None)
            _campaign.Ending = EndingResolver.ResolveCampaignEnd(_campaign);

        _campaign.Phase = CampaignPhase.Finished;
        await _screens.PresentEndingAsync(_campaign, _campaign.Ending);

        return _campaign.Ending;
    }
}