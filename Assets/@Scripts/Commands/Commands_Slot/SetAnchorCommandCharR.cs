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
    public CharacterRigTarget target = CharacterRigTarget.Character_Anchor;

    [Header("Preset")]
    public CharAnchorPreset preset = CharAnchorPreset.Center;

    [Tooltip("StageSlot 폭 대비 상대 위치. 0.33이면 좌/우가 화면폭의 약 1/3 지점.")]
    [Range(0f, 0.5f)]
    public float baseRatioX = 0.5f;

    [Header("Tuning (optional)")]
    public CharStageTuningSO globalTuning;
    public RoleAnchorTuningDBSO roleTuningDb;

    [Tooltip("선택: 같은 캐릭터라도 포즈/의상에 따라 보정을 다르게 하고 싶을 때.\n" +
             "예: roleKey=seina, poseKey=outfit_dressWide => DB key 'seina:outfit_dressWide'")]
    public string poseKey = "";

    [Header("Offset (after tuning)")]
    public Vector2 offset = Vector2.zero;

    [Header("Override")]
    public bool overrideAnchoredPosition = false;
    public Vector2 anchoredPositionOverride = Vector2.zero;
}

public sealed class SetAnchorCommandCharR : CommandBase
{
    private readonly SetAnchorCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetAnchorCommandCharR(SetAnchorCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (_rect == null)
            yield break;
        
        Apply();
    }
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        Apply();
    }
    
    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    private void Apply()
    {
        if (_spec.overrideAnchoredPosition)
        {
            _rect.anchoredPosition = _spec.anchoredPositionOverride;
            return;
        }

        Vector2 anchoredPosition = CharAnchorPlacementResolver.ResolveAnchoredPosition(
            _rect,
            _spec.preset,
            _spec.baseRatioX,
            _spec.globalTuning,
            _spec.roleTuningDb,
            _spec.targetKey,
            _spec.poseKey,
            _spec.offset);

        _rect.anchoredPosition = anchoredPosition;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);


        _rect = rigRefs.GetRect(_spec.target);
    }
}