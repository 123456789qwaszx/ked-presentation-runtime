using System;
using UnityEngine;
using Yarn.Unity;

// 게스트하우스 v3 합성 루트.
// 이 클래스는 콘텐츠/상태/플로우를 조립하고 캠페인 실행을 시작하는 일만 담당한다.
public sealed class GuesthouseV3Bootstrap : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private GuesthouseV3TuningSO tuningAsset;

    [Header("Narrative")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private EpisodePlayer episodePlayer;

    [Header("Campaign")]
    [SerializeField] private ulong seed = 20260727UL;

    private GuesthouseV3Runtime _runtime;
    private bool _isRunning;

    public GuesthouseV3Runtime Runtime => _runtime;
    public CampaignStateV3 Campaign => _runtime?.State;

    // VnScreenBindings가 캠페인 상태를 먼저 잡을 수 있도록,
    // 객체 조립과 실제 실행을 분리한다.
    public GuesthouseV3Runtime BuildRuntime(
        VnScreenBindings screens,
        INodePlayerV3 nodePlayer = null)
    {
        if (screens == null)
            throw new ArgumentNullException(nameof(screens));

        GuesthouseTuningV3 tuning =
            tuningAsset != null
                ? tuningAsset.BuildTuning()
                : GuesthouseTuningV3.CreateStandard();

        GuesthouseV3ContentDB content =
            GuesthouseV3Content.Build();

        CampaignStateV3 state =
            new(content, tuning, seed);

        INodePlayerV3 nodes =
            nodePlayer
            ?? new ScenarioNodeRunner(episodePlayer);

        ServiceSessionFlowV3 sessionFlow =
            new(state, screens, nodes);

        DailyMonsterSelectorV3 monsterSelector =
            new(content);

        NightPrepFlowV3 nightPrepFlow =
            new(content, screens);

        NightMaidFlowV3 nightMaidFlow =
            new(
                content,
                sessionFlow,
                screens,
                nodes);

        NightPhaseFlowV3 nightFlow =
            new(
                nightPrepFlow,
                nightMaidFlow,
                screens,
                nodes);

        DayCycleFlowV3 dayFlow =
            new(
                monsterSelector,
                sessionFlow,
                nightFlow,
                screens,
                nodes);

        CampaignFlow campaignFlow =
            new(
                state,
                dayFlow,
                screens);

        _runtime = new GuesthouseV3Runtime(
            content,
            state,
            sessionFlow,
            nightFlow,
            dayFlow,
            campaignFlow);

        return _runtime;
    }

    public async void RunCampaign()
    {
        if (_runtime == null)
            throw new InvalidOperationException(
                "BuildRuntime()을 먼저 호출해야 합니다.");

        if (_isRunning)
            throw new InvalidOperationException(
                "게스트하우스 캠페인이 이미 실행 중입니다.");

        _isRunning = true;

        try
        {
            EndingKindV3 ending =
                await _runtime.Campaign.RunAsync();

            Debug.Log(
                $"[GuesthouseV3] Ending={ending} " +
                $"Lifetime={_runtime.State.Ledger.Lifetime}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            _isRunning = false;
        }
    }
}
