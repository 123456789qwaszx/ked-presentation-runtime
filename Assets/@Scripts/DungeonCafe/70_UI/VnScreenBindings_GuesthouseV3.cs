/// <summary>
/// 게스트하우스 v3 화면 바인딩 루트.
///
/// 캠페인/하루/접객/밤 플로우는 VnScreenBindings를 직접 호출한다.
/// 각 화면 요청은 VnScreenBindings_Guesthouse_* 파셜에 역할별로 나뉜다.
/// 이 파일은 캠페인 시작과 HUD 갱신만 담당한다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private CampaignStateV3 _guesthouseCampaign;

    // 에피소드 쪽 진입점.
    // 런타임을 먼저 조립해 캠페인 상태를 연결한 뒤 실제 진행을 시작한다.
    public void StartGuesthouseCampaign(
        GuesthouseV3Bootstrap bootstrap,
        INodePlayerV3 nodePlayer = null)
    {
        GuesthouseV3Runtime runtime =
            bootstrap.BuildRuntime(this, nodePlayer);

        _guesthouseCampaign = runtime.State;
        _hudSlotIndex = 0;

        ShowGuesthouseHud();
        RefreshGuesthouseHud("개업 준비");

        bootstrap.RunCampaign();
    }

    // ------------------------------------------------------------
    // HUD 갱신
    // ------------------------------------------------------------
    private void RefreshGuesthouseHud(string phaseLabel)
    {
        if (_guesthouseCampaign == null)
            return;

        UpdateGuesthouseHud(
            GuesthouseHudSnapshot.FromCampaign(
                _guesthouseCampaign,
                phaseLabel));
    }

    private void RefreshGuesthouseHud(
        ServiceSessionStateV3 session,
        string phaseLabel)
    {
        if (_guesthouseCampaign == null || session == null)
            return;

        UpdateGuesthouseHud(
            GuesthouseHudSnapshot.FromSession(
                _guesthouseCampaign,
                session,
                _hudSlotIndex,
                phaseLabel));
    }

    // 접객 슬롯 진행 표시용.
    // PresentBoardAsync에서 리셋하고 RequestAssignmentAsync에서 증가한다.
    private int _hudSlotIndex;
}