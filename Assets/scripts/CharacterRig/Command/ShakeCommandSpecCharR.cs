using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum ShakeCommandSpecCharRShakeAxis
{
    X  = 0,
    Y  = 1,
    XY = 2,
}

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Shake",
    Order = 100
    )]
public class ShakeCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    [Tooltip("어느 초상화 세트를 흔들지 선택합니다.")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Shake;

    [Header("Shake Axis")]
    public ShakeCommandSpecCharRShakeAxis axis = ShakeCommandSpecCharRShakeAxis.X;

    [Header("Strength")]
    [Tooltip("흔들림 강도(픽셀). 8~30 정도가 UI에서 효과적입니다.")]
    public float intensity = 28f;

    [Header("Tween")]
    [Tooltip("흔들리는 시간(초). <= 0이면 실행하지 않습니다.")]
    public float duration = 0.55f;

    [Tooltip("초당 진동 횟수 느낌. 10~20 정도가 자연스럽습니다.")]
    public int vibrato = 15;

    [Tooltip("각도/방향 랜덤성(0~90). 값이 클수록 방향이 더 흩어집니다.")]
    [Range(0f, 90f)]
    public float randomness = 10f;
    
    [Tooltip("체크시 진폭이 초반에 강하게 몰립니다.")]
    public bool shakeFadeout = true;

    [Tooltip("체크하면 흔들림이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;
}


public class ShakeCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ShakeCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _originPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;
    
    
    public ShakeCommandCharR(ShakeCommandSpecCharR spec)
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
        
        _originPos = _rect.anchoredPosition;
        
        Vector2 strength = GetStrength(_spec.axis, _spec.intensity);

        Tween tween = _rect
            .DOShakeAnchorPos(_spec.duration, strength, _spec.vibrato, _spec.randomness, snapping: true, fadeOut: _spec.shakeFadeout)
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

        _rect.anchoredPosition = _originPos;
        _rect = null;
    }

    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
    }
    
    private static Vector2 GetStrength(ShakeCommandSpecCharRShakeAxis axis, float intensity)
    {
        return axis switch
        {
            ShakeCommandSpecCharRShakeAxis.Y  => new Vector2(0f, intensity),
            ShakeCommandSpecCharRShakeAxis.XY => new Vector2(intensity, intensity),
            _ => new Vector2(intensity, 0f),
        };
    }
}