using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Background Rig",
    "Set Background Sprite",
    Order = 870,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -964)]
public sealed class SetBackgroundSpriteCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Sprite")]
    public string spriteKey;

    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_BackLayer_Image;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetBackgroundSpriteCommandBgR : CommandBase
{
    private readonly SetBackgroundSpriteCommandSpecBgR _spec;
    private readonly BackgroundSpriteResolver _resolver;

    private Image _image;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetBackgroundSpriteCommandBgR(
        SetBackgroundSpriteCommandSpecBgR spec,
        BackgroundSpriteResolver resolver)
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

        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Apply()
    {
        if (_image == null)
            return;

        Sprite sprite = _resolver.Resolve(_spec.spriteKey, nameof(SetBackgroundSpriteCommandBgR));

        _image.sprite = sprite;
        CharRigImageSizingPolicy.Apply(
            _image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rigRefs =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);

        _image = rigRefs.GetImage(_spec.target);
    }
}