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


    private void BootstrapGuesthouseRuntime(VnScreenBindings vnScreenBindings)
    {
        GuesthouseContentDB content = contentBundle.BuildContentDB();
        
        ScenarioNodeRunner scenarioNodeRunner = new(dialogueRunner);
        
        GuesthousePresentationPort port = new(
            scenarioNodeRunner,
            vnScreenBindings,
            ResolveYarnContext());
        
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
    
    
    /// <summary>
    /// 대본이 참조할 표시용 변수를 밀어 넣을 통로를 만든다.
    /// 변수 저장소가 없으면 null 을 반환하고, 이 경우 문맥 주입만 생략된다.
    /// </summary>
    private GuesthouseYarnContext ResolveYarnContext()
    {
        if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
            return null;

        return new GuesthouseYarnContext(dialogueRunner.VariableStorage);
    }
}
