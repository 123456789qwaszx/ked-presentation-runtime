using UnityEngine;
using Yarn.Unity;

public class DungeonCafeBootstrap : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private GuesthouseContentBundleSO contentBundle;
    
    [SerializeField] private EpisodePlayer episodePlayer;

    private CampaignState _campaignState;

    private GuesthouseRuntime _guesthouseRuntime;

    public void DungeonCafeStart(VnScreenBindings vnScreenBindings)
    {
        BootstrapGuesthouseRuntime(vnScreenBindings);

        RunCampaign();
    }

    private void BootstrapGuesthouseRuntime(VnScreenBindings screens)
    {
        GuesthouseContentDB content = contentBundle.BuildContentDB();

        ScenarioNodeRunner nodes = new(episodePlayer);

        ServiceOptionSelector serviceOptionSelector = new();
        ServiceSettlementCalculator settlementCalculator = new(content.Tuning);
        ServiceSessionFlow session = new(
            content, 
            screens, 
            nodes, 
            serviceOptionSelector, 
            settlementCalculator);

        NightPhaseFlow nightFlow = new(content, screens, nodes);

        BookingPlanner bookingPlanner = new(content);

        DayCycleFlow dayFlow = new(
            bookingPlanner,
            session,
            nightFlow,
            screens,
            nodes);

        _campaignState = new(content.Tuning, content.Maids);
        EndingResolver endingResolver = new(content.Tuning, content.ProtocolBySpecies);

        CampaignFlow campaign = new(
            _campaignState,
            dayFlow,
            screens,
            nodes,
            endingResolver);

        _guesthouseRuntime = new(content, session, campaign);
    }

    public async void RunCampaign()
    {
        CampaignEndingResult ending = await _guesthouseRuntime.Campaign.RunAsync();
        Debug.Log($"[GuesthouseRuntime] Ending={ending.EndingKey} ({ending.Title}) : {ending.Reason}");
    }
}
