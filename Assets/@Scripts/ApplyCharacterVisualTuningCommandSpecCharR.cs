using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Apply Visual Tuning",
    Order = -930,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -930)]
public sealed class ApplyCharacterVisualTuningCommandSpecCharR
    : CharacterRigCommandSpecBase
{
    [Header("Targets")]
    public CharacterRigTarget offsetTarget =
        CharacterRigTarget.CharacterPortrait_VisualOffset;

    public CharacterRigTarget scaleTarget =
        CharacterRigTarget.CharacterPortrait_VisualOffset;

    [Header("Command Override")]
    public Vector2 offset = Vector2.zero;

    [Tooltip("0 이하이면 1로 취급합니다.")]
    public float scaleMultiplier = 1f;

    [Header("Reset Motion Layers")]
    public bool resetSlotMotionLayers = true;
    public bool resetCharacterMotionLayers = true;
}

public sealed class ApplyCharacterVisualTuningCommandCharR : CommandBase
{
    private readonly ApplyCharacterVisualTuningCommandSpecCharR _spec;
    private readonly CharacterVisualTuningDBSO _visualTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _offsetRect;
    private RectTransform _scaleRect;

    private bool _resolveAttempted;

    public ApplyCharacterVisualTuningCommandCharR(
        ApplyCharacterVisualTuningCommandSpecCharR spec,
        CharacterVisualTuningDBSO visualTuningDb)
    {
        _spec = spec;
        _visualTuningDb = visualTuningDb;
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
            return _rigRefs != null &&
                   _offsetRect != null &&
                   _scaleRect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            scope,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _offsetRect = _rigRefs.GetRect(_spec.offsetTarget);
        _scaleRect = _rigRefs.GetRect(_spec.scaleTarget);

        return _offsetRect != null && _scaleRect != null;
    }

    private void Apply(CommandRunScope scope)
    {
        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(
                scope,
                _spec.slotKey);

        CharacterVisualTuningResult result =
            CharacterVisualTuningResolver.Resolve(
                _visualTuningDb,
                tuningKey,
                _spec.offset,
                _spec.scaleMultiplier);

        KillTweenAndClearPlacementTarget(_offsetRect);
        KillTweenAndClearPlacementTarget(_scaleRect);

        _offsetRect.anchoredPosition = result.Offset;
        _scaleRect.localScale = new Vector3(result.Scale, result.Scale, 1f);

        ClearPlacementTarget(_offsetRect);
        ClearPlacementTarget(_scaleRect);

        if (_spec.resetSlotMotionLayers)
            ResetSlotMotionLayers();

        if (_spec.resetCharacterMotionLayers)
            ResetCharacterMotionLayers();
    }

    private void ResetSlotMotionLayers()
    {
        ResetAnchoredPosition(_rigRefs.CharSlot_Track);
        ResetAnchoredPosition(_rigRefs.CharSlot_Track_X);
        ResetAnchoredPosition(_rigRefs.CharSlot_Track_Y);

        ResetEulerAngles(_rigRefs.CharSlot_Rotation);
        ResetLocalScale(_rigRefs.CharSlot_Scale);
    }

    private void ResetCharacterMotionLayers()
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