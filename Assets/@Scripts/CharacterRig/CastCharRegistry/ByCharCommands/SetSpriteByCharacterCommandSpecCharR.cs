using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Sprite By Character (CharRig)", Order = 876, SetOrder = -964)]
public sealed class SetSpriteByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Sprite")]
    public Sprite sprite;

    [Header("Sizing Policy (Recommended)")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    [Tooltip("HeightFit일 때 가로 정렬")]
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;

    [Header("Legacy")]
    [Tooltip("DEPRECATED: sizingMode 사용 권장")]
    public bool setNativeSize = false;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SetSpriteByCharacterCommandCharR : CommandBase
{
    private readonly SetSpriteByCharacterCommandSpecCharR _spec;

    public SetSpriteByCharacterCommandCharR(SetSpriteByCharacterCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        Apply(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        if (scope == null)
            return;

        string characterKey = _spec.characterKey;
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetSpriteByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        characterKey = characterKey.Trim();

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        Image image = rig.GetComponent(_spec.target) as Image;
        if (image == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] Target image not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
            return;
        }

        image.sprite = _spec.sprite;

        if (_spec.sprite == null)
            return;

        CharRigImageSizingMode mode = _spec.sizingMode;
        if (_spec.setNativeSize && mode == CharRigImageSizingMode.HeightFitPreserveAspect)
            mode = CharRigImageSizingMode.NativeSizeNoReanchor;

        CharRigImageSizingPolicy.Apply(
            image,
            _spec.sprite,
            mode,
            _spec.horizontalAlign);
    }
}