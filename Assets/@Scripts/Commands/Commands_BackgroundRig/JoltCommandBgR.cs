using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Jolt", Order = -740)]
public sealed class JoltCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track_Y;

    [Header("Nudge")]
    public float strength = 22f;
    public CharRigDirection direction = CharRigDirection.Right;
    public float duration = 0.88f;

    [Min(1)]
    public int taps = 3;

    public float damping = 6f;

    [Header("Style")]
    public float anticipation = 3f;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class JoltCommandBgR : CommandBase, IStepScopedCommand
{
    private readonly JoltCommandSpecBgR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public JoltCommandBgR(JoltCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true);

        _canCommitFinalState = true;

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            _rect.anchoredPosition = _destPos;
            ClearRuntimeRefs();
            yield break;
        }

        Vector2 basePos = _destPos;

        float amplitude = Mathf.Abs(_spec.strength);
        int taps = Mathf.Max(1, _spec.taps);
        float damping = Mathf.Max(0.01f, _spec.damping);
        float anticipation = Mathf.Abs(_spec.anticipation);

        Vector2 dir = GetSignedDirection(_spec.direction);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float u = Mathf.Clamp01(t);

                    float antiTerm = 0f;
                    if (!Mathf.Approximately(anticipation, 0f))
                    {
                        float s = Mathf.Clamp01(u / 0.15f);
                        float bump = Mathf.Sin(Mathf.PI * s);
                        antiTerm = -anticipation * bump * (1f - s);
                    }

                    float decay = Mathf.Exp(-damping * u);
                    float settleEnvelope = Mathf.Sin(Mathf.PI * u);
                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);

                    float scalar = antiTerm + amplitude * decay * osc * settleEnvelope;

                    _rect.anchoredPosition = basePos + dir * scalar;
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = basePos;
                ClearRuntimeRefs();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.anchoredPosition = _destPos;
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _destPos;

        ClearRuntimeRefs();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rigRefs =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);

        _rect = rigRefs.GetRect(_spec.target);

        if (_rect != null)
            _destPos = _rect.anchoredPosition;
    }

    private void ClearRuntimeRefs()
    {
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private static Vector2 GetSignedDirection(CharRigDirection direction)
    {
        return direction switch
        {
            CharRigDirection.Left => Vector2.left,
            CharRigDirection.Right => Vector2.right,
            CharRigDirection.Up => Vector2.up,
            CharRigDirection.Down => Vector2.down,
            _ => Vector2.right,
        };
    }
}