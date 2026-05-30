public sealed class VNSeekLineResolver
{
    private readonly LinePresentationAdvanceState _advanceState;

    public VNSeekLineResolver(LinePresentationAdvanceState advanceState)
    {
        _advanceState = advanceState;
    }

    public VNSeekLineDecision ResolveOnLineEntered(YarnLineMeta meta)
    {
        if (!_advanceState.IsSeeking)
            return VNSeekLineDecision.NotSeeking();

        VNSeekKind seekKind = _advanceState.SeekKind;
        bool isTarget = _advanceState.IsSeekTarget(meta);

        if (isTarget)
        {
            _advanceState.MarkSeekTargetReached(meta);
            return VNSeekLineDecision.TargetReached(seekKind, meta);
        }

        return VNSeekLineDecision.PassThrough(seekKind, meta);
    }

    public VNSeekLineDecision ResolveBeforePresentation(string lineId)
    {
        if (_advanceState.IsPendingSeekTargetLine(lineId))
        {
            return VNSeekLineDecision.PendingTargetLine(
                _advanceState.SeekKind,
                lineId);
        }

        if (_advanceState.IsSeeking)
        {
            return new VNSeekLineDecision
            {
                Kind = VNSeekLineDecisionKind.PassThrough,
                SeekKind = _advanceState.SeekKind,
                LineId = lineId,
                ShouldDispatchSeekNext = true,
                ShouldPassThroughPresentation = true,
                ShouldUseImmediateTransition = true,
            };
        }

        return VNSeekLineDecision.NotSeeking();
    }

    public void ConsumeTargetLine(string lineId)
    {
        _advanceState.ConsumeSeekTargetLine(lineId);
    }
}