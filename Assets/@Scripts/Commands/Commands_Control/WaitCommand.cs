using System;
using System.Collections;
using UnityEngine;

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

    public WaitCommand(WaitCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        float seconds = Mathf.Max(0f, _spec.duration);
        float elapsed = 0f;
        
        while (elapsed < seconds)
        {
            if (scope.Token.IsCancellationRequested)
                yield break;

            elapsed += Time.unscaledDeltaTime * scope.TimeScale;

            yield return null;
        }
    }
}