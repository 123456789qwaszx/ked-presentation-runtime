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
    void Awake()
    {
        BootstrapGuesthouseRuntime();

        RunCampaign();
    }

    private void BootstrapGuesthouseRuntime()
    {
        GuesthouseContentDB content = contentBundle.BuildContentDB();
        
        IGuesthouseScreenBindings screens = new HeadlessGuesthouseScreens();

        ScenarioNodeRunner scenarioNodeRunner = new(dialogueRunner);
        GuesthousePresentationPort port = new(scenarioNodeRunner, screens);
        
        ServiceSessionFlow session = new ServiceSessionFlow(content, port);
        
        NightPhaseFlow nightFlow = new(content, port);

        RotatingBookingPlanner rotatingBookingPlanner = new(content);
        DayCycleFlow dayFlow = new(
            content,
            rotatingBookingPlanner,
            session,
            nightFlow,
            port);
        
        _campaignState = new (content.Tuning, content.Maids);
        EndingResolver endingResolver = new (content.Tuning, content.ProtocolBySpecies);
        
        CampaignFlow campaign = new CampaignFlow(_campaignState, dayFlow, port, endingResolver);

        _guesthouseRuntime = new(content, session, campaign);
    }
    
    public async void RunCampaign()
    {
        CampaignEndingResult ending = await _guesthouseRuntime.Campaign.RunAsync();
        Debug.Log($"[GuesthouseRuntime] Ending={ending.EndingKey} ({ending.Title}) : {ending.Reason}");
    }
}
