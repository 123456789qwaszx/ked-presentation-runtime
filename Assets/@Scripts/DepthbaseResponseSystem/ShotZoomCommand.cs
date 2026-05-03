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

    [Tooltip("roleKey로 찾은 CharacterRigRefs 내부에서 focus 기준으로 쓸 target.")]
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
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class ShotZoomCommand : CommandBase
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotZoomCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotZoomCommand(PresentationResponseRig rig, ShotZoomCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_rig == null)
            yield break;

        if (scope == null || scope.Presentation == null)
            yield break;

        if (_spec.killTween)
            _tween?.Kill(true);

        _fromState = _rig.CurrentState;

        Debug.Log(
            $"[ShotZoomCommand] Captured fromState | " +
            $"from.zoom={_fromState.zoom}, " +
            $"from.pan={_fromState.pan}, " +
            $"from.focusPoint={_fromState.focusPoint}");
        
        _toState = BuildTargetState(_rig, _fromState, scope);
        Debug.Log(
            $"[ShotZoomCommand] Built toState | " +
            $"to.zoom={_toState.zoom}, " +
            $"to.pan={_toState.pan}, " +
            $"to.focusPoint={_toState.focusPoint}");

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

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_rig, _fromState, scope);

        Commit(_rig, _toState, scope.Presentation);

        _tween = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false이면 tween을 background로 유지.
    }

    private PresentationIntentState BuildTargetState(
        PresentationResponseRig rig,
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        Vector2 focusPoint = from.focusPoint;
        bool hasFocus = false;

        if (!string.IsNullOrWhiteSpace(_spec.focusRoleKey) &&
            TryGetRigFocusPoint(
                scope,
                _spec.focusRoleKey,
                _spec.focusTarget,
                _spec.focusLocalOffset,
                out Vector2 rigFocusPoint))
        {
            focusPoint = rigFocusPoint;
            hasFocus = true;
        }

        float targetZoom = _spec.applyZoom
            ? _spec.zoom
            : from.zoom;

        Vector2 manualPanPixels = Vector2.zero;
        if (_spec.applyPan)
        {
            manualPanPixels =
                rig.GetManualPanPixels(new Vector2(_spec.panX, _spec.panY));
        }

        Vector2 targetPan = from.pan;

        if (hasFocus && _spec.reframeToFocus)
        {
            targetPan = rig.ComposePanForFocus(
                focusPoint,
                _spec.desiredFramingPoint);

            if (_spec.applyPan)
                targetPan += manualPanPixels;
        }
        else if (_spec.applyPan)
        {
            targetPan = manualPanPixels;
        }

        return new PresentationIntentState
        {
            zoom = Mathf.Clamp(targetZoom, -10f, 10f),
            pan = targetPan,
            focusPoint = focusPoint,
        };
    }

    private static bool TryGetRigFocusPoint(
        CommandRunScope scope,
        string roleKey,
        CharacterRigTarget target,
        Vector2 localOffset,
        out Vector2 focusPoint)
    {
        focusPoint = Vector2.zero;

        if (scope == null || string.IsNullOrWhiteSpace(roleKey))
            return false;

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rigRefs))
            return false;

        RectTransform rect = rigRefs.GetRect(target);
        if (rect == null)
            return false;

        RectTransform stageRoot =
            scope.Presentation != null
                ? scope.Presentation.GetRect(PresentationTarget.Stage_Root)
                : null;

        Vector3 world =
            rect.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));

        focusPoint = WorldToSpacePoint(stageRoot, world);
        return true;
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

    public static Vector2 WorldToSpacePoint(
        RectTransform stageRoot,
        Vector3 worldPoint)
    {
        if (stageRoot == null)
            return new Vector2(worldPoint.x, worldPoint.y);

        Vector3 local = stageRoot.InverseTransformPoint(worldPoint);
        return new Vector2(local.x, local.y);
    }
}