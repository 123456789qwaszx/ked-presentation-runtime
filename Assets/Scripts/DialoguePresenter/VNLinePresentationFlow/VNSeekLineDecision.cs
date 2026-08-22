public sealed class VNSeekLineDecision
{
    public bool IsTargetLineReached { get; private set; }
    public bool ShouldSkipVisual { get; private set; }

    public static VNSeekLineDecision NotSeeking()
    {
        return new VNSeekLineDecision { };
    }

    public static VNSeekLineDecision SkipVisualAndDispatchSeekNext()
    {
        return new VNSeekLineDecision
        {
            ShouldSkipVisual = true,
        };
    }

    public static VNSeekLineDecision TargetLineReached()
    {
        return new VNSeekLineDecision
        {
            IsTargetLineReached = true,
        };
    }
}