public sealed partial class VnScreenBindings
{
    private CampaignState _dungeonCafeCampaign;

    public void StartDungeonCafeCampaign(DungeonCafeBootstrap bootstrap)
    {
        DungeonCafeRuntime runtime = bootstrap.BuildRuntime(this);

        _dungeonCafeCampaign = runtime.State;
        _hudSlotIndex = 0;

        DungeonCafeHud();
        RefreshDungeonCafeHud("개업 준비");

        bootstrap.RunCampaign();
    }

    private void RefreshDungeonCafeHud(string phaseLabel)
    => UpdateDungeonCafeHud(
        DungeonCafeHudSnapshot.FromCampaign(_dungeonCafeCampaign, phaseLabel));
    
    private void RefreshDungeonCafeHud(ServiceSessionState session, string phaseLabel)
    => UpdateDungeonCafeHud(
        DungeonCafeHudSnapshot.FromSession(_dungeonCafeCampaign, session, _hudSlotIndex, phaseLabel));

    // 접객 슬롯 진행 표시용.
    // PresentBoardAsync에서 리셋하고 RequestAssignmentAsync에서 증가.
    private int _hudSlotIndex;
}