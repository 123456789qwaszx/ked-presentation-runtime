using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Portrait Sprite", Order = 870,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
        CommandMenuSets.SetupEmotion
    }, SetOrder = -964)]
public sealed class SetPortraitSpriteCommandSpecCharR : CommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode =
        CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetPortraitSpriteCommandCharR : CommandBase
{
    private readonly SetPortraitSpriteCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;

    private Image _image;
    private bool _resolveAttempted;

    public SetPortraitSpriteCommandCharR(
        SetPortraitSpriteCommandSpecCharR spec,
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
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_image == null)
            return;

        Apply();
    }
    
    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _image = rig.GetComponent(_spec.target) as Image;
    }

    private void Apply()
    {
        if (_image == null)
            return;

        Sprite sprite = ResolveSprite(_spec.portrait);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"[SetPortraitSpriteCommandCharR] Failed to resolve portrait:\n" +
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