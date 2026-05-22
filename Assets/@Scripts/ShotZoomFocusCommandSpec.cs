using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom Focus", Order = -849)]
public sealed class ShotZoomFocusCommandSpec : CommandSpecBase
{
    [Header("Character Focus")]
    public string focusRoleKey = "";
    public CharacterFocusAnchor focusAnchor = CharacterFocusAnchor.Face;

    [Tooltip("anchor가 없을 때 fallback으로 사용할 CharacterRig target")]
    public CharacterRigTarget fallbackTarget = CharacterRigTarget.Character_Root;

    [Tooltip("선택한 focus anchor의 로컬 오프셋")]
    public Vector2 focusLocalOffset = Vector2.zero;

    [Header("Screen Focus")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage local space 기준.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Intent")]
    [Range(-10f, 10f)]
    public float zoom = 0f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
    public bool strict = true;
}

public sealed class ShotZoomFocusCommand : CommandBase, IStepScopedCommand
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotZoomFocusCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotZoomFocusCommand(
        PresentationResponseRig rig,
        ShotZoomFocusCommandSpec spec)
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
            _tween?.Kill(true); // Finish previous motion so this command starts from a committed state.

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            _rig.ApplyToAllBindings(_toState);
            ClearRuntimeState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rig == null)
                        return;

                    float u = Mathf.Clamp01(t);
                    PresentationIntentState state = InterpolateState(_fromState, _toState, u);
                    _rig.ApplyToAllBindings(state);
                },
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rig)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rig == null)
                    return;

                _rig.ApplyToAllBindings(_toState);
                ClearRuntimeState();
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
        ClearRuntimeState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rig == null)
            return;

        _tween?.Kill(false);
        _rig.ApplyToAllBindings(_toState);
        ClearRuntimeState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
    }

    private PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        if (!CharacterFocusPointResolver.TryResolve(
                scope,
                _spec.focusRoleKey,
                _spec.focusAnchor,
                _spec.fallbackTarget,
                _spec.focusLocalOffset,
                out CharacterFocusPointResult focus))
        {
            if (_spec.strict)
                Debug.LogWarning($"[ShotZoomFocusCommand] Focus point not found. roleKey='{_spec.focusRoleKey}', anchor='{_spec.focusAnchor}'.");

            return from;
        }

        float targetZoom = Mathf.Clamp(_spec.zoom, -10f, 10f);

        float cameraScale = _rig != null
            ? _rig.EvaluateCameraScale(targetZoom)
            : 1f + targetZoom * 0.05f;

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.StageRoot, _spec.screenPoint) +
            _spec.screenOffset;

        Vector2 targetPan = desiredPoint - focus.FocusPointInStageSpace * cameraScale;

        return new PresentationIntentState
        {
            zoom = targetZoom,
            pan = targetPan,
            focusPoint = focus.FocusPointInStageSpace,
        };
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;
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
}