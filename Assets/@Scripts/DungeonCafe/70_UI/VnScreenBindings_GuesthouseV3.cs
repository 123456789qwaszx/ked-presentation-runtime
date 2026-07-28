public sealed partial class VnScreenBindings
{
    private CampaignStateV3 _guesthouseCampaign;

    public void StartGuesthouseCampaign(GuesthouseV3Bootstrap bootstrap)
    {
        GuesthouseV3Runtime runtime = bootstrap.BuildRuntime(this);

        _guesthouseCampaign = runtime.State;
        _hudSlotIndex = 0;

        ShowGuesthouseHud();
        RefreshGuesthouseHud("개업 준비");

        bootstrap.RunCampaign();
    }

    private void RefreshGuesthouseHud(string phaseLabel)
    => UpdateGuesthouseHud(
        GuesthouseHudSnapshot.FromCampaign(_guesthouseCampaign, phaseLabel));
    
    private void RefreshGuesthouseHud(ServiceSessionStateV3 session, string phaseLabel)
    => UpdateGuesthouseHud(
        GuesthouseHudSnapshot.FromSession(_guesthouseCampaign, session, _hudSlotIndex, phaseLabel));

    // 접객 슬롯 진행 표시용.
    // PresentBoardAsync에서 리셋하고 RequestAssignmentAsync에서 증가.
    private int _hudSlotIndex;
}