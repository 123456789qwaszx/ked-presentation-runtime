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
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class ShotZoomCommand : CommandBase, IStepScopedCommand
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotZoomCommandSpec _spec;

    private PresentationViewRefs _presentation;
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

        if (_rig == null || _presentation == null)
            yield break;

        if (_spec.killTween)
            _tween.Kill(true); // Finish previous motion so this command starts from a committed state.

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f || ApproximatelyEqual(_fromState, _toState))
        {
            Commit(_rig, _toState, _presentation);
            ClearRuntimeState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rig == null || _presentation == null)
                        return;

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
                if (!_canCommitFinalState || _rig == null || _presentation == null)
                    return;

                Commit(_rig, _toState, _presentation);
                ClearRuntimeState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null || _presentation == null)
            return;

        KillRigTween(false);

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        Commit(_rig, _toState, _presentation);
        ClearRuntimeState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rig == null || _presentation == null)
            return;

        _tween?.Kill(false);
        KillRigTween(false);
        Commit(_rig, _toState, _presentation);
        ClearRuntimeState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (_rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotZoomCommand] PresentationResponseRig is null.");
            return;
        }

        if (scope == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotZoomCommand] CommandRunScope is null.");
            return;
        }

        if (scope.Presentation == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotZoomCommand] PresentationViewRefs is null.");
            return;
        }

        _presentation = scope.Presentation;
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

        string roleKey = SafeTrim(_spec.focusRoleKey);
        if (string.IsNullOrEmpty(roleKey))
            return false;

        if (scope == null)
        {
            if (_spec.strict)
                Debug.LogWarning($"[ShotZoomCommand] Scope is null. focusRoleKey='{roleKey}'.");
            return false;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rigRefs) || rigRefs == null)
        {
            if (_spec.strict)
                Debug.LogWarning($"[ShotZoomCommand] Rig refs not found. roleKey='{roleKey}'.");
            return false;
        }

        RectTransform rect = rigRefs.GetRect(_spec.focusTarget);
        if (rect == null)
        {
            if (_spec.strict)
            {
                Debug.LogWarning(
                    $"[ShotZoomCommand] Focus target rect not found. roleKey='{roleKey}', target='{_spec.focusTarget}'.");
            }

            return false;
        }

        RectTransform stageRoot = _presentation != null
            ? _presentation.GetRect(PresentationTarget.Stage_Root)
            : null;

        Vector3 world =
            rect.TransformPoint(new Vector3(_spec.focusLocalOffset.x, _spec.focusLocalOffset.y, 0f));

        focusPoint = WorldToSpacePoint(stageRoot, world);
        return true;
    }

    private void KillRigTween(bool complete)
    {
        if (_rig == null)
            return;

        DOTween.Kill(_rig, complete);
        _tween = null;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;
        _presentation = null;
        _tween = null;
    }

    private static void Commit(
        PresentationResponseRig rig,
        in PresentationIntentState state,
        PresentationViewRefs presentation)
    {
        if (rig == null || presentation == null)
            return;

        rig.ApplyToAllBindings(state);
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

    private static bool ApproximatelyEqual(
        in PresentationIntentState a,
        in PresentationIntentState b)
    {
        return Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
               Vector2.SqrMagnitude(a.pan - b.pan) <= 0.0001f &&
               Vector2.SqrMagnitude(a.focusPoint - b.focusPoint) <= 0.0001f;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
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