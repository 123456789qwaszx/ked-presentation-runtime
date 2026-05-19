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

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

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

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.targetKey);

        _image = rig.GetComponent(_spec.target) as Image;

        if (_image == null)
        {
            throw new InvalidOperationException(
                $"[SetSpriteCommandCharR] Target Image not found. targetKey='{_spec.targetKey}', target='{_spec.target}'.");
        }
    }

    private void Apply()
    {
        _image.sprite = _spec.sprite;

        if (_spec.sprite == null)
            return;

        CharRigImageSizingMode mode = _spec.sizingMode;

        CharRigImageSizingPolicy.Apply(
            _image,
            _spec.sprite,
            mode,
            _spec.horizontalAlign);
    }
}