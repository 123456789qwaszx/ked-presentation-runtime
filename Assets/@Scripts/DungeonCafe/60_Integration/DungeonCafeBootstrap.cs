using System;
using UnityEngine;
using Yarn.Unity;

public sealed class DungeonCafeBootstrap : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private DungeonCafeTuningSO tuningAsset;

    [Header("Narrative")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private EpisodePlayer episodePlayer;

    [Header("Campaign")]
    [SerializeField] private ulong seed = 20260727UL;

    private DungeonCafeRuntime _runtime;
    private bool _isRunning;

    public DungeonCafeRuntime BuildRuntime(
        VnScreenBindings screens)
    {
        DungeonCafeTuning tuning =
            tuningAsset != null
                ? tuningAsset.BuildTuning()
                : DungeonCafeTuning.CreateStandard();

        DungeonCafeContentDB content = DungeonCafeContent.Build();

        CampaignState state = new(content, tuning, seed);

        IDungeonCafeNodePlayer dungeonCafeNodes = episodePlayer;

        ServiceSessionFlow sessionFlow = new(state, screens, dungeonCafeNodes);

        DailyMonsterSelector monsterSelector = new(content);

        NightPrepFlow nightPrepFlow = new(content, screens);

        NightMaidFlow nightMaidFlow = new(
            content,
            sessionFlow,
            screens,
            dungeonCafeNodes);

        NightPhaseFlow nightFlow = new(
            nightPrepFlow,
            nightMaidFlow,
            screens,
            dungeonCafeNodes);
        
        DayCycleFlow dayFlow = new(
            monsterSelector,
            sessionFlow,
            nightFlow,
            screens,
            dungeonCafeNodes);

        CampaignFlow campaignFlow = new(
            state,
            dayFlow,
            screens);

        _runtime = new DungeonCafeRuntime(
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
        EndingKind ending =
            await _runtime.Campaign.RunAsync();

        Debug.Log(
            $"[DungeonCafe] Ending={ending} " +
            $"Lifetime={_runtime.State.Ledger.Lifetime}");
    }
}
