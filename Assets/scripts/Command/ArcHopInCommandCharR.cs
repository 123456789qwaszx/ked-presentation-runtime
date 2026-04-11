using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "ArcHop In", Order = -760)]
public sealed class ArcHopInCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide In")]
    public CharRDirection from = CharRDirection.Left;
    public float distance = 480f;

    [Header("Timing")]
    public float duration = 0.85f;
    public Ease ease = Ease.OutCubic;

    [Header("Hop (main arcs)")]
    [Min(1)]
    public int hopCount = 3;

    [Tooltip("Arc height in pixels (how high it jumps).")]
    public float arcHeight = 40f;

    [Range(0.05f, 1f)]
    [Tooltip("How much of each hop segment is airborne (arc width). 1=arc spans whole segment, 0.2=short/narrow arc.")]
    public float airWidth = 0.75f;

    [Header("Last arc override (optional)")]
    [Tooltip("If < 0, uses arcHeight.")]
    public float lastArcHeight = -1f;

    [Range(0.05f, 1f)]
    [Tooltip("If < 0, uses airWidth.")]
    public float lastAirWidth = -1f;

    [Range(0f, 1f)]
    [Tooltip("How much of the total horizontal travel is reserved for the last hop. 0 = last hop in-place.")]
    public float lastTravelFraction = 0.15f;

    [Header("Wait")]
    public bool wait = false;
    
    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class ArcHopInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ArcHopInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ArcHopInCommandCharR(ArcHopInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        
        _canCommitFinalState = true;

        Vector2 dest = _destPos;
        Vector2 fromDir = GetFromDir(_spec.from);
        Vector2 start = dest + fromDir * _spec.distance;

        if (_spec.duration <= 0f || _spec.hopCount <= 0)
        {
            _rect.anchoredPosition = dest;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector2 moveDir = dest - start;
        moveDir = moveDir.sqrMagnitude > 0f
            ? moveDir.normalized
            : -fromDir;

        Vector2 jumpDir = Vector2.up;

        int hops = Mathf.Max(1, _spec.hopCount);

        float totalDist = _spec.distance;
        float lastFrac = Mathf.Clamp01(_spec.lastTravelFraction);
        float lastDist = totalDist * lastFrac;
        float mainDist = totalDist - lastDist;

        int mainHops = Mathf.Max(0, hops - 1);
        float mainSegLen = mainHops > 0 ? mainDist / mainHops : 0f;

        float mainH = _spec.arcHeight;
        float mainAirW = Mathf.Clamp(_spec.airWidth, 0.05f, 1f);

        float lastH = _spec.lastArcHeight >= 0f ? _spec.lastArcHeight : mainH;
        float lastAirW = _spec.lastAirWidth >= 0f
            ? Mathf.Clamp(_spec.lastAirWidth, 0.05f, 1f)
            : mainAirW;

        _rect.anchoredPosition = start;

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

                    float segStart;
                    float segLen;
                    float height;
                    float airW;

                    if (hopIndex < mainHops)
                    {
                        segStart = hopIndex * mainSegLen;
                        segLen = mainSegLen;
                        height = mainH;
                        airW = mainAirW;
                    }
                    else
                    {
                        segStart = mainDist;
                        segLen = lastDist;
                        height = lastH;
                        airW = lastAirW;
                    }

                    float xInSeg = HopAdvance(u, segLen, airW);
                    float y = HopHeight(u, height, airW);

                    Vector2 pos = start
                                  + moveDir * (segStart + xInSeg)
                                  + jumpDir * y;

                    _rect.anchoredPosition = pos;
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

                _rect.anchoredPosition = dest;
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);
        
        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _destPos;

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
        _destPos = _rect.anchoredPosition;
    }

    private static Vector2 GetFromDir(CharRDirection from) => from switch
    {
        CharRDirection.Right => new Vector2(+1f, 0f),
        CharRDirection.Up => new Vector2(0f, +1f),
        CharRDirection.Down => new Vector2(0f, -1f),
        _ => new Vector2(-1f, 0f),
    };

    private static float HopAdvance(float u, float segLen, float airW)
    {
        u = Mathf.Clamp01(u);
        if (segLen == 0f)
            return 0f;

        float airLen = segLen * airW;
        float groundLen = segLen - airLen;
        float preLen = groundLen * 0.5f;
        float postLen = groundLen * 0.5f;

        float preT = groundLen <= 0f ? 0f : preLen / segLen;
        float airT = airW;
        float postT = groundLen <= 0f ? 0f : postLen / segLen;

        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd && uPreEnd > 0f)
        {
            float k = u / uPreEnd;
            return Mathf.Lerp(0f, preLen, k);
        }

        if (u < uAirEnd && airT > 0f)
        {
            float k = (u - uPreEnd) / airT;
            return preLen + airLen * k;
        }

        if (postT > 0f)
        {
            float k = (u - uAirEnd) / postT;
            return preLen + airLen + postLen * Mathf.Clamp01(k);
        }

        return segLen;
    }

    private static float HopHeight(float u, float height, float airW)
    {
        u = Mathf.Clamp01(u);
        if (height == 0f)
            return 0f;

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