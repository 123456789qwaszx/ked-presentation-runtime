using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Punch Scale",
    Order = 100)]
public class PunchScaleCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Scale;

    [Header("Punch")]
    [Tooltip("펀치 강도. 0.15 ~ 0.35 정도가 UI에서 예쁘게 보입니다.")]
    public float strength = 0.25f;

    [Header("Tween")]
    [Tooltip("펀치에 걸리는 시간(초). <= 0이면 실행하지 않습니다.")]
    public float duration = 0.22f;

    [Tooltip("진동 횟수 느낌. 6~10 정도가 자연스럽습니다.")]
    public int vibrato = 8;

    [Tooltip("탄성(0~1). 값이 클수록 더 튕기는 느낌입니다.")]
    [Range(0f, 1f)]
    public float elasticity = 0.75f;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class PunchScaleCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly PunchScaleCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector3 _originScale;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PunchScaleCommandCharR(PunchScaleCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;
        _originScale = _rect.localScale;

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            _rect.localScale = _originScale;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector3 baseScale = _originScale;
        int vibrato = Mathf.Max(1, _spec.vibrato);
        float elasticity = Mathf.Clamp01(_spec.elasticity);
        float strength = _spec.strength;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float u = Mathf.Clamp01(t);

                    float punch = EvaluatePunch(u, vibrato, elasticity);
                    float scaleOffset = strength * punch;

                    Vector3 s = baseScale;
                    s.x = baseScale.x + scaleOffset;
                    s.y = baseScale.y + scaleOffset;
                    _rect.localScale = s;
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.localScale = _originScale;
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.localScale = _originScale;
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.localScale = _originScale;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _rect = rig.GetRect(_spec.target);
        if (_rect != null)
            _originScale = _rect.localScale;
    }

    private static float EvaluatePunch(float u, int vibrato, float elasticity)
    {
        u = Mathf.Clamp01(u);
        vibrato = Mathf.Max(1, vibrato);
        elasticity = Mathf.Clamp01(elasticity);

        float decayPower = Mathf.Lerp(5.5f, 2.2f, elasticity);
        float envelope = Mathf.Pow(1f - u, decayPower);

        float wave = Mathf.Sin(u * Mathf.PI * (vibrato + 0.5f));

        float attack = 1f - Mathf.Pow(1f - u, 2.2f);

        return wave * envelope * attack;
    }
}