using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Scale (From → To)",
    Order = -170
)]
public class ScaleToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_ActingScale;

    [Header("Scale (XY)")]
    public Vector2 toScale = Vector2.one;

    [Header("From")]
    public bool overrideFromScale = false;
    public Vector2 fromScale = Vector2.one;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class ScaleToCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly ScaleToCommandSpecCharR _spec;

    private RectTransform _rect;

    private Vector2 _startScale;
    private Vector2 _targetScale;
    private Vector3 _endScale;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public ScaleToCommandCharR(ScaleToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.overrideFromScale)
            ApplyScaleXY(_rect, _spec.fromScale);

        CaptureTweenEndpoints();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOScale(_endScale, _spec.duration)
            .SetEase(_spec.ease)
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

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _targetScale = _spec.toScale;

        HasClaimedTarget = true;
    }

    private void CaptureTweenEndpoints()
    {
        Vector3 currentScale = _rect.localScale;

        _startScale = new Vector2(currentScale.x, currentScale.y);
        _targetScale = _spec.toScale;

        _endScale = currentScale;
        _endScale.x = _targetScale.x;
        _endScale.y = _targetScale.y;
    }

    private void CommitFinalState()
    {
        ApplyScaleXY(_rect, _targetScale);

        HasClaimedTarget = false;
        _tween = null;
    }

    private static void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 scale = rect.localScale;
        scale.x = targetXY.x;
        scale.y = targetXY.y;
        rect.localScale = scale;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _rect
            .DOScale(_endScale, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        Vector3 currentScale = _rect.localScale;
        Vector2 currentXY = new(currentScale.x, currentScale.y);

        float originalDistance = Vector2.Distance(_startScale, _targetScale);
        float remainingDistance = Vector2.Distance(currentXY, _targetScale);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion
}