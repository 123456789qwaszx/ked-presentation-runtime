using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Color (Z)",
    Order = 870
)]
public class SetColorCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")] public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Color")] public Color color = Color.white;

    [Tooltip("체크하면 현재 알파(A)는 그대로 두고 색상(RGB)만 변경합니다.")]
    public bool keepAlpha = true;
}

public class SetColorCommandCharR : CommandBase
{
    private readonly SetColorCommandSpecCharR _spec;

    public SetColorCommandCharR(SetColorCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out var rig))
            yield break;

        Image image = rig.GetGraphic(_spec.target) as Image;
        if (image == null)
            yield break;

        Color color = _spec.color;

        if (_spec.keepAlpha)
        {
            Color curAlpha = image.color;
            color.a = curAlpha.a;
        }

        image.color = color;
    }
}