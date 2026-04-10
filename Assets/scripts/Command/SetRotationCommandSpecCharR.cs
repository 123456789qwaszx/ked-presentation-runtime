using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;


[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Rotation (Z)",
    Order = 865
)]
public class SetRotationCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Root;

    [Header("Rotation (Z axis)")]
    public float toAngle = 0f;
}

public sealed class SetRotationCommandCharR : CommandBase
{
    private readonly SetRotationCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetRotationCommandCharR(SetRotationCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);
        
        Apply();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.DOKill(false);

        Apply();
    }
    
    private void Apply()
    {
        Vector3 euler = _rect.localEulerAngles;
        euler.z = _spec.toAngle;
        _rect.localEulerAngles = euler;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}