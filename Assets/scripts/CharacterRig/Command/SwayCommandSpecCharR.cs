using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Sway", 
    Order = 100)]
public class SwayCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Targets")]
    [Tooltip("좌우로 흔들릴 피벗(SwayPivot).")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_SwayPivot;
    
    [Header("Sway (Fan-like)")]
    [Tooltip("아랫변 중앙을 축으로 좌우로 흔들리는 각도 (절대값 기준). 10~25 추천.")]
    public float swayAngle = 5f;

    public int swayLoops = 2;

    public float duration = 0.55f;
    
    
    [Header("Sway Easing")]
    [Tooltip("첫 방향으로 갈 때 이징.")]
    public Ease swayForwardEase = Ease.OutQuad;

    [Tooltip("true면 시간이 지날수록 진폭이 줄어듦.")]
    public bool swayDecay = false;
    
    [Tooltip("체크하면 연출이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;
}
public sealed class SwayCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SwayCommandSpecCharR _spec;

    private RectTransform _rect;
    private float _originSwayRotationZ;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SwayCommandCharR(SwayCommandSpecCharR spec)
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

        if (_spec.duration <= 0f || _spec.swayAngle <= 0f || _spec.swayLoops <= 0)
        {
            SetLocalEulerZ(_rect, _originSwayRotationZ);
            _rect = null;
            yield break;
        }

        DOVirtual.Float(
                0f,
                1f,
                _spec.duration,
                ts =>
                {
                    float phase = ts * Mathf.PI * 2f * _spec.swayLoops;
                    float raw = Mathf.Sin(phase);

                    float envelope = 1f;
                    if (_spec.swayDecay)
                    {
                        float eased = DOVirtual.EasedValue(0f, 1f, ts, _spec.swayForwardEase);
                        envelope = 1f - eased;
                    }

                    float swayAngle = raw * _spec.swayAngle * envelope;
                    SetLocalEulerZ(_rect, _originSwayRotationZ + swayAngle);
                }
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        
       
        if (_spec.wait)
        {
            Tween waitTween = DOVirtual.DelayedCall(_spec.duration, () => { }, ignoreTimeScale: true)
                .SetUpdate(true);
                
            yield return waitTween.WaitForCompletion();
        }
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

        SetLocalEulerZ(_rect, _originSwayRotationZ);
        _rect = null;
    }
    

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _originSwayRotationZ = _rect.localEulerAngles.z;
    }
    
    
    private void SetLocalEulerZ(RectTransform rect, float z)
    {
        if (rect == null)
            return;

        Vector3 e = rect.localEulerAngles;
        e.z = z;
        rect.localEulerAngles = e;
    }
}