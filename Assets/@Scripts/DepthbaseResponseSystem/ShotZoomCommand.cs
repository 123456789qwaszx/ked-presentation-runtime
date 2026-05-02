using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : CommandSpecBase
{
    [Header("Focus")]
    public string focusKey = "";
    public string[] groupFocusKeys = Array.Empty<string>();
    public bool reframeToFocus = true;
    public Vector2 desiredFramingPoint = Vector2.zero;

    [Header("Intent")]
    [Range(-10f, 10f)] public float zoom = 0f;
    [Range(-10f, 10f)] public float panX = 0f;
    [Range(-10f, 10f)] public float panY = 0f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    public bool absoluteZoom = true;
    public bool absolutePan = true;
}

public sealed class ShotZoomCommand : CommandBase
{
    private readonly ShotZoomCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotZoomCommand(ShotZoomCommandSpec spec)
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
        _toState = BuildTargetState(rig, _fromState, scope.Presentation);

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
        _toState = BuildTargetState(rig, _fromState, scope.Presentation);
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
        in PresentationIntentState from,
        PresentationViewRefs presentation)
    {
        Vector2 focusPoint = from.focusPoint;
        bool hasFocus = false;

        Vector2 sum = Vector2.zero;
        int count = 0;

        if (_spec.groupFocusKeys != null)
        {
            for (int i = 0; i < _spec.groupFocusKeys.Length; i++)
            {
                string key = _spec.groupFocusKeys[i];
                if (rig.TryGetFocusPoint(key, presentation, out Vector2 point))
                {
                    sum += point;
                    count++;
                }
                else if (!string.IsNullOrWhiteSpace(key))
                {
                    Debug.LogWarning($"[ShotZoomCommand] Group focus key not found: {key}");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_spec.focusKey))
        {
            if (rig.TryGetFocusPoint(_spec.focusKey, presentation, out Vector2 point))
            {
                sum += point;
                count++;
            }
            else
            {
                Debug.LogWarning($"[ShotZoomCommand] Focus key not found: {_spec.focusKey}");
            }
        }

        if (count > 0)
        {
            focusPoint = sum / count;
            hasFocus = true;
        }

        float targetZoom = _spec.absoluteZoom
            ? _spec.zoom
            : from.zoom + _spec.zoom;

        Vector2 manualPanPixels = rig.GetManualPanPixels(new Vector2(_spec.panX, _spec.panY));

        Vector2 targetPan = _spec.absolutePan
            ? manualPanPixels
            : from.pan + manualPanPixels;

        if (hasFocus && _spec.reframeToFocus)
        {
            Vector2 framingPan = rig.ComposePanForFocus(focusPoint, _spec.desiredFramingPoint);
            targetPan += framingPan;
        }

        return new PresentationIntentState
        {
            zoom = Mathf.Clamp(targetZoom, -10f, 10f),
            pan = targetPan,
            focusPoint = focusPoint,
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