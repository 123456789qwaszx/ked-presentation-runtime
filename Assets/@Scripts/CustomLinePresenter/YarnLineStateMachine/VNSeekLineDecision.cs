public sealed class VNSeekLineDecision
{
    public VNSeekLineDecisionKind Kind { get; set; }
    public VNSeekKind SeekKind { get; set; }

    public string NodeName { get; set; }
    public string LineId { get; set; }

    public bool ShouldSkipVisualAndDispatchSeekNext { get; set; }
    public bool ShouldPassThroughPresentation { get; set; }
    public bool ShouldUseImmediateTransition { get; set; }
    public bool IsTargetLineReached { get; set; }

    public static VNSeekLineDecision NotSeeking()
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.NotSeeking,
            SeekKind = VNSeekKind.None,
        };
    }

    public static VNSeekLineDecision SkipVisualAndDispatchSeekNext(
        VNSeekKind seekKind,
        YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.SkipVisualAndDispatchSeekNext,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,

            ShouldSkipVisualAndDispatchSeekNext = true,
            ShouldPassThroughPresentation = true,
            ShouldUseImmediateTransition = true,
            IsTargetLineReached = false,
        };
    }

    public static VNSeekLineDecision TargetLineReachedAndResumePresentation(
        VNSeekKind seekKind,
        YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.PrepareTargetForVisualResume,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,

            ShouldSkipVisualAndDispatchSeekNext = false,
            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = true,
            IsTargetLineReached = true,
        };
    }
    
    // public static VNSeekLineDecision PendingTargetLine(VNSeekKind seekKind, YarnLineMeta meta)
    // {
    //     return new VNSeekLineDecision
    //     {
    //         Kind = VNSeekLineDecisionKind.TargetLineVisualResumeImmediate,
    //         SeekKind = seekKind,
    //         LineId = meta.lineId,
    //         
    //         ShouldPassThroughPresentation = true,
    //         ShouldUseImmediateTransition = true,
    //         IsTargetLineReached = true
    //     };
    // }
    
    
    public static VNSeekLineDecision TargetLineVisualResumeImmediate(VNSeekKind seekKind,
        YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.TargetLineVisualResumeImmediate,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,

            ShouldSkipVisualAndDispatchSeekNext = false,
            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = true,
            IsTargetLineReached = true,
        };
    }

    public static VNSeekLineDecision TargetLineVisualResumeNormal(VNSeekKind seekKind,
        YarnLineMeta meta)
    {
        return new VNSeekLineDecision
        {
            Kind = VNSeekLineDecisionKind.TargetLineVisualResumeNormal,
            SeekKind = seekKind,
            NodeName = meta.nodeName,
            LineId = meta.lineId,

            ShouldSkipVisualAndDispatchSeekNext = false,
            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = false,
            IsTargetLineReached = true,
        };
    }
    
    //
    // public static VNSeekLineDecision PendingTargetLine(
    //     VNSeekKind seekKind,
    //     YarnLineMeta meta)
    // {
    //     if (seekKind == VNSeekKind.Rollback)
    //     {
    //         return new VNSeekLineDecision
    //         {
    //             Kind = VNSeekLineDecisionKind.TargetLineVisualResumeImmediate,
    //             SeekKind = seekKind,
    //             NodeName = meta.nodeName,
    //             LineId = meta.lineId,
    //
    //             ShouldSkipVisualAndDispatchSeekNext = false,
    //             ShouldPassThroughPresentation = false,
    //             ShouldUseImmediateTransition = true,
    //             IsTargetLineReached = true,
    //         };
    //     }
    //
    //     if (seekKind == VNSeekKind.Load)
    //     {
    //         return new VNSeekLineDecision
    //         {
    //             Kind = VNSeekLineDecisionKind.TargetLineVisualResumeNormal,
    //             SeekKind = seekKind,
    //             NodeName = meta.nodeName,
    //             LineId = meta.lineId,
    //
    //             ShouldSkipVisualAndDispatchSeekNext = false,
    //             ShouldPassThroughPresentation = false,
    //             ShouldUseImmediateTransition = false,
    //             IsTargetLineReached = true,
    //         };
    //     }
    //
    //     return new VNSeekLineDecision
    //     {
    //         Kind = VNSeekLineDecisionKind.TargetLineVisualResumeNormal,
    //         SeekKind = seekKind,
    //         NodeName = meta.nodeName,
    //         LineId = meta.lineId,
    //
    //         ShouldSkipVisualAndDispatchSeekNext = false,
    //         ShouldPassThroughPresentation = false,
    //         ShouldUseImmediateTransition = false,
    //         IsTargetLineReached = true,
    //     };
    // }

    public override string ToString()
    {
        return $"seekDecision={Kind}, seekKind={SeekKind}, node={NodeName}, line={LineId}, " +
               $"dispatchNext={ShouldSkipVisualAndDispatchSeekNext}, passThrough={ShouldPassThroughPresentation}, " +
               $"immediate={ShouldUseImmediateTransition}, targetReached={IsTargetLineReached}";
    }
}