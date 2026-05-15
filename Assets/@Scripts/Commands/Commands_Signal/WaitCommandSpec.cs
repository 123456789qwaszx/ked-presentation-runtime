using System;
using UnityEngine;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Signal",
    "Wait",
    SetOrder = 10,
    Order = 10)]
public sealed class WaitCommandSpec : CommandSpecBase
{
    [Header("Time")]
    public float seconds = 0.2f;
}

public sealed class CpsWaitCommand : CommandBase
{
    private readonly float _seconds;

    public CpsWaitCommand(float seconds)
    {
        _seconds = Mathf.Max(0f, seconds);
    }

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        yield return Wait(scope, _seconds);
    }

    protected override void OnSkip(CommandRunScope scope) { }
}