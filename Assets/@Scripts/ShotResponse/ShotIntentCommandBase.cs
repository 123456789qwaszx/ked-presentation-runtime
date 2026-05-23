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

public abstract class ShotIntentCommandBase<TSpec> : CommandBase, IStepScopedCommand
    where TSpec : ShotIntentCommandSpecBase
{
    protected readonly PresentationResponseRig Rig;
    protected readonly TSpec Spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => Spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected ShotIntentCommandBase(PresentationResponseRig rig, TSpec spec)
    {
        Rig = rig;
        Spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (Rig == null)
            yield break;

        if (Spec.killTween)
            KillRigTween(true);

        _fromState = Rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (Spec.duration <= 0f ||
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
                    if (!_canCommitFinalState || Rig == null)
                        return;

                    PresentationIntentState state =
                        PresentationShotIntentMath.Interpolate(_fromState, _toState, t);

                    Rig.ApplyToAllBindings(state);
                },
                1f,
                Spec.duration)
            .SetEase(Spec.ease)
            .SetUpdate(true)
            .SetTarget(Rig)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || Rig == null)
                    return;

                Commit(_toState);
                ClearRuntimeState();
            });

        if (Spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (Rig == null)
            return;

        KillRigTween(false);

        _fromState = Rig.CurrentState;
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
        if (!_canCommitFinalState || Rig == null)
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
        if (Rig == null)
            return;

        Rig.ApplyToAllBindings(state);
    }

    private void KillRigTween(bool complete)
    {
        if (Rig == null)
            return;

        DOTween.Kill(Rig, complete);
        _tween = null;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;
        _tween = null;
    }
}