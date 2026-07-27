using Yarn.Unity;

/// <summary>
/// 게스트하우스 v3 표현 계층 루트.
///
/// VnScreenBindings 가 IGuesthouseV3Screens 를 통째로 구현한다 — 각 메서드는
/// VnScreenBindings_Guesthouse_* 파셜에 흩어져 있고, 이 파일은 선언·구동·HUD 갱신만 담당한다.
///
/// 시스템 쪽에서 UI 로 들어오는 문은 이 인터페이스 하나뿐이다. 요청 DTO 가
/// 표시 정보를 전부 들고 오므로, 파셜들은 캠페인 상태를 직접 뒤지지 않는 것을 원칙으로 한다.
/// (HUD 스냅숏 생성만 예외로 캠페인을 읽는다.)
/// </summary>
public sealed partial class VnScreenBindings : IGuesthouseV3Screens
{
    private CampaignStateV3 _guesthouseCampaign;

    /// <summary>
    /// 에피소드 쪽 진입점. 부트스트랩에 자신을 화면 구현으로 주입하며 캠페인을 연다.
    /// 노드 재생은 기존 ScenarioNodeRunner 를 어댑터로 감싼다 (nodePlayer 가 null 이면
    /// 부트스트랩이 DialogueRunner 직결 어댑터를 쓴다).
    /// </summary>
    public void StartGuesthouseCampaign(GuesthouseV3Bootstrap bootstrap, INodePlayerV3 nodePlayer = null)
    {
        ShowGuesthouseHud();
        bootstrap.StartCampaign(this, nodePlayer);
        _guesthouseCampaign = bootstrap.Campaign;
        RefreshGuesthouseHud("개업 준비");
    }

    // ------------------------------------------------------------
    // HUD 갱신 — 각 파셜이 국면 전환 시 호출한다.
    // ------------------------------------------------------------
    private void RefreshGuesthouseHud(string phaseLabel)
    {
        if (_guesthouseCampaign == null)
            return;

        UpdateGuesthouseHud(GuesthouseHudSnapshot.FromCampaign(_guesthouseCampaign, phaseLabel));
    }

    private void RefreshGuesthouseHud(ServiceSessionStateV3 session, string phaseLabel)
    {
        if (_guesthouseCampaign == null || session == null)
            return;

        UpdateGuesthouseHud(GuesthouseHudSnapshot.FromSession(
            _guesthouseCampaign, session, _hudSlotIndex, phaseLabel));
    }

    // 접객 슬롯 진행 표시용. PresentBoardAsync 에서 리셋, RequestAssignmentAsync 에서 +1.
    private int _hudSlotIndex;
}

/// <summary>
/// 기존 ScenarioNodeRunner(EpisodePlayer 경유)를 v3 노드 재생 포트로 감싼다.
/// 부트스트랩의 DialogueRunner 직결 대신 에피소드 파이프라인을 태우고 싶을 때 쓴다.
/// </summary>
public sealed class ScenarioNodePlayerV3 : INodePlayerV3
{
    private readonly ScenarioNodeRunner _runner;

    public ScenarioNodePlayerV3(ScenarioNodeRunner runner) { _runner = runner; }

    public YarnTask PlayNodeAsync(string nodeName) => _runner.PlayNodeAsync(nodeName);
}
