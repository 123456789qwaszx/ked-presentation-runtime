// 게스트하우스 v3 런타임 묶음.
// 상위 에피소드나 세이브 계층에서는 Campaign만 잡으면 전체 진행을 시작할 수 있다.
public sealed class GuesthouseV3Runtime
{
    public GuesthouseV3ContentDB Content { get; }
    public CampaignStateV3 State { get; }

    public ServiceSessionFlowV3 Session { get; }
    public NightPhaseFlowV3 Night { get; }
    public DayCycleFlowV3 Day { get; }
    public CampaignFlow Campaign { get; }

    public GuesthouseV3Runtime(
        GuesthouseV3ContentDB content,
        CampaignStateV3 state,
        ServiceSessionFlowV3 session,
        NightPhaseFlowV3 night,
        DayCycleFlowV3 day,
        CampaignFlow campaign)
    {
        Content = content;
        State = state;
        Session = session;
        Night = night;
        Day = day;
        Campaign = campaign;
    }
}