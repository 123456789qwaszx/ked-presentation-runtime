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
        GuesthouseYarnContext yarnContext = ResolveYarnContext();

        ServiceSessionFlow session = new(content, screens, nodes, yarnContext);

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
