using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public abstract class ShotIntentCommandSpecBase : CommandSpecBase
{
    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;
}

public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    protected readonly PresentationResponseRig rig;
    protected readonly TSpec spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;

    private bool HasClaimedRig { get; set; }

    public override bool WaitForCompletion => spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected ShotIntentCommandBase(PresentationResponseRig rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ClaimRig(scope);

        if (spec.duration <= 0f ||
            PresentationShotIntentMath.ApproximatelyEqual(_fromState, _toState))
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    PresentationIntentState state =
                        PresentationShotIntentMath.Interpolate(_fromState, _toState, t);

                    rig.ApplyToAllBindings(state);
                },
                1f,
                spec.duration)
            .SetEase(spec.ease)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(CommitFinalState);

        if (spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!HasClaimedRig)
            ClaimRig(scope);

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected abstract PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope);
    

    private void ClaimRig(CommandRunScope scope)
    {
        DOTween.Kill(rig, true);

        _fromState = rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        HasClaimedRig = true;
    }

    private void CommitFinalState()
    {
        rig.ApplyToAllBindings(_toState);

        HasClaimedRig = false;
    }
}