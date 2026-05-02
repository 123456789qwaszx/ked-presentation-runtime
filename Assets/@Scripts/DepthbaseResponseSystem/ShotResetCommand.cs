using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Reset", Order = -849)]
public sealed class ShotResetCommandSpec : CommandSpecBase
{
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;
    public bool wait = false;
}

public sealed class ShotResetCommand : CommandBase
{
    private readonly ShotResetCommandSpec _spec;

    private PresentationResponseRig _rig;
    private PresentationIntentState _fromState;
    private Tween _tween;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotResetCommand(ShotResetCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Resolve(scope);
        if (_rig == null)
            yield break;

        KillTweenIfNeeded();

        _fromState = _rig.CurrentState;
        PresentationIntentState toState = PresentationIntentState.Default;

        if (_spec.duration <= 0f)
        {
            Commit(toState, scope.Presentation);
            yield break;
        }

        float progress = 0f;
        PresentationViewRefs presentation = scope.Presentation;

        _tween = DOTween.To(
                () => progress,
                value =>
                {
                    progress = value;

                    PresentationIntentState state =
                        InterpolateState(_fromState, toState, value);

                    _rig.ApplyImmediate(state, presentation);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rig)
            .OnComplete(() => Commit(toState, presentation));

        if (_spec.wait && _tween != null)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Resolve(scope);
        if (_rig == null)
            return;

        KillTweenIfNeeded();
        Commit(PresentationIntentState.Default, scope.Presentation);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false이면 tween을 background로 유지한다.
    }

    private void Resolve(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return;

        _resolveAttempted = true;
        _rig = PresentationResponseRigResolver.Resolve(scope.Presentation);
    }

    private void Commit(in PresentationIntentState state, PresentationViewRefs presentation)
    {
        if (_rig == null)
            return;

        _rig.ApplyImmediate(state, presentation);
        _tween = null;
    }

    private static PresentationIntentState InterpolateState(
        in PresentationIntentState from,
        in PresentationIntentState to,
        float t)
    {
        return new PresentationIntentState
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            pan = Vector2.Lerp(from.pan, to.pan, t),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t),
        };
    }

    private void KillTweenIfNeeded()
    {
        if (_tween == null)
            return;

        _tween.Kill(false);
        _tween = null;
    }
}