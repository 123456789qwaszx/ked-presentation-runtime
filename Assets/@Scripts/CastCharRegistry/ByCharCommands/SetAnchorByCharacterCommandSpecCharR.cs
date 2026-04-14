using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Anchor By Character",
    Order = -929,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -929)]
public sealed class SetAnchorByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

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
             "예: characterKey=seina, poseKey=outfit_dressWide => DB key 'seina:outfit_dressWide'")]
    public string poseKey = "";

    [Header("Offset (after tuning)")]
    public Vector2 offset = Vector2.zero;

    [Header("Override")]
    public bool overrideAnchoredPosition = false;
    public Vector2 anchoredPositionOverride = Vector2.zero;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SetAnchorByCharacterCommandCharR : CommandBase
{
    private readonly SetAnchorByCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetAnchorByCharacterCommandCharR(SetAnchorByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Apply()
    {
        if (_rect == null)
            return;

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
            _spec.characterKey,
            _spec.poseKey,
            _spec.offset);

        _rect.anchoredPosition = anchoredPosition;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetAnchorByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetAnchorByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetAnchorByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _rect = rig.GetRect(_spec.target);
        if (_rect == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[SetAnchorByCharacterCommandCharR] Target rect not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}