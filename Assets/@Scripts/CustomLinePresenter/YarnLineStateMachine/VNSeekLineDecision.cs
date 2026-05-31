public sealed class VNSeekLineDecision
{
    public VNSeekKind SeekKind { get; set; }

    public bool ShouldSkipVisualAndDispatchSeekNext { get; private set; }
    public bool ShouldPassThroughPresentation { get; private set; }
    public bool ShouldUseImmediateTransition { get; private set; }
    public bool IsTargetLineReached { get; private set; }

    public static VNSeekLineDecision NotSeeking()
    {
        return new VNSeekLineDecision
        {
            SeekKind = VNSeekKind.None,
        };
    }

    public static VNSeekLineDecision SkipVisualAndDispatchSeekNext(VNSeekKind seekKind)
    {
        return new VNSeekLineDecision
        {
            SeekKind = seekKind,

            ShouldPassThroughPresentation = true,
            ShouldUseImmediateTransition = true,
            ShouldSkipVisualAndDispatchSeekNext = true,
            IsTargetLineReached = false,
        };
    }

    public static VNSeekLineDecision TargetLineReachedAndResumePresentation(VNSeekKind seekKind)
    {
        return new VNSeekLineDecision
        {
            SeekKind = seekKind,

            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = false,
            IsTargetLineReached = true,
        };
    }
    
    public static VNSeekLineDecision TargetLineVisualResumeImmediate(VNSeekKind seekKind)
    {
        return new VNSeekLineDecision
        {
            SeekKind = seekKind,

            ShouldPassThroughPresentation = true,
            ShouldUseImmediateTransition = true,
            IsTargetLineReached = true,
        };
    }

    public static VNSeekLineDecision TargetLineVisualResumeNormal(VNSeekKind seekKind)
    {
        return new VNSeekLineDecision
        {
            SeekKind = seekKind,

            ShouldPassThroughPresentation = false,
            ShouldUseImmediateTransition = false,
            IsTargetLineReached = true,
        };
    }
}