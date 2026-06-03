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

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    protected readonly PresentationResponseRig rig;
    protected readonly TSpec spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected ShotIntentCommandBase(PresentationResponseRig rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (rig == null)
            yield break;

        if (spec.killTween)
            KillRigTween(true);

        _fromState = rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (spec.duration <= 0f ||
            PresentationShotIntentMath.ApproximatelyEqual(_fromState, _toState))
        {
            Commit(_toState);
            ClearRuntimeState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || rig == null)
                        return;

                    PresentationIntentState state =
                        PresentationShotIntentMath.Interpolate(_fromState, _toState, t);

                    rig.ApplyToAllBindings(state);
                },
                1f,
                spec.duration)
            .SetEase(spec.ease)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || rig == null)
                    return;

                Commit(_toState);
                ClearRuntimeState();
            });

        if (spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (rig == null)
            return;

        KillRigTween(false);

        _fromState = rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        Commit(_toState);
        ClearRuntimeState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState || rig == null)
            return;

        KillRigTween(false);
        Commit(_toState);
        ClearRuntimeState();
    }

    protected abstract PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope);

    private void Commit(in PresentationIntentState state)
    {
        if (rig == null)
            return;

        rig.ApplyToAllBindings(state);
    }

    private void KillRigTween(bool complete)
    {
        if (rig == null)
            return;

        DOTween.Kill(rig, complete);
        _tween = null;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;
        _tween = null;
    }
}