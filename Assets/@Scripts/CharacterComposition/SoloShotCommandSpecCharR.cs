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

    public bool applyScale = true;

    [Header("Scale")]
    public Vector2 targetScale = new Vector2(1.08f, 1.08f);

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class SoloShotCommandCharR : CommandBase
{
    private readonly SoloShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;
    private RectTransform _scaleRect;

    private Tween _moveTween;
    private Tween _scaleTween;

    private bool _resolveAttempted;
    private bool _hasComputedDestination;
    private bool _canCommitFinalState;

    private Vector2 _destination;
    private Vector2 _targetScale;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SoloShotCommandCharR(
        SoloShotCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!EnsureRefs(scope))
            yield break;

        if (_spec.killTween)
        {
            _moveRect.DOKill(true);

            if (_scaleRect != null)
                _scaleRect.DOKill(true);
        }

        _hasComputedDestination = false;

        if (!ComputeDestinationIfNeeded(scope))
            yield break;

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            ClearRuntimeState();
            yield break;
        }

        _moveTween = _moveRect
            .DOAnchorPos(_destination, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect);

        if (_spec.applyScale && _scaleRect != null)
        {
            Vector3 endScale = _scaleRect.localScale;
            endScale.x = _targetScale.x;
            endScale.y = _targetScale.y;

            _scaleTween = _scaleRect
                .DOScale(endScale, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_scaleRect);
        }

        if (_spec.wait)
            yield return WaitUntilTweensComplete();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!EnsureRefs(scope))
            return;

        KillTweens(false);

        _hasComputedDestination = false;

        if (!ComputeDestinationIfNeeded(scope))
            return;

        CommitFinalState();
        ClearRuntimeState();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        if (!EnsureRefs(scope))
            return;

        KillTweens(false);

        if (!ComputeDestinationIfNeeded(scope))
            return;

        CommitFinalState();
        ClearRuntimeState();
    }

    private bool EnsureRefs(CommandRunScope scope)
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
                $"[SoloShotCommandCharR] Missing move target. " +
                $"slotKey='{_spec.slotKey}', target='{_spec.moveTarget}'.");
            return false;
        }

        if (_spec.applyScale)
        {
            _scaleRect = rigRefs.GetRect(_spec.scaleTarget);

            if (_scaleRect == null)
            {
                Debug.LogWarning(
                    $"[SoloShotCommandCharR] Missing scale target. " +
                    $"slotKey='{_spec.slotKey}', target='{_spec.scaleTarget}'.");
                return false;
            }
        }

        return true;
    }

    private bool ComputeDestinationIfNeeded(CommandRunScope scope)
    {
        if (_hasComputedDestination)
            return true;

        _hasComputedDestination = true;
        _targetScale = _spec.targetScale;

        CharacterPlacementScalePreview scalePreview =
            _spec.applyScale
                ? new CharacterPlacementScalePreview(_scaleRect, _targetScale)
                : CharacterPlacementScalePreview.None;

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
                scalePreview,
                out CharacterPlacementResult placement))
        {
            Debug.LogWarning(
                $"[SoloShotCommandCharR] Failed to calculate solo shot placement. " +
                $"slotKey='{_spec.slotKey}', focus='{_spec.focusPreset}', screenPoint='{_spec.screenPoint}'.");
            return false;
        }

        _destination = placement.DestinationAnchoredPosition;
        return true;
    }

    private IEnumerator WaitUntilTweensComplete()
    {
        while (IsTweenActive(_moveTween) || IsTweenActive(_scaleTween))
            yield return null;
    }

    private static bool IsTweenActive(Tween tween)
    {
        return tween != null && tween.IsActive() && tween.IsPlaying();
    }

    private void CommitFinalState()
    {
        if (_moveRect != null)
            _moveRect.anchoredPosition = _destination;

        if (_spec.applyScale && _scaleRect != null)
        {
            Vector3 s = _scaleRect.localScale;
            s.x = _targetScale.x;
            s.y = _targetScale.y;
            _scaleRect.localScale = s;
        }
    }

    private void KillTweens(bool complete)
    {
        _moveTween?.Kill(complete);
        _scaleTween?.Kill(complete);

        if (_moveRect != null)
            _moveRect.DOKill(complete);

        if (_scaleRect != null)
            _scaleRect.DOKill(complete);

        _moveTween = null;
        _scaleTween = null;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;

        _moveTween = null;
        _scaleTween = null;
        _moveRect = null;
        _scaleRect = null;
    }
}