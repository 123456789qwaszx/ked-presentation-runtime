using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : CommandSpecBase
{
    [Header("Focus")]
    [Tooltip("focus 기준으로 삼을 character roleKey. 비우면 focus 재구성 없이 zoom/pan만 적용.")]
    public string focusRoleKey = "";
    public CharacterRigTarget focusTarget = CharacterRigTarget.CharacterPortrait_Root;

    [Tooltip("focus target rect의 로컬 오프셋.")]
    public Vector2 focusLocalOffset = Vector2.zero;

    [Tooltip("focus가 있으면 desiredFramingPoint 쪽으로 pan을 자동 구성한다.")]
    public bool reframeToFocus = true;

    [Tooltip("focus를 가져올 목표 구도점. Rig 공간 기준. (0,0)=중앙")]
    public Vector2 desiredFramingPoint = Vector2.zero;

    [Header("Apply")]
    [Tooltip("체크하면 zoom을 절대 목표값으로 적용합니다. 끄면 현재 zoom을 유지합니다.")]
    public bool applyZoom = true;

    [Tooltip("체크하면 pan을 절대 목표값으로 적용합니다. 끄면 현재 pan을 유지합니다.")]
    public bool applyPan = true;

    [Header("Intent")]
    [Range(-10f, 10f)] public float zoom = 0f;
    [Range(-10f, 10f)] public float panX = 0f;
    [Range(-10f, 10f)] public float panY = 0f;

    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ShotZoomCommand : CommandBase, IStepScopedCommand
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotZoomCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotZoomCommand(PresentationResponseRig rig, ShotZoomCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null)
            yield break;

        if (_spec.killTween)
            _tween.Kill(true); // Finish previous motion so this command starts from a committed state.

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            _rig.ApplyToAllBindings(_toState);
            
            _canCommitFinalState = false;
            _tween = null;
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t);
                    PresentationIntentState state = InterpolateState(_fromState, _toState, u);
                    _rig.ApplyToAllBindings(state);
                },
                1f,
                _spec.duration
            )
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rig)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rig == null)
                    return;
                
                _rig.ApplyToAllBindings(_toState);
                
                _canCommitFinalState = false;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null)
            return;

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);
        
        _rig.ApplyToAllBindings(_toState);
        
        _canCommitFinalState = false;
        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rig == null)
            return;

        _tween?.Kill(false);
        
        _rig.ApplyToAllBindings(_toState);
        
        _canCommitFinalState = false;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
    }

    private PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        Vector2 focusPoint = from.focusPoint;
        bool hasFocus = TryResolveFocusPoint(scope, out Vector2 resolvedFocusPoint);

        if (hasFocus)
            focusPoint = resolvedFocusPoint;

        float targetZoom = _spec.applyZoom
            ? Mathf.Clamp(_spec.zoom, -10f, 10f)
            : from.zoom;

        Vector2 panOffset = _spec.applyPan
            ? new Vector2(_spec.panX, _spec.panY)
            : Vector2.zero;

        Vector2 targetPan = from.pan;

        if (hasFocus && _spec.reframeToFocus)
        {
            targetPan = _spec.desiredFramingPoint - focusPoint;

            if (_spec.applyPan)
                targetPan += panOffset;
        }
        else if (_spec.applyPan)
        {
            targetPan = panOffset;
        }

        return new PresentationIntentState
        {
            zoom = targetZoom,
            pan = targetPan,
            focusPoint = focusPoint,
        };
    }
    
    private bool TryResolveFocusPoint(CommandRunScope scope, out Vector2 focusPoint)
    {
        focusPoint = Vector2.zero;
        
        scope.Refs.TryGetCharRigRefs(_spec.focusRoleKey, out CharacterRigRefs rigRefs);
        RectTransform rect = rigRefs.GetRect(_spec.focusTarget);
        
        RectTransform stageRoot = scope.Presentation.GetRect(PresentationTarget.Stage_Root);

        Vector3 world = rect.TransformPoint(new Vector3(_spec.focusLocalOffset.x, _spec.focusLocalOffset.y, 0f));
        Vector3 local = stageRoot.InverseTransformPoint(world);
        
        focusPoint = new Vector2(local.x, local.y);
        return true;
    }

    private static PresentationIntentState InterpolateState(in PresentationIntentState from, in PresentationIntentState to, float t)
    {
        return new PresentationIntentState
        {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            pan = Vector2.Lerp(from.pan, to.pan, t),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t),
        };
    }
}