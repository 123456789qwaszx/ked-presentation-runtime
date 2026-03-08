using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Rotate (From → To)",
    Order = -180
)]
public class RotateFromToCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Root;

    [Header("Rotation (localEulerAngles)")]
    /// <summary>
    /// 최종 Euler 각도 (localEulerAngles 기준, X/Y/Z 모두 사용 가능).
    /// </summary>
    public Vector3 toEuler = Vector3.zero;
    
    [Header("From")]
    /// <summary>
    /// true면 fromEuler를 시작 각도로 사용하고,
    /// false면 현재 localEulerAngles에서부터 시작한다.
    /// </summary>
    public bool overrideFromEuler = false;

    /// <summary>
    /// overrideFromEuler가 true일 때만 사용되는 시작 각도.
    /// </summary>
    public Vector3 fromEuler = Vector3.zero;

    [Header("Tween")]
    /// <summary>
    /// 트윈 시간. <= 0이면 즉시 toEuler로 스냅.
    /// </summary>
    public float duration = 0.4f;

    public Ease ease = Ease.OutCubic;

    /// <summary>
    /// true면 트윈이 끝날 때까지 Step 진행을 멈춤.
    /// </summary>
    public bool wait = false;

    [Header("Options")]
    /// <summary>
    /// true면 기존 회전 관련 트윈을 끊고 시작.
    /// </summary>
    public bool killTween = true;
}

public sealed class RotateFromToCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly RotateFromToCommandSpecCharR _spec;

    private RectTransform _rect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RotateFromToCommandCharR(RotateFromToCommandSpecCharR spec)
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
        
        if (_spec.overrideFromEuler)
        {
            SetLocalEuler(_rect, _spec.fromEuler);
        }

        if (_spec.duration <= 0f)
        {
            SetLocalEuler(_rect, _spec.toEuler);
            yield break;
        }

        
        Tween tween = _rect
            .DOLocalRotate(_spec.toEuler, _spec.duration, RotateMode.Fast)
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

        SetLocalEuler(_rect, _spec.toEuler);
        _rect = null;
    }
    
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
    
    
    private void SetLocalEuler(RectTransform rect, Vector3 euler)
    {
        rect.localEulerAngles = euler;
    }
}