using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Hop", Order = -760)]
public sealed class ArcHopInCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track_Y;

    [Header("Timing")]
    public float duration = 0.95f;
    public Ease ease = Ease.OutCubic;

    [Header("Hop")]
    [Min(1)]
    public int hopCount = 1;

    [Tooltip("Arc height in pixels. Positive value hops upward.")]
    public float arcHeight = 48f;

    [Range(0.05f, 1f)]
    [Tooltip("How much of each hop segment is airborne. 1=arc spans whole segment, 0.2=short/narrow arc.")]
    public float airWidth = 0.85f;

    [Header("Last hop override (optional)")]
    [Tooltip("If < 0, uses arcHeight.")]
    public float lastArcHeight = -1f;

    [Range(0.05f, 1f)]
    [Tooltip("If < 0, uses airWidth.")]
    public float lastAirWidth = -1f;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ArcHopInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ArcHopInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _basePos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ArcHopInCommandCharR(ArcHopInCommandSpecCharR spec)
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
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _basePos = _rect.anchoredPosition;
        _canCommitFinalState = true;

        int hops = Mathf.Max(1, _spec.hopCount);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        float mainH = _spec.arcHeight;
        float mainAirW = Mathf.Clamp(_spec.airWidth, 0.05f, 1f);

        float lastH = _spec.lastArcHeight >= 0f ? _spec.lastArcHeight : mainH;
        float lastAirW = _spec.lastAirWidth >= 0f
            ? Mathf.Clamp(_spec.lastAirWidth, 0.05f, 1f)
            : mainAirW;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

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
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                CommitFinalState();
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

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);

        _rect = rigRefs.GetRect(_spec.target);

        if (_rect != null)
            _basePos = _rect.anchoredPosition;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _basePos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
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