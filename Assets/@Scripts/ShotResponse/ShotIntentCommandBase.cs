using System.Collections;
using DG.Tweening;

public abstract class ShotIntentCommandBase<TSpec> : CommandBase, IStepScopedCommand
    where TSpec : CommandSpecBase
{
    protected readonly PresentationResponseRig Rig;
    protected readonly TSpec Spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _canCommitFinalState;

    protected abstract float Duration { get; }
    protected abstract Ease Ease { get; }
    protected abstract bool KillTween { get; }

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

        if (KillTween)
            KillRigTween(true);

        _fromState = Rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (Duration <= 0f ||
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
                Duration)
            .SetEase(Ease)
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