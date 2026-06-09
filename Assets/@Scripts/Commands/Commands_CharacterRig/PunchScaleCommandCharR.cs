using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Punch Scale",
    Order = 100)]
public class PunchScaleCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_ActingScale;

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
}

public sealed class PunchScaleCommandCharR : CommandBase
{
    private readonly PunchScaleCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _baseScale;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public PunchScaleCommandCharR(PunchScaleCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        int vibrato = Mathf.Max(1, _spec.vibrato);
        float elasticity = Mathf.Clamp01(_spec.elasticity);
        float strength = _spec.strength;

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t);

                    float punch = EvaluatePunch(u, vibrato, elasticity);
                    float scaleOffset = strength * punch;

                    Vector3 scale = _baseScale;
                    scale.x = _baseScale.x + scaleOffset;
                    scale.y = _baseScale.y + scaleOffset;

                    _rect.localScale = scale;
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        _baseScale = _rect.localScale;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.localScale = _baseScale;

        HasClaimedTarget = false;
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