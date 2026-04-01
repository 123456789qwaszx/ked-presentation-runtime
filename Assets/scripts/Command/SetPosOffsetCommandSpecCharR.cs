using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

[Serializable]
[CommandMenuHint(
    "Char Rig", "#Offset Position (default = ResetToZero)", Order = -890,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    }, 
    SetOrder = -940)]
public class SetPosOffsetCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Offset (Relative)")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 offset = Vector2.zero;

    [Header("Reset (Track Origin)")]
    [Tooltip("체크하면 현재 설정된 오프셋을 무시하고 anchoredPosition을 (0,0)으로 리셋합니다.")]
    public bool resetToZero = true;
}


public sealed class SetPosOffsetCommandCharR : CommandBase
{
    private readonly SetPosOffsetCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetPosOffsetCommandCharR(SetPosOffsetCommandSpecCharR spec)
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

        if (_spec.resetToZero)
            _rect.anchoredPosition = Vector2.zero;

        _rect.anchoredPosition += _spec.offset;
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
}