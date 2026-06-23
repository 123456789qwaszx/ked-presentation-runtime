using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Clear All Transitions",
    Order = -840)]
public sealed class ClearAllTransitionsCommandSpec : CommandSpecBase
{ }

public sealed class ClearAllTransitionsCommand : CommandBase
{
    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        PresentationTransitionClearUtility.ClearAll();
    }
}