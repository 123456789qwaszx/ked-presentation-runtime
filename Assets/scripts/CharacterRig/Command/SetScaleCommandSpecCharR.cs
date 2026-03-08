using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Set Scale", Order = -850,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    }, SetOrder = -964)]
public class SetScaleCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Scale;

    [Header("Scale")]
    public Vector2 toScale = Vector2.one;
}

public sealed class SetScaleCommandCharR : CommandBase
{
    private readonly SetScaleCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetScaleCommandCharR(SetScaleCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        Apply();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;
        
        Apply();
    }
    

    private void Apply()
    {
        _rect.DOKill(false);

        Vector3 scale = _rect.localScale;
        scale.x = _spec.toScale.x;
        scale.y = _spec.toScale.y;
        _rect.localScale = scale;
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}