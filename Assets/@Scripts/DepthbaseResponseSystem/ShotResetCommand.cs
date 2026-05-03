using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Reset", Order = -849)]
public sealed class ShotResetCommandSpec : CommandSpecBase
{
    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.35f;

    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;

    [Header("Options")]
    [Tooltip("체크하면 기존 shot tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class ShotResetCommand : CommandBase, IStepScopedCommand
{
    private readonly PresentationResponseRig _rig;
    private readonly ShotResetCommandSpec _spec;

    private PresentationViewRefs _presentation;
    private PresentationIntentState _fromState;
    private PresentationIntentState _toState;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

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
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rig == null || _presentation == null)
            yield break;

        if (_spec.killTween)
            KillRigTween(true); // Finish previous motion so this command starts from a committed state.

        _fromState = _rig.CurrentState;
        _toState = PresentationIntentState.Default;

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
                    _rig.ApplyImmediate(state, _presentation);
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
        _toState = PresentationIntentState.Default;

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
                Debug.LogWarning("[ShotResetCommand] PresentationResponseRig is null.");
            return;
        }

        if (scope == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotResetCommand] CommandRunScope is null.");
            return;
        }

        if (scope.Presentation == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[ShotResetCommand] PresentationViewRefs is null.");
            return;
        }

        _presentation = scope.Presentation;
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

    private static bool ApproximatelyEqual(
        in PresentationIntentState a,
        in PresentationIntentState b)
    {
        return Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
               Vector2.SqrMagnitude(a.pan - b.pan) <= 0.0001f &&
               Vector2.SqrMagnitude(a.focusPoint - b.focusPoint) <= 0.0001f;
    }
}