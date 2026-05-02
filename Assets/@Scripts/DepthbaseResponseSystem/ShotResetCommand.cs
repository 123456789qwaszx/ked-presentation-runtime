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

    private PresentationIntentState _fromState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotResetCommand(ShotResetCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        PresentationResponseRig rig = scope.ResponseRig;
        if (rig == null)
            yield break;

        KillTweenIfNeeded();

        _fromState = rig.CurrentState;
        PresentationIntentState toState = PresentationIntentState.Default;

        if (_spec.duration <= 0f)
        {
            Commit(rig, toState, scope.Presentation);
            yield break;
        }

        float progress = 0f;
        PresentationViewRefs presentation = scope.Presentation;

        _tween = DOTween.To(
                () => progress,
                value =>
                {
                    progress = value;

                    PresentationIntentState state = InterpolateState(_fromState, toState, value);
                    rig.ApplyImmediate(state, presentation);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(() => Commit(rig, toState, presentation));

        if (_spec.wait && _tween != null)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        PresentationResponseRig rig = scope.ResponseRig;
        if (rig == null)
            return;

        KillTweenIfNeeded();
        Commit(rig, PresentationIntentState.Default, scope.Presentation);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false이면 tween을 background로 유지
    }

    private static void Commit(
        PresentationResponseRig rig,
        in PresentationIntentState state,
        PresentationViewRefs presentation)
    {
        if (rig == null)
            return;

        rig.ApplyImmediate(state, presentation);
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