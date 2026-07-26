using UnityEngine;
using Yarn.Unity;

public class DungeonCafeBootstrap : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private GuesthouseContentBundleSO contentBundle;

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;

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

        ScenarioNodeRunner nodes = new(dialogueRunner);

        ServiceSessionFlow session = new(content, screens, nodes);

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
            content,
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
