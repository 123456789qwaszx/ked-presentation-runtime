using System;
using UnityEngine;
using System.Collections;

public enum TransitionTimeoutPolicy
{
    ForceUncover,
    KeepCovered,
    CancelTransition,
}

[Serializable]
[CommandMenuHint("Transition", "Play Transition", Order = -950)]
public sealed class TransitionCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public TransitionTargetKind targetKind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";

    [Header("Opacity")]
    [Range(0f, 1f)] public float coveredAlpha = 1f;
    [Range(0f, 1f)] public float uncoveredAlpha = 0f;

    [Header("Durations")]
    public float coverDuration = 0.20f;
    public float uncoverDuration = 0.20f;
    public float holdAfterReadySeconds = 0.5f;

    [Header("Ease")]
    public AnimationCurve coverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve uncoverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Wait")]
    public bool wait = true;

    [Header("Ready Wait [Default:3sec]")]
    [Tooltip("0 이하이면 무한 대기. 0보다 크면 타임아웃 적용.")]
    public float readyTimeoutSeconds = 3f;
    public TransitionTimeoutPolicy timeoutPolicy = TransitionTimeoutPolicy.ForceUncover;

    [Header("Options")]
    public bool blockRaycastsWhileCovered = true;
    public bool resetToOpenAtStart = true;
}


public sealed class TransitionCommand : CommandBase
{
    private readonly TransitionCommandSpec _spec;
    private readonly TransitionCoordinator _coordinator;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionCommand(TransitionCoordinator coordinator, TransitionCommandSpec spec)
    {
        _coordinator = coordinator;
        _spec        = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        yield return _coordinator.Play(_spec, scope);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
    }
}