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
    private readonly SoloShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;
    private RectTransform _scaleRect;

    private Vector2 _destination;
    private Vector2 _targetScale;

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

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_moveRect);

        sequence.Join(
            _moveRect
                .DOAnchorPos(_destination, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_moveRect));

        Vector3 endScale = _scaleRect.localScale;
        endScale.x = _targetScale.x;
        endScale.y = _targetScale.y;

        sequence.Join(
            _scaleRect
            .DOScale(endScale, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_scaleRect));
        

        sequence.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return sequence.WaitForCompletion();
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
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _moveRect = rig.GetRect(_spec.moveTarget);
        _scaleRect = rig.GetRect(_spec.scaleTarget);
    }

    private bool ClaimTargets(CommandRunScope scope)
    {
        KillPreviousTweens();

        if (!ComputeDestination(scope))
            return false;

        HasClaimedTargets = true;
        return true;
    }

    private void KillPreviousTweens()
    {
        _moveRect.DOKill(true);
        _scaleRect.DOKill(true);
    }

    private bool ComputeDestination(CommandRunScope scope)
    {
        _targetScale = _spec.targetScale;

        CharacterPlacementScalePreview scalePreview = new CharacterPlacementScalePreview(_scaleRect, _targetScale);

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

    private void CommitFinalState()
    {
        _moveRect.anchoredPosition = _destination;

        Vector3 scale = _scaleRect.localScale;
        scale.x = _targetScale.x;
        scale.y = _targetScale.y;
        _scaleRect.localScale = scale;

        HasClaimedTargets = false;
    }
}