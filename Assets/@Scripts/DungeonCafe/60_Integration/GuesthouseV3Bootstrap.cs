using System;
using UnityEngine;
using Yarn.Unity;

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

    public GuesthouseV3Runtime BuildRuntime(
        VnScreenBindings screens)
    {
        GuesthouseTuningV3 tuning =
            tuningAsset != null
                ? tuningAsset.BuildTuning()
                : GuesthouseTuningV3.CreateStandard();

        GuesthouseV3ContentDB content = GuesthouseV3Content.Build();

        CampaignStateV3 state = new(content, tuning, seed);

        INodePlayerV3 nodes = episodePlayer;

        ServiceSessionFlowV3 sessionFlow = new(state, screens, nodes);

        DailyMonsterSelectorV3 monsterSelector = new(content);

        NightPrepFlowV3 nightPrepFlow = new(content, screens);

        NightMaidFlowV3 nightMaidFlow = new(
            content,
            sessionFlow,
            screens,
            nodes);

        NightPhaseFlowV3 nightFlow = new(
            nightPrepFlow,
            nightMaidFlow,
            screens,
            nodes);
        
        DayCycleFlowV3 dayFlow = new(
            monsterSelector,
            sessionFlow,
            nightFlow,
            screens,
            nodes);

        CampaignFlow campaignFlow = new(
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
        EndingKindV3 ending =
            await _runtime.Campaign.RunAsync();

        Debug.Log(
            $"[GuesthouseV3] Ending={ending} " +
            $"Lifetime={_runtime.State.Ledger.Lifetime}");
    }
}
