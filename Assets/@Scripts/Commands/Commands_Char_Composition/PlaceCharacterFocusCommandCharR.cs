using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Place Character Focus",
    Order = -155)]
public sealed class PlaceCharacterFocusCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Focus")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("선택한 focus preset에 추가로 더할 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Screen Point")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage/RigSpace local 기준입니다.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Placement Target")]
    [Tooltip("Focus placement 전용 축. 보통 CharSlot_Track_Focus를 사용합니다.")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track_Focus;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class PlaceCharacterFocusCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly PlaceCharacterFocusCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _moveRect;

    private Vector2 _startPosition;
    private Vector2 _destination;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public PlaceCharacterFocusCommandCharR(
        PlaceCharacterFocusCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if(!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _moveRect
            .DOAnchorPos(_destination, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if(!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget(scope);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _moveRect = _rigRefs.GetRect(_spec.moveTarget);
        
        _resolveAttempted = true;
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        _moveRect.DOKill(true);

        _startPosition = _moveRect.anchoredPosition;
        CalculateFocusPlacement(scope);

        _rigRefs.PlacementTargets.Publish(_moveRect, _destination);
        HasClaimedTarget = true;
    }

    private void CalculateFocusPlacement(CommandRunScope scope)
    {
        CharacterFocusPlacementSolver.TryCalculateFocusPlacement(
            scope,
            _spec.slotKey,
            _moveRect,
            _spec.focusPreset,
            _spec.focusOffset,
            _focusTuningDb,
            _spec.screenPoint,
            _spec.screenOffset,
            out _destination);
    }

    private void CommitFinalState()
    {
        _moveRect.anchoredPosition = _destination;
        _rigRefs.PlacementTargets.Clear(_moveRect);

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        _tween = _moveRect
            .DOAnchorPos(_destination, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector2.Distance(_startPosition, _destination);
        float remainingDistance = Vector2.Distance(_moveRect.anchoredPosition, _destination);
        
        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;
        
        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion
}