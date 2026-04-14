using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetSpriteByCharacterCommandCharR] characterKey is null or empty.");
            yield break;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            yield break;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            yield break;
        }

        Image image = rig.GetComponent(_spec.target) as Image;
        if (image == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetSpriteByCharacterCommandCharR] Target image not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
            yield break;
        }

        image.sprite = _spec.sprite;

        if (_spec.sprite == null)
            yield break;

        CharRigImageSizingMode mode = _spec.sizingMode;
        if (_spec.setNativeSize && mode == CharRigImageSizingMode.HeightFitPreserveAspect)
            mode = CharRigImageSizingMode.NativeSizeNoReanchor;

        CharRigImageSizingPolicy.Apply(
            image,
            _spec.sprite,
            mode,
            _spec.horizontalAlign);
    }
    
    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetSpriteByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

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

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}