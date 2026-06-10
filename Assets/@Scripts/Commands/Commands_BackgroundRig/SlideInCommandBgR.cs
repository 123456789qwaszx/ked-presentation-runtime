using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Slide In", Order = -771)]
public sealed class SlideInCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track;

    [Header("Slide")]
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("(overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;
}

public sealed class SlideInCommandBgR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly SlideInCommandSpecBgR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _destPos;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public SlideInCommandBgR(SlideInCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _startPos;
        Vector2 dest = _destPos;

        Vector2 fromDir = GetDir(_spec.direction);

        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : -fromDir;

        _rect.anchoredPosition = start;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = JuicyBumpEnd(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
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

        _destPos = _rect.anchoredPosition;
        _startPos = _destPos + GetDir(_spec.direction) * _spec.distance;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _destPos;

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;
        
        _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector2.Distance(_startPos, _destPos);
        float remainingDistance = Vector2.Distance(_rect.anchoredPosition, _destPos);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion

    private static Vector2 GetDir(CharRigDirection from)
    {
        return from switch
        {
            CharRigDirection.Right => new Vector2(+1f, 0f),
            CharRigDirection.Up => new Vector2(0f, +1f),
            CharRigDirection.Down => new Vector2(0f, -1f),
            _ => new Vector2(-1f, 0f),
        };
    }

    private static float JuicyBumpEnd(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}