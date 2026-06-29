using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Anchor",
    Order = -930,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -930)]
public sealed class SetAnchorCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Anchor only)")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_VisualOffset;

    [Header("Reset")]
    [Tooltip("체크하면 Anchor 설정 후 CharSlot_Track / Move / X / Y / Rotation / Scale 축을 기본값으로 초기화합니다.")]
    public bool resetSlotPos = true;

    [Tooltip("체크하면 Anchor 설정 후 CharacterPortrait_Track / Move / X / Y / Rotation / SwayPivot / Shake / ActingScale 축을 기본값으로 초기화합니다.")]
    public bool resetCharacterPos = true;
}

public sealed class SetAnchorCommandCharR : CommandBase
{
    private readonly SetAnchorCommandSpecCharR _spec;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private bool _resolveAttempted;

    public SetAnchorCommandCharR(
        SetAnchorCommandSpecCharR spec,
        RoleAnchorTuningDBSO roleTuningDb)
    {
        _spec = spec;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            yield break;

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            return;

        Apply(scope);
    }

    private bool TryResolveRefs(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _rigRefs != null && _rect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            scope,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _rect = _rigRefs.GetRect(_spec.target);

        return _rect != null;
    }

    private void Apply(CommandRunScope scope)
    {
        // SetAnchor는 즉시 확정 command다.
        // 이전 PlaceTo tween/ledger가 남아 있으면 새 anchor 값과 싸우므로 먼저 정리.
        KillTweenAndClearPlacementTarget(_rect);

        if (_spec.resetSlotPos)
            ResetSlotLayers();

        if (_spec.resetCharacterPos)
            ResetCharacterLayers();

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);

        Vector2 anchoredPosition = Vector2.zero;
        float visualScale = 1f;

        if (_roleTuningDb != null && _roleTuningDb.TryGet(tuningKey, out var entry))
        {
            anchoredPosition += entry.offset;
            visualScale *= Mathf.Max(0.0001f, entry.visualScale);
        }

        _rect.anchoredPosition = anchoredPosition;
        _rect.localScale = new Vector3(visualScale, visualScale, 1f);

        // 즉시 적용이므로 Publish하지 않는다.
        // live transform이 이미 settled target.
        ClearPlacementTarget(_rect);
    }

    private void ResetSlotLayers()
    {
        ResetAnchoredPosition(_rigRefs.CharSlot_Track);
        ResetAnchoredPosition(_rigRefs.CharSlot_Track_X);
        ResetAnchoredPosition(_rigRefs.CharSlot_Track_Y);

        ResetEulerAngles(_rigRefs.CharSlot_Rotation);

        ResetLocalScale(_rigRefs.CharSlot_Scale);
    }

    private void ResetCharacterLayers()
    {
        ResetAnchoredPosition(_rigRefs.CharacterPortrait_Track);
        ResetAnchoredPosition(_rigRefs.CharacterPortrait_Track_Move);
        ResetAnchoredPosition(_rigRefs.CharacterPortrait_Track_Move_X);
        ResetAnchoredPosition(_rigRefs.CharacterPortrait_Track_Move_Y);

        ResetEulerAngles(_rigRefs.CharacterPortrait_Rotation);

        ResetAnchoredPosition(_rigRefs.CharacterPortrait_SwayPivot);
        ResetEulerAngles(_rigRefs.CharacterPortrait_SwayPivot);
        ResetLocalScale(_rigRefs.CharacterPortrait_SwayPivot);

        ResetAnchoredPosition(_rigRefs.CharacterPortrait_Shake);
        ResetEulerAngles(_rigRefs.CharacterPortrait_Shake);
        ResetLocalScale(_rigRefs.CharacterPortrait_Shake);

        ResetLocalScale(_rigRefs.CharacterPortrait_ActingScale);
        ResetLocalScale(_rigRefs.CharacterPortrait_ActingScale_X);
        ResetLocalScale(_rigRefs.CharacterPortrait_ActingScale_Y);
    }

    private void ResetAnchoredPosition(RectTransform rect)
    {
        if (rect == null)
            return;

        KillTweenAndClearPlacementTarget(rect);

        rect.anchoredPosition = Vector2.zero;
    }

    private void ResetEulerAngles(RectTransform rect)
    {
        if (rect == null)
            return;

        KillTweenAndClearPlacementTarget(rect);

        rect.localEulerAngles = Vector3.zero;
    }

    private void ResetLocalScale(RectTransform rect)
    {
        if (rect == null)
            return;

        KillTweenAndClearPlacementTarget(rect);

        rect.localScale = Vector3.one;
    }

    private void KillTweenAndClearPlacementTarget(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(true);

        ClearPlacementTarget(rect);
    }

    private void ClearPlacementTarget(RectTransform rect)
    {
        if (_rigRefs == null || rect == null)
            return;

        _rigRefs.PlacementTargets.Clear(rect);
    }
}