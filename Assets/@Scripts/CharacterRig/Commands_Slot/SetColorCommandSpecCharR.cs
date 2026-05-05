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
public class SetColorCommandSpecCharR : CharacterRigCommandSpecBase
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

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetColorCommandCharR(SetColorCommandSpecCharR spec)
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

        _image = rig.GetGraphic(_spec.target) as Image;

        if (_image == null)
        {
            throw new InvalidOperationException(
                $"[SetColorCommandCharR] Target Image not found. targetKey='{_spec.targetKey}', target='{_spec.target}'.");
        }
    }

    private void Apply()
    {
        Color color = _spec.color;

        if (_spec.keepAlpha)
            color.a = _image.color.a;

        _image.color = color;
    }
}