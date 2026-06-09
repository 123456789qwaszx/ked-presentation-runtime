using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Signal",
    "Wait",
    SetOrder = 10,
    Order = 10)]
public sealed class WaitCommandSpec : CommandSpecBase
{
    public float duration = 0.2f;
}

public sealed class WaitCommand : CommandBase
{
    private readonly WaitCommandSpec _spec;

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public WaitCommand(WaitCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        yield return Wait(scope, _spec.duration);
    }
}