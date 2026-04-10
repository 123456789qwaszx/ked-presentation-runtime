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
public class SetColorCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Color")]
    public Color color = Color.white;

    [Tooltip("체크하면 현재 알파(A)는 그대로 두고 색상(RGB)만 변경합니다.")]
    public bool keepAlpha = true;
}

public sealed class SetColorCommandCharR : CommandBase
{
    private readonly SetColorCommandSpecCharR _spec;

    private Image _image;
    private bool _resolveAttempted;

    public SetColorCommandCharR(SetColorCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _image = rig.GetGraphic(_spec.target) as Image;
    }

    private void Apply()
    {
        if (_image == null)
            return;

        Color color = _spec.color;

        if (_spec.keepAlpha)
            color.a = _image.color.a;

        _image.color = color;
    }
}