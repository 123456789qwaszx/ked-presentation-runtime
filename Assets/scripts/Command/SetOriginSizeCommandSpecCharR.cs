using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set OriginSize", Order = -915,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    }, SetOrder = -964)]
public class SetOriginSizeCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Pad;

    [Header("Scale")]
    public Vector2 toScale = Vector2.one;
    
    [Header("Rotation (Z axis)")]
    public float toAngle = 0f;
    
    [Header("Anchor (if nativeSize, not work.)")]
    public Vector2 anchorMin = new Vector2(0f, 0f);
    public Vector2 anchorMax = new Vector2(1f, 1f);
    
}

public sealed class SetOriginSizeCommandCharR : CommandBase
{
    private readonly SetOriginSizeCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetOriginSizeCommandCharR(SetOriginSizeCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        ApplyScale();
        ApplyRotation();
        ApplyPad();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;
        
        ApplyScale();
        ApplyRotation();
        ApplyPad();
    }
    
    
    private void ApplyScale()
    {
        _rect.DOKill(false);

        Vector3 scale = _rect.localScale;
        scale.x = _spec.toScale.x;
        scale.y = _spec.toScale.y;
        _rect.localScale = scale;
    }
    
    private void ApplyRotation()
    {
        Vector3 euler = _rect.localEulerAngles;
        euler.z = _spec.toAngle;
        _rect.localEulerAngles = euler;
    }
    
    private void ApplyPad()
    {
        _rect.anchorMin = _spec.anchorMin;
        _rect.anchorMax = _spec.anchorMax;
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}