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
}

public sealed class JoltCommandBgR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly JoltCommandSpecBgR _spec;

    private RectTransform _rect;
    private Vector2 _basePos;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public JoltCommandBgR(JoltCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        Vector2 basePos = _basePos;

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
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _basePos = _rect.anchoredPosition;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _rect
            .DOAnchorPos(_basePos, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = CalculateReferenceAmplitude();
        float remainingDistance = Vector2.Distance(_rect.anchoredPosition, _basePos);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private float CalculateReferenceAmplitude()
    {
        return Mathf.Max(
            Mathf.Abs(_spec.strength),
            Mathf.Abs(_spec.anticipation),
            0.001f);
    }

    #endregion

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