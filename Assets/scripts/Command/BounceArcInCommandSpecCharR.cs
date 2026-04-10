using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

public enum BounceJumpAxisCharR { X = 0, Y = 1 }

[Serializable]
[CommandMenuHint("Char Rig Motion", "Bounce Arc In", Order = -760)]
public sealed class BounceArcInCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide In")]
    public SlideFromCharR from = SlideFromCharR.Left;
    public float distance = 480f;

    [Header("Timing")]
    public float duration = 0.85f;
    public Ease ease = Ease.OutCubic;

    [Header("Hop (main arcs)")]
    [Min(1)] public int hopCount = 3;

    [Tooltip("Arc height in pixels (how high it jumps).")]
    public float arcHeight = 40f;

    [Range(0.05f, 1f)]
    [Tooltip("How much of each hop segment is airborne (arc width). 1=arc spans whole segment, 0.2=short/narrow arc.")]
    public float airWidth = 0.75f;

    [Header("Jump axis")]
    public BounceJumpAxisCharR jumpAxis = BounceJumpAxisCharR.Y;

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
}

public sealed class BounceArcInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly BounceArcInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public BounceArcInCommandCharR(BounceArcInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 dest = _destPos;
        Vector2 fromDir = GetFromDir(_spec.from);
        Vector2 start = dest + fromDir * _spec.distance; // slide-in start

        if (_spec.duration <= 0f || _spec.hopCount <= 0)
        {
            _rect.anchoredPosition = dest;
            yield break;
        }

        Vector2 moveDir = (dest - start);
        moveDir = moveDir.sqrMagnitude > 0f ? moveDir.normalized : (-fromDir);

        Vector2 jumpDir = _spec.jumpAxis == BounceJumpAxisCharR.Y ? Vector2.up : Vector2.right;

        int hops = Mathf.Max(1, _spec.hopCount);

        // Last hop gets its own distance slice
        float totalDist = _spec.distance;
        float lastFrac = Mathf.Clamp01(_spec.lastTravelFraction);
        float lastDist = totalDist * lastFrac;
        float mainDist = totalDist - lastDist;

        int mainHops = Mathf.Max(0, hops - 1);
        float mainSegLen = mainHops > 0 ? (mainDist / mainHops) : 0f;

        float mainH = _spec.arcHeight;
        float mainAirW = Mathf.Clamp(_spec.airWidth, 0.05f, 1f);

        float lastH = (_spec.lastArcHeight >= 0f) ? _spec.lastArcHeight : mainH;
        float lastAirW = (_spec.lastAirWidth >= 0f) ? Mathf.Clamp(_spec.lastAirWidth, 0.05f, 1f) : mainAirW;

        _rect.anchoredPosition = start;

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    // Keep tween time linear; apply ease exactly once.
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    // Map progress into hop index + local hop progress u
                    float hf = e * hops;
                    int hopIndex = Mathf.Min((int)hf, hops - 1);
                    float u = hf - hopIndex; // 0..1 (last hop ends at u=1)

                    // Segment parameters for this hop
                    float segStart, segLen, height, airW;

                    if (hopIndex < mainHops)
                    {
                        segStart = hopIndex * mainSegLen;
                        segLen = mainSegLen;
                        height = mainH;
                        airW = mainAirW;
                    }
                    else
                    {
                        segStart = mainDist; // last hop begins after main distance
                        segLen = lastDist;   // can be 0 => in-place last hop
                        height = lastH;
                        airW = lastAirW;
                    }

                    // Compute within-segment advance and arc height
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
            .SetUpdate(true);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) return;

        _rect.DOKill();
        _rect.anchoredPosition = _destPos;

        _rect = null;
        _destPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _destPos = _rect.anchoredPosition;
    }

    private static Vector2 GetFromDir(SlideFromCharR from) => from switch
    {
        SlideFromCharR.Right => new Vector2(+1f, 0f),
        SlideFromCharR.Up    => new Vector2(0f, +1f),
        SlideFromCharR.Down  => new Vector2(0f, -1f),
        _                    => new Vector2(-1f, 0f), // Left
    };

    // --- Hop math ---
    // We split a hop into: ground (pre) -> airborne arc -> ground (post)
    // airW controls how wide the airborne part is relative to the segment.
    private static float HopAdvance(float u, float segLen, float airW)
    {
        u = Mathf.Clamp01(u);
        if (segLen == 0f) return 0f;

        float airLen = segLen * airW;
        float groundLen = segLen - airLen;
        float preLen = groundLen * 0.5f;
        float postLen = groundLen * 0.5f;

        float preT = (groundLen <= 0f) ? 0f : (preLen / segLen);
        float airT = airW;
        float postT = (groundLen <= 0f) ? 0f : (postLen / segLen);

        // Normalize thresholds in u-space
        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd && uPreEnd > 0f)
        {
            float k = u / uPreEnd;
            return Mathf.Lerp(0f, preLen, k);
        }

        if (u < uAirEnd && airT > 0f)
        {
            float k = (u - uPreEnd) / airT; // 0..1 in-air
            return preLen + (airLen * k);
        }

        // post ground
        if (postT > 0f)
        {
            float k = (u - uAirEnd) / postT;
            return preLen + airLen + (postLen * Mathf.Clamp01(k));
        }

        return segLen;
    }

    private static float HopHeight(float u, float height, float airW)
    {
        u = Mathf.Clamp01(u);
        if (height == 0f) return 0f;

        float preT = (1f - airW) * 0.5f;
        float airT = airW;

        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd || u > uAirEnd || airT <= 0f)
            return 0f;

        float a = (u - uPreEnd) / airT; // 0..1 in-air
        // Smooth “arch”: 0 -> peak -> 0, with zero velocity at endpoints.
        return Mathf.Sin(Mathf.PI * a) * height;
    }
}
