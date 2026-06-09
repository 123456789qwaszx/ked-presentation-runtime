using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Hop", Order = -760)]
public sealed class HopCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track_Y;

    [Header("Timing")]
    public float duration = 0.95f;
    public Ease ease = Ease.OutCubic;

    [Header("Hop")]
    [Min(1)]
    public int hopCount = 1;

    [Tooltip("Arc height in pixels. Positive value hops upward.")]
    public float height = 48f;

    [Range(0.05f, 1f)]
    [Tooltip("How much of each hop segment is airborne. 1=arc spans whole segment, 0.2=short/narrow arc.")]
    public float airWidth = 0.85f;

    [Header("Last hop override (optional)")]
    [Tooltip("If < 0, uses arcHeight.")]
    public float lastArcHeight = -1f;

    [Range(0.05f, 1f)]
    [Tooltip("If < 0, uses airWidth.")]
    public float lastAirWidth = -1f;
}

public sealed class HopCommandCharR : CommandBase
{
    private readonly HopCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _basePos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public HopCommandCharR(HopCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        int hops = Mathf.Max(1, _spec.hopCount);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        float mainH = _spec.height;
        float mainAirW = Mathf.Clamp(_spec.airWidth, 0.05f, 1f);

        float lastH = _spec.lastArcHeight >= 0f ? _spec.lastArcHeight : mainH;
        float lastAirW = _spec.lastAirWidth >= 0f
            ? Mathf.Clamp(_spec.lastAirWidth, 0.05f, 1f)
            : mainAirW;

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    float hf = e * hops;
                    int hopIndex = Mathf.Min((int)hf, hops - 1);
                    float u = hf - hopIndex;

                    bool isLastHop = hopIndex == hops - 1;

                    float height = isLastHop ? lastH : mainH;
                    float airW = isLastHop ? lastAirW : mainAirW;

                    float y = HopHeight(u, height, airW);

                    _rect.anchoredPosition = _basePos + Vector2.up * y;
                },
                1f,
                _spec.duration)
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
        _basePos = _rect.anchoredPosition;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _basePos;

        HasClaimedTarget = false;
    }

    private static float HopHeight(float u, float height, float airW)
    {
        u = Mathf.Clamp01(u);

        if (height == 0f)
            return 0f;

        airW = Mathf.Clamp(airW, 0.05f, 1f);

        float preT = (1f - airW) * 0.5f;
        float airT = airW;

        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd || u > uAirEnd || airT <= 0f)
            return 0f;

        float a = (u - uPreEnd) / airT;
        return Mathf.Sin(Mathf.PI * a) * height;
    }
}