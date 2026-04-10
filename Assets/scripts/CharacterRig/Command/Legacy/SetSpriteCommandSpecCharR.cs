using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Sprite (CharRig)", Order = 875, SetOrder = -965)]
public sealed class SetSpriteCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Sprite")]
    public Sprite sprite;

    [Header("Sizing Policy (Recommended)")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    [Tooltip("HeightFit일 때 가로 정렬")]
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center;
    

    [Header("Legacy")]
    [Tooltip("DEPRECATED: sizingMode 사용 권장")]
    public bool setNativeSize = false; // 기존 데이터 호환용 (나중에 제거)
}

public sealed class SetSpriteCommandCharR : CommandBase
{
    private readonly SetSpriteCommandSpecCharR _spec;

    public SetSpriteCommandCharR(SetSpriteCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            yield break;

        Image image = rig.GetComponent(_spec.target) as Image;
        if (image == null)
            yield break;

        image.sprite = _spec.sprite;

        if (_spec.sprite == null)
            yield break;

        // Legacy 호환: setNativeSize=true면 NativeSizeNoReanchor로 처리
        CharRigImageSizingMode mode = _spec.sizingMode;
        if (_spec.setNativeSize && mode == CharRigImageSizingMode.HeightFitPreserveAspect)
            mode = CharRigImageSizingMode.NativeSizeNoReanchor;

        CharRigImageSizingPolicy.Apply(image, _spec.sprite, mode, _spec.horizontalAlign);
    }
}
