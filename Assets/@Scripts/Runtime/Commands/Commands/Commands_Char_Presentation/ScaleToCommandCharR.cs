using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Scale (From -> To)",
    Order = -170
)]
public class ScaleToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Scale;

    [Header("Scale (XY)")]
    public Vector2 toScale = Vector2.one;

    [Header("Mode")]
    [Tooltip("false면 toScale을 절대 scale로 사용합니다. true면 현재 scale에 toScale을 곱한 값을 목표로 사용합니다.")]
    public bool relativeToCurrent = false;

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

    private CharacterRigRefs _rigRefs;
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

        // 중요:
        // 이 커맨드는 duration 동안 _rect의 localScale을 바꾸는 placement(scale) writer다.
        // FocusPoint solver가 라이브 scale이 아니라 "정착 scale"을 알 수 있도록 게시한다.
        PublishSettledTarget();

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
        {
            ClaimTarget();

            if (_spec.overrideFromScale)
                ApplyScaleXY(_rect, _spec.fromScale);

            CaptureTweenEndpoints();
            PublishSettledTarget();
        }

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = _rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        HasClaimedTarget = true;
    }

    private void CaptureTweenEndpoints()
    {
        Vector3 currentScale = _rect.localScale;

        _startScale = new Vector2(currentScale.x, currentScale.y);
        _targetScale = ResolveTargetScale(currentScale);

        _endScale = currentScale;
        _endScale.x = _targetScale.x;
        _endScale.y = _targetScale.y;
    }

    private Vector2 ResolveTargetScale(Vector3 currentScale)
    {
        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-4 경계).
        Ked.Presentation.Core.StageNodeClaim claim = Ked.Presentation.Core.ScaleToReduction.Reduce(
            _rect.name,
            new Ked.Presentation.Core.ScaleToReduction.Args(
                _spec.relativeToCurrent,
                new Ked.Presentation.Core.Vec2(_spec.toScale.x, _spec.toScale.y)),
            new Ked.Presentation.Core.Vec2(currentScale.x, currentScale.y));

        return new Vector2(claim.Value.X, claim.Value.Y);
    }

    private void PublishSettledTarget()
    {
        if (_rigRefs == null || _rect == null)
            return;

        _rigRefs.PlacementTargets.PublishLocalScale(_rect, _targetScale);
    }

    private void ClearSettledTarget()
    {
        if (_rigRefs == null || _rect == null)
            return;

        _rigRefs.PlacementTargets.Clear(_rect);
    }

    private void CommitFinalState()
    {
        ApplyScaleXY(_rect, _targetScale);
        ClearSettledTarget();

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
        if (!HasClaimedTarget)
            return;

        if (_tween != null && _tween.IsActive())
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