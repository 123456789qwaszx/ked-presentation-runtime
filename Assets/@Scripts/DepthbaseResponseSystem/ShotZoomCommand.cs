using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : CommandSpecBase
{
    [Header("Focus")]
    [Tooltip("단일 focus target key. 비워두면 focus 변경 없음.")]
    public string focusKey = "";

    [Tooltip("여러 슬롯을 동시에 focus. 평균점으로 계산.")]
    public string[] groupFocusKeys = Array.Empty<string>();

    [Tooltip("focus target이 있으면 desiredFramingPoint 쪽으로 pan을 자동 구성한다.")]
    public bool reframeToFocus = true;

    [Tooltip("focus를 가져올 목표 구도점. Rig 공간 기준. (0,0)=중앙")]
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
    [Tooltip("체크하면 zoom을 절대값으로 설정한다. 해제하면 현재 zoom에 delta로 더한다.")]
    public bool absoluteZoom = true;

    [Tooltip("체크하면 manual pan을 절대값으로 설정한다. 해제하면 현재 pan에 delta로 더한다.")]
    public bool absolutePan = true;
}

public sealed class ShotZoomCommand : CommandBase
{
    private readonly ShotZoomCommandSpec _spec;

    private PresentationResponseRig _rig;
    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotZoomCommand(ShotZoomCommandSpec spec)
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
        _toState = BuildTargetState(_fromState, scope.Presentation);

        if (_spec.duration <= 0f)
        {
            Commit(_toState, scope.Presentation);
            yield break;
        }

        PlayTween(_fromState, _toState, scope.Presentation);

        if (_spec.wait && _tween != null)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Resolve(scope);
        if (_rig == null)
            return;

        KillTweenIfNeeded();
        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope.Presentation);
        Commit(_toState, scope.Presentation);
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

    private PresentationIntentState BuildTargetState(
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
                if (_rig.TryGetFocusPoint(key, presentation, out Vector2 point))
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
            if (_rig.TryGetFocusPoint(_spec.focusKey, presentation, out Vector2 point))
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

        Vector2 manualPanPixels = _rig.GetManualPanPixels(new Vector2(_spec.panX, _spec.panY));

        Vector2 targetPan = _spec.absolutePan
            ? manualPanPixels
            : from.pan + manualPanPixels;

        if (hasFocus && _spec.reframeToFocus)
        {
            Vector2 framingPan = _rig.ComposePanForFocus(focusPoint, _spec.desiredFramingPoint);
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
                        PresentationResponseSolver.Lerp(from, to, value);

                    _rig.ApplyImmediate(state, presentation);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rig)
            .OnComplete(() => Commit(to, presentation));
    }

    private void Commit(in PresentationIntentState state, PresentationViewRefs presentation)
    {
        if (_rig == null)
            return;

        _rig.ApplyImmediate(state, presentation);
        _tween = null;
    }

    private void KillTweenIfNeeded()
    {
        if (_tween == null)
            return;

        _tween.Kill(false);
        _tween = null;
    }
}