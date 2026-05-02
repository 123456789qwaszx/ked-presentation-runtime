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
    private readonly PresentationResponseRig _rig;
    private readonly ShotResetCommandSpec _spec;

    private PresentationIntentState _fromState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotResetCommand(
        PresentationResponseRig rig,
        ShotResetCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_rig == null)
        {
            Debug.LogError("[ShotResetCommand] PresentationResponseRig is null.");
            yield break;
        }

        if (scope == null || scope.Presentation == null)
        {
            Debug.LogError("[ShotResetCommand] PresentationViewRefs is null.");
            yield break;
        }

        KillTweenIfNeeded();

        _fromState = _rig.CurrentState;
        PresentationIntentState toState = PresentationIntentState.Default;

        if (_spec.duration <= 0f)
        {
            Commit(_rig, toState, scope.Presentation);
            yield break;
        }

        PlayTween(_rig, _fromState, toState, scope.Presentation);

        if (_spec.wait && _tween != null)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (_rig == null)
            return;

        if (scope == null || scope.Presentation == null)
            return;

        KillTweenIfNeeded();

        Commit(
            _rig,
            PresentationIntentState.Default,
            scope.Presentation);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false이면 tween을 background로 유지
    }

    private void PlayTween(
        PresentationResponseRig rig,
        PresentationIntentState from,
        PresentationIntentState to,
        PresentationViewRefs presentation)
    {
        float progress = 0f;

        _tween = DOTween.To(
                () => progress,
                value =>
                {
                    progress = value;

                    PresentationIntentState state =
                        InterpolateState(from, to, value);

                    rig.ApplyImmediate(state, presentation);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(rig)
            .OnComplete(() => Commit(rig, to, presentation));
    }

    private static void Commit(
        PresentationResponseRig rig,
        in PresentationIntentState state,
        PresentationViewRefs presentation)
    {
        if (rig == null || presentation == null)
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