using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Pan To", Order = -847)]
public sealed class ShotPanToCommandSpec : CommandSpecBase
{
    [Header("Pan")]
    [Range(-10f, 10f)] public float panX = 0f;
    [Range(-10f, 10f)] public float panY = 0f;

    [Tooltip("체크하면 manual pan을 절대값으로 설정한다. 해제하면 현재 pan에 delta로 더한다.")]
    public bool absolutePan = true;

    [Header("Tween")]
    public float duration = 0.35f;
    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class ShotPanToCommand : CommandBase
{
    private readonly ShotPanToCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotPanToCommand(ShotPanToCommandSpec spec)
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
        _toState = BuildTargetState(rig, _fromState);

        if (_spec.duration <= 0f)
        {
            Commit(rig, _toState, scope.Presentation);
            yield break;
        }

        PlayTween(rig, _fromState, _toState, scope.Presentation);

        if (_spec.wait && _tween != null)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        PresentationResponseRig rig = scope.ResponseRig;
        if (rig == null)
            return;

        KillTweenIfNeeded();
        _fromState = rig.CurrentState;
        _toState = BuildTargetState(rig, _fromState);
        Commit(rig, _toState, scope.Presentation);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false이면 tween을 background로 유지
    }

    private PresentationIntentState BuildTargetState(
        PresentationResponseRig rig,
        in PresentationIntentState from)
    {
        Vector2 manualPanPixels = rig.GetManualPanPixels(new Vector2(_spec.panX, _spec.panY));

        Vector2 targetPan = _spec.absolutePan
            ? manualPanPixels
            : from.pan + manualPanPixels;

        return new PresentationIntentState
        {
            zoom = from.zoom,
            pan = targetPan,
            focusPoint = from.focusPoint,
        };
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
                    PresentationIntentState state = InterpolateState(from, to, value);
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