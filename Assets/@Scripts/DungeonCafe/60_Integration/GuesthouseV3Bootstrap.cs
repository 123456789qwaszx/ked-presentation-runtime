using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 씬 합성 루트 v3. 기존 GuesthouseRuntime 자리에 이 컴포넌트를 놓는다.
/// UI 어댑터(IGuesthouseV3Screens 구현)가 준비되기 전에는 headlessSmoke 로 무UI 검증이 가능하다.
/// </summary>
public sealed class GuesthouseV3Bootstrap : MonoBehaviour
{
    [SerializeField] private GuesthouseV3TuningSO tuningAsset;
    [SerializeField] private DialogueRunner dialogueRunner;
    [Tooltip("체크 시 Start 에서 헤드리스 캠페인 1회를 돌리고 로그를 남긴다.")]
    [SerializeField] private bool headlessSmoke;
    [SerializeField] private ulong seed = 20260727UL;

    public CampaignStateV3 Campaign { get; private set; }
    public CampaignFlowV3 Flow { get; private set; }

    /// <summary>UI 계층이 화면 구현을 주입하며 캠페인을 시작한다.</summary>
    public void StartCampaign(IGuesthouseV3Screens screens, INodePlayerV3 nodes)
    {
        GuesthouseTuningV3 tuning = tuningAsset != null
            ? tuningAsset.BuildTuning() : GuesthouseTuningV3.CreateStandard();

        Campaign = new CampaignStateV3(GuesthouseV3Content.Build(), tuning, seed);
        Flow = new CampaignFlowV3(Campaign, screens, nodes ?? new DialogueRunnerNodePlayer(dialogueRunner));
        _ = Flow.RunAsync();
    }

    private void Start()
    {
        if (!headlessSmoke) return;
        var screens = new HeadlessV3Screens(HeadlessPolicyV3.Ideal);
        StartCampaign(screens, new HeadlessNodePlayerV3());
        Debug.Log($"[GuesthouseV3] smoke ending={Campaign.Ending} lifetime={Campaign.Ledger.Lifetime} " +
                  $"services={screens.ServiceCount} landings={screens.LandingCount} depth={screens.DepthEntryCount}");
        Campaign.ReleaseCounters();
    }
}

/// <summary>기존 DialogueRunner 를 INodePlayerV3 로 감싼다. 미작성 노드는 경고 후 통과 (기존 규칙 계승).</summary>
public sealed class DialogueRunnerNodePlayer : INodePlayerV3
{
    private readonly DialogueRunner _runner;
    public DialogueRunnerNodePlayer(DialogueRunner runner) { _runner = runner; }

    public async YarnTask PlayNodeAsync(string nodeName)
    {
        if (_runner == null || string.IsNullOrEmpty(nodeName)) return;

        if (_runner.Dialogue == null || !_runner.Dialogue.NodeExists(nodeName))
        {
            Debug.LogWarning($"[GuesthouseV3] 미작성 노드 통과: {nodeName}");
            return;
        }

        _runner.StartDialogue(nodeName);
        while (_runner.IsDialogueRunning)
            await YarnTask.Yield();
    }
}
