public sealed class VNSeekLineDecision
{
    public VNSeekLineDecisionKind Kind { get; set; }
    public VNSeekKind SeekKind { get; set; }

    public string NodeName { get; set; }
    public string LineId { get; set; }

    public bool ShouldDispatchSeekNext { get; set; }
    public bool ShouldPassThroughPresentation { get; set; }
    public bool ShouldUseImmediateTransition { get; set; }
    public bool ShouldConsumeTargetLine { get; set; }

    public static VNSeekLineDecision NotSeeking()
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.NotSeeking,
            SeekKind = VNSeekKind.None,
        };
    }

    public static VNSeekLineDecision PassThrough(VNSeekKind seekKind, YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.PassThrough,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,
            ShouldDispatchSeekNext = true,
            ShouldPassThroughPresentation = true,
            ShouldUseImmediateTransition = true,
        };
    }

    public static VNSeekLineDecision TargetReached(VNSeekKind seekKind, YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.TargetReached,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,
            ShouldDispatchSeekNext = false,
            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = true,
        };
    }

    public static VNSeekLineDecision PendingTargetLine(VNSeekKind seekKind, string lineId)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.PendingTargetLine,
            SeekKind = seekKind,
            LineId = lineId,
            ShouldConsumeTargetLine = true,
            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = true,
        };
    }

    public override string ToString()
    {
        return $"seekDecision={Kind}, seekKind={SeekKind}, node={NodeName}, line={LineId}, " +
               $"dispatchNext={ShouldDispatchSeekNext}, passThrough={ShouldPassThroughPresentation}, " +
               $"immediate={ShouldUseImmediateTransition}, consume={ShouldConsumeTargetLine}";
    }
}