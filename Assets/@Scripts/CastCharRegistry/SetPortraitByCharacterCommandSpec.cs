using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Portrait By Character",
    Order = 871,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -963)]
public sealed class SetPortraitByCharacterCommandSpec : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode =
        CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SetPortraitByCharacterCommand : CommandBase
{
    private readonly SetPortraitByCharacterCommandSpec _spec;
    private readonly PortraitResolver _resolver;

    private Image _image;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetPortraitByCharacterCommand(
        SetPortraitByCharacterCommandSpec spec,
        PortraitResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetPortraitByCharacterCommand] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetPortraitByCharacterCommand] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetPortraitByCharacterCommand] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _image = rig.GetComponent(_spec.target) as Image;

        if (_image == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[SetPortraitByCharacterCommand] Target image not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }

    private void Apply()
    {
        if (_image == null)
            return;

        Sprite sprite = ResolveSprite(_spec.portrait);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"[SetPortraitByCharacterCommand] Failed to resolve portrait:\n" +
                $"  Character: {_spec.portrait?.character}\n" +
                $"  Variant: {_spec.portrait?.variant}\n" +
                $"  Emotion: {_spec.portrait?.emotion}");
            return;
        }

        _image.sprite = sprite;

        CharRigImageSizingPolicy.Apply(
            _image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }

    private Sprite ResolveSprite(PortraitIdentity id)
    {
        if (id == null)
            return null;

        string character = SafeTrim(id.character);
        if (string.IsNullOrEmpty(character))
            return null;

        string variant = ResolveVariantKey(character, id.variant);
        return _resolver.Resolve(character, variant, id.emotion);
    }

    private static string ResolveVariantKey(string character, string variant)
    {
        if (string.IsNullOrEmpty(variant))
            return "";

        variant = variant.Trim();

        if (variant.StartsWith(character + "_", StringComparison.Ordinal))
            return variant;

        return $"{character}_{variant}";
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }
}