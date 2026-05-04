using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Track", Order = -848)]
public sealed class ShotTrackCommandSpec : CommandSpecBase
{
    [Header("Focus")]
    [Tooltip("추적할 character roleKey")]
    public string focusRoleKey = "";

    [Tooltip("roleKey로 찾은 CharacterRigRefs 내부에서 focus 기준으로 쓸 target")]
    public CharacterRigTarget focusTarget = CharacterRigTarget.CharacterPortrait_Root;

    [Tooltip("focus target rect의 로컬 오프셋")]
    public Vector2 focusLocalOffset = Vector2.zero;

    [Tooltip("focus를 가져올 목표 구도점. Rig 공간 기준. (0,0)=중앙")]
    public Vector2 desiredFramingPoint = Vector2.zero;

    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.35f;

    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class ShotTrackCommand : CommandBase, IStepScopedCommand
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotTrackCommandSpec _spec;

    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShotTrackCommand(
        PresentationResponseRig rig,
        ShotTrackCommandSpec spec)
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

        KillRigTween(false);

        _fromState = _rig.CurrentState;
        _toState = BuildTargetState(_fromState, scope);

        _rig.ApplyToAllBindings(_toState);
        ClearRuntimeState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rig == null)
            return;

        _tween?.Kill(false);
        KillRigTween(false);
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
        if (!TryResolveFocusPoint(scope, out Vector2 focusPoint))
            return from;

        Vector2 targetPan = _spec.desiredFramingPoint - focusPoint;

        return new PresentationIntentState
        {
            zoom = from.zoom,
            pan = targetPan,
            focusPoint = focusPoint,
        };
    }

    private bool TryResolveFocusPoint(CommandRunScope scope, out Vector2 focusPoint)
    {
        focusPoint = Vector2.zero;

        string roleKey = SafeTrim(_spec.focusRoleKey);
        if (string.IsNullOrEmpty(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotTrackCommand] focusRoleKey is null or empty.");
            return false;
        }

        if (scope == null)
        {
            if (_spec.strict)
                Debug.LogWarning($"[ShotTrackCommand] Scope is null. focusRoleKey='{roleKey}'.");
            return false;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rigRefs) || rigRefs == null)
        {
            if (_spec.strict)
                Debug.LogWarning($"[ShotTrackCommand] Rig refs not found. roleKey='{roleKey}'.");
            return false;
        }

        RectTransform rect = rigRefs.GetRect(_spec.focusTarget);
        if (rect == null)
        {
            if (_spec.strict)
            {
                Debug.LogWarning(
                    $"[ShotTrackCommand] Focus target rect not found. roleKey='{roleKey}', target='{_spec.focusTarget}'.");
            }

            return false;
        }

        RectTransform stageRoot = scope.Presentation.GetRect(PresentationTarget.Stage_Root);

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