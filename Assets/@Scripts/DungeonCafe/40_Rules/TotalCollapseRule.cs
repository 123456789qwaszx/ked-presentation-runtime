/// <summary>완전 붕괴(200) 처리. </summary>
public readonly struct TotalCollapseOutcome
{
    public bool Rescued { get; }
    public string NodeToPlay { get; }
    public string AftereffectId { get; }
    public string AccidentQuirkId { get; }
    public TotalCollapseOutcome(bool rescued, string node, string aftereffectId, string quirkId)
    { Rescued = rescued; NodeToPlay = node; AftereffectId = aftereffectId; AccidentQuirkId = quirkId; }
}

public static class TotalCollapseRule
{
    public const string HollowQuirkId = "qk_acc_hollowmark";

    public static TotalCollapseOutcome Resolve(
        MaidState maid, SpeciesProtocol protocol, DungeonCafeTuning tuning)
    {
        bool rescued = maid.HasRescueTicket;
        maid.MarkTotalCollapse(rescued);

        if (rescued)
        {
            // 100 초과 축 전부 100 복귀. (§5)
            for (int i = 0; i < BurdenAxes.Count; i++)
            {
                BurdenAxis axis = BurdenAxes.FromIndex(i);
                if (maid.Gauge.Get(axis) > tuning.ControlLossThreshold)
                    maid.Gauge.SetValue(axis, tuning.ControlLossThreshold);
            }

            return new TotalCollapseOutcome(
                rescued: true,
                node: $"Rescue_{maid.MaidId}",
                aftereffectId: null,
                quirkId: HollowQuirkId);
        }

        return new TotalCollapseOutcome(
            rescued: false,
            node: protocol != null ? protocol.CollapseEndingNodeName : null,
            aftereffectId: null,
            quirkId: null);
    }
}