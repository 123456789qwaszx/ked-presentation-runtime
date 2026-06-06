using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Clear All Transitions",
    Order = -840)]
public sealed class ClearAllTransitionsCommandSpec : CommandSpecBase
{
}

public sealed class ClearAllTransitionsCommand : CommandBase
{
    private readonly ClearAllTransitionsCommandSpec _spec;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ClearAllTransitionsCommand(ClearAllTransitionsCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
    }
}