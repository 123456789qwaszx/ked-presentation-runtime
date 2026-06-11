using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Solo Shot",
    Order = -152)]
public sealed class SoloShotCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Focus")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("focusPreset이 Custom일 때 사용할 custom point key입니다. 예: hand_left, weapon, phone")]
    public string customFocusKey = "";

    [Tooltip("비워두면 roleKey만 tuning key로 사용합니다. 입력하면 roleKey:poseKey로 DB를 찾습니다.")]
    public string poseKey = "";

    [Tooltip("선택한 focus preset에 추가로 더할 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Screen Point")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("단독샷 구도 보정값입니다. 기본값은 얼굴을 살짝 위로 올립니다.")]
    public Vector2 screenOffset = new Vector2(0f, 80f);

    [Header("Targets")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track;

    [Tooltip("단독샷 스케일을 적용할 대상입니다. 보통 CharSlot_Scale입니다.")]
    public CharacterRigTarget scaleTarget = CharacterRigTarget.CharSlot_Scale;

    [Header("Scale")]
    public Vector2 targetScale = new Vector2(1.08f, 1.08f);

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;
}

public sealed class SoloShotCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly SoloShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;
    private RectTransform _scaleRect;

    private Vector2 _startPosition;
    private Vector2 _destination;

    private Vector2 _startScale;
    private Vector2 _targetScale;

    private Sequence _sequence;

    private bool _resolveAttempted;

    private bool HasClaimedTargets { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public SoloShotCommandCharR(
        SoloShotCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTargets(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _sequence = CreateSequence(_spec.duration)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTargets)
            ClaimTargets(scope);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _moveRect = rig.GetRect(_spec.moveTarget);
        _scaleRect = rig.GetRect(_spec.scaleTarget);

        _resolveAttempted = true;
    }

    private void ClaimTargets(CommandRunScope scope)
    {
        _moveRect.DOKill(true);
        _scaleRect.DOKill(true);

        _startPosition = _moveRect.anchoredPosition;

        Vector3 currentScale = _scaleRect.localScale;
        _startScale = new Vector2(currentScale.x, currentScale.y);

        ComputeDestination(scope);

        HasClaimedTargets = true;
    }

    private void ComputeDestination(CommandRunScope scope)
    {
        _targetScale = _spec.targetScale;

        CharacterFocusPlacementSolver.TryCalculateFocusPlacement(
            scope,
            _spec.slotKey,
            _moveRect,
            _spec.focusPreset,
            _spec.customFocusKey,
            _spec.focusOffset,
            _focusTuningDb,
            _spec.screenPoint,
            _spec.screenOffset,
            out Vector2 destPos);

        _destination = destPos;
    }

    private Sequence CreateSequence(float duration)
    {
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(this);

        sequence.Join(
            _moveRect
                .DOAnchorPos(_destination, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_moveRect));

        Vector3 endScale = _scaleRect.localScale;
        endScale.x = _targetScale.x;
        endScale.y = _targetScale.y;

        sequence.Join(
            _scaleRect
                .DOScale(endScale, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_scaleRect));

        return sequence;
    }

    private void CommitFinalState()
    {
        _moveRect.anchoredPosition = _destination;

        Vector3 scale = _scaleRect.localScale;
        scale.x = _targetScale.x;
        scale.y = _targetScale.y;
        _scaleRect.localScale = scale;

        HasClaimedTargets = false;
        _sequence = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTargets)
            return;
        
        _sequence.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _sequence = CreateSequence(duration)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float moveRatio = CalculateMoveRemainingRatio();
        float scaleRatio = CalculateScaleRemainingRatio();

        float remainingRatio = Mathf.Max(moveRatio, scaleRatio);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private float CalculateMoveRemainingRatio()
    {
        float originalDistance = Vector2.Distance(_startPosition, _destination);
        float remainingDistance = Vector2.Distance(_moveRect.anchoredPosition, _destination);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    private float CalculateScaleRemainingRatio()
    {
        Vector3 currentScale3 = _scaleRect.localScale;
        Vector2 currentScale = new(currentScale3.x, currentScale3.y);

        float originalDistance = Vector2.Distance(_startScale, _targetScale);
        float remainingDistance = Vector2.Distance(currentScale, _targetScale);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    #endregion
}