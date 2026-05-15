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
public sealed class SetPortraitSpriteCommandSpecCharR : CharacterRigCommandSpecBase
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

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

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

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);

        _image = rigRefs.GetComponent(_spec.target) as Image;

        if (_image == null)
        {
            throw new InvalidOperationException(
                $"[SetPortraitSpriteCommandCharR] Image missing. targetKey='{_spec.targetKey}', target='{_spec.target}'.");
        }
    }

    private void Apply(CommandRunScope scope)
    {
        if (_image == null)
            return;

        Sprite sprite =
            PortraitIdentityResolveUtility.ResolveSprite(
                scope,
                _resolver,
                _spec.targetKey,
                _spec.portrait,
                nameof(SetPortraitSpriteCommandCharR));

        _image.sprite = sprite;

        CharRigImageSizingPolicy.Apply(
            _image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }
}