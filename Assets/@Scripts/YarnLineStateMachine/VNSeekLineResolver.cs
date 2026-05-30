public sealed class VNSeekLineResolver
{
    private readonly LinePresentationState _advanceState;

    public VNSeekLineResolver(LinePresentationState advanceState)
    {
        _advanceState = advanceState;
    }

    public VNSeekLineDecision ResolveOnLineEntered(YarnLineMeta meta)
    {
        if (!_advanceState.IsSeekingActive)
            return VNSeekLineDecision.NotSeeking();

        VNSeekKind seekKind = _advanceState.SeekKind;
        bool isTarget = _advanceState.IsSeekTargetLine(meta);

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

        if (_advanceState.IsSeekingActive)
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
        _advanceState.AcceptPendingSeekTargetLine(lineId);
    }
}