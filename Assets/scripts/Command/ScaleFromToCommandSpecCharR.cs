using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Scale (From → To)",
    Order = -170
)]
public class ScaleFromToCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Scale;

    [Header("Scale (XY)")]
    /// <summary>
    /// 최종 스케일 (localScale X/Y)
    /// </summary>
    public Vector2 toScale = Vector2.one;

    [Header("From")]
    /// <summary>
    /// true면 fromScale을 시작 스케일로 사용하고,
    /// false면 현재 localScale에서부터 시작한다.
    /// </summary>
    public bool overrideFromScale = false;

    /// <summary>
    /// overrideFromScale이 true일 때만 사용되는 시작 스케일 (X/Y).
    /// </summary>
    public Vector2 fromScale = Vector2.one;

    [Header("Tween")]
    /// <summary>
    /// 트윈 시간. <= 0이면 즉시 toScale로 스냅.
    /// </summary>
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    public bool wait = false;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class ScaleFromToCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ScaleFromToCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ScaleFromToCommandCharR(ScaleFromToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(false);

        if (_spec.overrideFromScale)
        {
            ApplyScaleXY(_rect, _spec.fromScale);
        }

        if (_spec.duration <= 0f)
        {
            ApplyScaleXY(_rect, _spec.toScale);
            yield break;
        }

        Vector3 endScale = _rect.localScale;
        endScale.x = _spec.toScale.x;
        endScale.y = _spec.toScale.y;

        Tween tween = _rect
            .DOScale(endScale, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true);
        
        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        OnCommandCompleted(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.DOKill();
        
        ApplyScaleXY(_rect, _spec.toScale);
        _rect = null;
    }

    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
    
    
    private void ApplyScaleXY(RectTransform rect, Vector2 targetXY)
    {
        Vector3 s = rect.localScale;
        s.x = targetXY.x;
        s.y = targetXY.y;
        rect.localScale = s;
    }
}