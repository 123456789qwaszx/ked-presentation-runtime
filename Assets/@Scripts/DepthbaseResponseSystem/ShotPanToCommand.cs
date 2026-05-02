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
    private readonly PresentationResponseRig _rig;
    private readonly ShotPanToCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotPanToCommand(
        PresentationResponseRig rig,
        ShotPanToCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_rig == null)
        {
            Debug.LogError("[ShotPanToCommand] PresentationResponseRig is null.");
            yield break;
        }

        if (scope == null || scope.Presentation == null)
        {
            Debug.LogError("[ShotPanToCommand] PresentationViewRefs is null.");
            yield break;
        }

        KillTweenIfNeeded();

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_rig, _fromState);

        if (_spec.duration <= 0f)
        {
            Commit(_rig, _toState, scope.Presentation);
            yield break;
        }

        PlayTween(_rig, _fromState, _toState, scope.Presentation);

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

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_rig, _fromState);

        Commit(_rig, _toState, scope.Presentation);
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
        Vector2 manualPanPixels =
            rig.GetManualPanPixels(new Vector2(_spec.panX, _spec.panY));

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