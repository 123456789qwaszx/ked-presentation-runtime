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

    [Tooltip("focusPreset이 Custom일 때 사용할 custom point key입니다. 예: hand_left, weapon, phone")]
    public string customFocusKey = "";

    [Tooltip("비워두면 roleKey만 tuning key로 사용합니다. 입력하면 roleKey:poseKey로 DB를 찾습니다.")]
    public string poseKey = "";

    [Tooltip("선택한 focus preset에 추가로 더할 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Screen Point")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage/RigSpace local 기준입니다.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Placement Target")]
    [Tooltip("보통 CharSlot_Track을 사용합니다. focus 측정은 CharSlot_Scale 기준으로 하고, 이동은 이 target에 적용합니다.")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class PlaceCharacterFocusCommandCharR : CommandBase
{
    private readonly PlaceCharacterFocusCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;
    private Tween _tween;

    private bool _resolveAttempted;
    private bool _hasComputedDestination;
    private bool _canCommitFinalState;

    private Vector2 _destination;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PlaceCharacterFocusCommandCharR(
        PlaceCharacterFocusCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!EnsureMoveRect(scope))
            yield break;

        if (_spec.killTween)
            _moveRect.DOKill(true);

        _hasComputedDestination = false;

        if (!ComputeDestinationIfNeeded(scope))
            yield break;

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            CommitDestination();
            ClearRuntimeState();
            yield break;
        }

        _tween = _moveRect
            .DOAnchorPos(_destination, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _moveRect == null)
                    return;

                CommitDestination();
                ClearRuntimeState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!EnsureMoveRect(scope))
            return;

        if (_spec.killTween)
            _moveRect.DOKill(false);

        _hasComputedDestination = false;

        if (!ComputeDestinationIfNeeded(scope))
            return;

        CommitDestination();
        ClearRuntimeState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        if (!EnsureMoveRect(scope))
            return;

        _tween?.Kill(false);
        _moveRect.DOKill(false);

        if (!ComputeDestinationIfNeeded(scope))
            return;

        CommitDestination();
        ClearRuntimeState();
    }

    private bool EnsureMoveRect(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _moveRect != null;

        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        if (rigRefs == null)
            return false;

        _moveRect = rigRefs.GetRect(_spec.moveTarget);

        if (_moveRect == null)
        {
            Debug.LogWarning(
                $"[PlaceCharacterFocusCommandCharR] Missing move target. " +
                $"slotKey='{_spec.slotKey}', target='{_spec.moveTarget}'.");
            return false;
        }

        return true;
    }

    private bool ComputeDestinationIfNeeded(CommandRunScope scope)
    {
        if (_hasComputedDestination)
            return true;

        _hasComputedDestination = true;

        if (!CharacterPlacementSolver.TryCalculateFocusPlacement(
                scope,
                _spec.slotKey,
                _moveRect,
                _spec.focusPreset,
                _spec.poseKey,
                _spec.customFocusKey,
                _spec.focusOffset,
                _focusTuningDb,
                _spec.screenPoint,
                _spec.screenOffset,
                CharacterPlacementScalePreview.None,
                out CharacterPlacementResult placement))
        {
            Debug.LogWarning(
                $"[PlaceCharacterFocusCommandCharR] Failed to calculate placement. " +
                $"slotKey='{_spec.slotKey}', focus='{_spec.focusPreset}', screenPoint='{_spec.screenPoint}'.");
            return false;
        }

        _destination = placement.DestinationAnchoredPosition;
        return true;
    }

    private void CommitDestination()
    {
        if (_moveRect != null)
            _moveRect.anchoredPosition = _destination;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;
        _tween = null;
        _moveRect = null;
    }
}