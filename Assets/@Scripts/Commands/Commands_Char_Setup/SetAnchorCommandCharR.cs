using System;
using System.Collections;
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
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Anchor;

    [Header("Preset")]
    public CharAnchorPreset preset = CharAnchorPreset.Center;

    [Tooltip("StageSlot 폭 대비 상대 위치. 0.33이면 좌/우가 화면폭의 약 1/3 지점.")]
    [Range(0f, 0.5f)]
    public float baseRatioX = 0.33f;

    [Header("Offset (after tuning)")]
    public Vector2 offset = Vector2.zero;
    
    [Header("Reset")]
    [Tooltip("체크하면 Anchor 설정 후 CharSlot_Track / Move / X / Y / Rotation / Scale 축을 기본값으로 초기화합니다.")]
    public bool resetSlotPos = true;
    
    [Tooltip("체크하면 Anchor 설정 후 CharacterPortrait_Track / Move / X / Y / Rotation / SwayPivot / Shake / ActingScale 축을 기본값으로 초기화합니다.")]
    public bool resetCharacterPos = true;
}

public sealed class SetAnchorCommandCharR : CommandBase
{
    private readonly SetAnchorCommandSpecCharR _spec;
    private readonly CharStageTuningSO _globalTuning;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;
    
    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;
    private bool _resolveAttempted;

    public SetAnchorCommandCharR(SetAnchorCommandSpecCharR spec, CharStageTuningSO globalTuning, RoleAnchorTuningDBSO roleTuningDb)
    {
        _spec = spec;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        Apply(scope);
        yield break;
    }
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
    }
    
    private void Apply(CommandRunScope scope)
    {
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);
    
        Vector2 anchoredPosition = CharAnchorPlacementResolver.ResolveAnchoredPosition(
            _rect,
            _spec.preset,
            _spec.baseRatioX,
            _globalTuning,
            _roleTuningDb,
            tuningKey,
            _spec.offset);

        _rect.anchoredPosition = anchoredPosition;
        
        if (_spec.resetSlotPos)
            ResetSlotLayers();
        
        if (_spec.resetCharacterPos)
            ResetCharacterLayers();
    }
    
    private void ResetSlotLayers()
    {
        _rigRefs.CharSlot_Track.anchoredPosition = Vector2.zero;
        _rigRefs.CharSlot_Track_X.anchoredPosition = Vector2.zero;
        _rigRefs.CharSlot_Track_Y.anchoredPosition = Vector2.zero;
        
        _rigRefs.CharSlot_Rotation.localEulerAngles= Vector3.zero;
        
        _rigRefs.CharSlot_Scale.localScale = Vector3.one;
    }
    
    private void ResetCharacterLayers()
    {
        _rigRefs.CharacterPortrait_Track.anchoredPosition = Vector2.zero;
        _rigRefs.CharacterPortrait_Track_Move.anchoredPosition = Vector2.zero;
        _rigRefs.CharacterPortrait_Track_X.anchoredPosition = Vector2.zero;
        _rigRefs.CharacterPortrait_Track_Y.anchoredPosition = Vector2.zero;

        _rigRefs.CharacterPortrait_Rotation.localEulerAngles = Vector3.zero;

        _rigRefs.CharacterPortrait_SwayPivot.anchoredPosition = Vector2.zero;
        _rigRefs.CharacterPortrait_SwayPivot.localEulerAngles = Vector3.zero;
        _rigRefs.CharacterPortrait_SwayPivot.localScale = Vector3.one;

        _rigRefs.CharacterPortrait_Shake.anchoredPosition = Vector2.zero;
        _rigRefs.CharacterPortrait_Shake.localEulerAngles = Vector3.zero;
        _rigRefs.CharacterPortrait_Shake.localScale = Vector3.one;

        _rigRefs.CharacterPortrait_ActingScale.localScale = Vector3.one;
        _rigRefs.CharacterPortrait_ActingScale_X.localScale = Vector3.one;
        _rigRefs.CharacterPortrait_ActingScale_Y.localScale = Vector3.one;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = _rigRefs.GetRect(_spec.target);
    }
}