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

    [Header("Clock")]
    public bool respectTimeScale = true; // ctx.TimeScale 반영
}

public sealed class CpsWaitCommand : CommandBase
{
    private readonly ITimeSource _time;
    private readonly float _seconds;
    private readonly bool _respectTimeScale;

    public CpsWaitCommand(ITimeSource time, float seconds, bool respectTimeScale = true)
    {
        _time = time;
        _seconds = Mathf.Max(0f, seconds);
        _respectTimeScale = respectTimeScale;
    }

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_time == null || _seconds <= 0f)
            yield break;

        float remaining = _seconds;

        while (remaining > 0f)
        {
            float dt = _time.UnscaledDeltaTime;
            if (_respectTimeScale)
                dt *= (scope != null ? scope.TimeScale : 1f);

            remaining -= dt;
            yield return null;
        }
    }

    protected override void OnSkip(CommandRunScope scope) { }
}