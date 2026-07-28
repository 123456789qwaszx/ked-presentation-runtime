// 상위 에피소드나 세이브 계층에서는 Campaign만 잡고 전체 진행 시작.
public sealed class DungeonCafeRuntime
{
    public DungeonCafeContentDB Content { get; }
    public CampaignState State { get; }

    public ServiceSessionFlow Session { get; }
    public NightPhaseFlow Night { get; }
    public DayCycleFlow Day { get; }
    public CampaignFlow Campaign { get; }

    public DungeonCafeRuntime(
        DungeonCafeContentDB content,
        CampaignState state,
        ServiceSessionFlow session,
        NightPhaseFlow night,
        DayCycleFlow day,
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