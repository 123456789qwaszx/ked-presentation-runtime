using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Portrait Pose", Order = 869,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -963)]
public sealed class SetPortraitPoseCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Pose / Variant")]
    [Tooltip("의상/자세/바디 variant 키. 예: a, b.")]
    public string variantKey = PortraitResolver.DefaultVariant;

    [Header("Default Face")]
    [Tooltip("pose 변경 시 함께 적용할 기본 표정. 비우면 PortraitResolver.DefaultEmotion 사용.")]
    public string defaultEmotionKey = PortraitResolver.DefaultEmotion;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetPortraitPoseCommandCharR : CommandBase
{
    private readonly SetPortraitPoseCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;

    private Image _image;
    private bool _resolveAttempted;

    public SetPortraitPoseCommandCharR(
        SetPortraitPoseCommandSpecCharR spec,
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

    private void Apply(CommandRunScope scope)
    {
        string resolvedSlotKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, _spec.slotKey);

        string variantKey = string.IsNullOrWhiteSpace(_spec.variantKey)
            ? PortraitResolver.DefaultVariant
            : _spec.variantKey;

        scope.CastRegistry.SetVariant(resolvedSlotKey, variantKey);

        // var portrait = new PortraitIdentity
        // {
        //     character = "",
        //     variant = variantKey,
        //     emotion = _spec.defaultEmotionKey
        // };
        //
        // Sprite sprite = _resolver.Resolve(
        //     scope,
        //     resolvedSlotKey,
        //     portrait,
        //     nameof(SetPortraitPoseCommandCharR));
        //
        // _image.sprite = sprite;
        // CharRigImageSizingPolicy.Apply(
        //     _image,
        //     sprite,
        //     _spec.sizingMode,
        //     _spec.horizontalAlign);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        _image = rigRefs.GetImage(_spec.target);
    }
}