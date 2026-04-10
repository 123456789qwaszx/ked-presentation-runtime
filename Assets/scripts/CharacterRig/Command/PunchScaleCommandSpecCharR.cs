using System;
using UnityEngine;
using System.Collections;
using DG.Tweening;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Punch Scale",
    Order = 100)]
public class PunchScaleCommandSpecCharR : CommandSpecBase
{
    [Header("Target")] public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Scale;

    [Header("Punch")] [Tooltip("펀치 강도. 0.15 ~ 0.35 정도가 UI에서 예쁘게 보입니다.")]
    public float strength = 0.25f;

    [Header("Tween")] [Tooltip("펀치에 걸리는 시간(초). <= 0이면 실행하지 않습니다.")]
    public float duration = 0.22f;

    [Tooltip("진동 횟수 느낌. 6~10 정도가 자연스럽습니다.")]
    public int vibrato = 8;

    [Tooltip("탄성(0~1). 값이 클수록 더 튕기는 느낌입니다.")] [Range(0f, 1f)]
    public float elasticity = 0.75f;

    [Tooltip("체크하면 펀치가 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = false;
}


public sealed class PunchScaleCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly PunchScaleCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _originScale;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PunchScaleCommandCharR(PunchScaleCommandSpecCharR spec)
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

        _originScale = _rect.localScale;

        Tween tween = _rect
            .DOPunchScale(new Vector3(
                _spec.strength,
                _spec.strength, 0f),
                _spec.duration,
                _spec.vibrato,
                _spec.elasticity)
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

        _rect.localScale = _originScale;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _originScale = _rect.localScale;
    }
}