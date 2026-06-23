using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Sprite (CharRig)", Order = 875, SetOrder = -965)]
public sealed class SetSpriteCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;

    [Header("Sprite")]
    public Sprite sprite;

    [Header("Sizing Policy (Recommended)")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    [Tooltip("HeightFit일 때 가로 정렬")]
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetSpriteCommandCharR : CommandBase
{
    private readonly SetSpriteCommandSpecCharR _spec;

    private Image _image;
    private bool _resolveAttempted;

    public SetSpriteCommandCharR(SetSpriteCommandSpecCharR spec)
    {
        _spec = spec;
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

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _image = rig.GetImage(_spec.target);
    }

    private void Apply()
    {
        if (_spec.sprite == null)
            return;
        
        _image.sprite = _spec.sprite;
        
        CharRigImageSizingPolicy.Apply(
            _image,
            _spec.sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }
}