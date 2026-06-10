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
}

public sealed class PlaceCharacterFocusCommandCharR : CommandBase
{
    private readonly PlaceCharacterFocusCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private RectTransform _moveRect;
    private Vector2 _destination;

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
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = _moveRect
            .DOAnchorPos(_destination, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget(scope);
        
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _moveRect = rig.GetRect(_spec.moveTarget);
        
        _resolveAttempted = true;
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        _moveRect.DOKill(true);

        ComputeDestination(scope);

        HasClaimedTarget = true;
    }

    private void ComputeDestination(CommandRunScope scope)
    {
       CharacterFocusPlacementSolver.TryCalculateFocusPlacement(
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
                out Vector2 destPos);

        _destination = destPos;
    }

    private void CommitFinalState()
    {
        _moveRect.anchoredPosition = _destination;

        HasClaimedTarget = false;
    }
}