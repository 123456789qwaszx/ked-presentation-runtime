using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;


[Serializable]
[CommandMenuHint("Char Rig Motion", "Bounce Arc In (Lite)", Order = -758)]
public sealed class BounceArcInLiteCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Direction")]
    public SlideFromCharR from = SlideFromCharR.Left;

    [Header("5 knobs")]
    [Tooltip("Total travel distance from offscreen-ish start to dest.")]
    public float distance = 580f;

    [Tooltip("Total time for the whole motion. <= 0 => snap.")]
    public float duration = 0.85f;

    [Min(1)]
    [Tooltip("How many hops while moving.")]
    public int hops = 3;

    [Tooltip("Arc height (jump diameter/energy).")]
    public float height = 24f;

    [Range(0f, 1f)]
    [Tooltip("0=normal last hop. 1=very low last hop + almost no horizontal travel.")]
    public float landing = 0.35f;

    [Header("Ease")]
    public Ease ease = Ease.OutCubic;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class BounceArcInLiteCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly BounceArcInLiteCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public BounceArcInLiteCommandCharR(BounceArcInLiteCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.from);
        Vector2 start = dest + fromDir * _spec.distance;

        int hops = Mathf.Max(1, _spec.hops);
        float total = _spec.duration;

        if (total <= 0f)
        {
            _rect.anchoredPosition = dest;
            yield break;
        }

        Vector2 travel = dest - start;
        Vector2 moveDir = travel.sqrMagnitude > 0f ? travel.normalized : (-fromDir);

        // ---- “폭/땅맛” 자동 보간 (파라미터 추가 없이) ----
        // height가 작으면 공중 구간이 짧아져 ‘톡’ + 땅맛, 크면 둥근 아치.
        float h = Mathf.Max(0f, _spec.height);
        float airWidth = AirWidthFromHeight(h); // 0.35..0.85

        // ---- 마지막 아치 노브(landing) 하나로 커스텀 ----
        float landing = Mathf.Clamp01(_spec.landing);

        float lastHeightScale = Mathf.Lerp(1f, 0.18f, landing);    // 1 -> 0.18
        float lastTravelFrac  = Mathf.Lerp(1f / hops, 0f, landing); // 기본 1/hops -> 0
        float lastAirWidth    = Mathf.Lerp(airWidth, 0.55f, landing);

        float totalDist = _spec.distance;

        float lastDist = totalDist * lastTravelFrac; // landing=1이면 0
        float mainDist = totalDist - lastDist;

        int mainHops = Mathf.Max(0, hops - 1);
        float mainSegLen = (mainHops > 0) ? (mainDist / mainHops) : 0f;

        _rect.anchoredPosition = start;

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    // t는 Linear로 돌리고, ease는 1번만 적용(맛 보존).
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    // hop index + local progress
                    float hf = e * hops;
                    int hopIndex = Mathf.Min((int)hf, hops - 1);
                    float u = hf - hopIndex; // 0..1

                    float segStart, segLen, arcH, aw;

                    if (hopIndex < mainHops)
                    {
                        segStart = hopIndex * mainSegLen;
                        segLen = mainSegLen;
                        arcH = h;
                        aw = airWidth;
                    }
                    else
                    {
                        segStart = mainDist;
                        segLen = lastDist;               // can be 0 => in-place last hop
                        arcH = h * lastHeightScale;      // low last arc if landing high
                        aw = lastAirWidth;
                    }

                    float x = HopAdvance(u, segLen, aw);
                    float y = HopHeight(u, arcH, aw); // y=0 on ground except airborne window

                    Vector2 pos = start + moveDir * (segStart + x) + Vector2.up * y;
                    _rect.anchoredPosition = pos;
                },
                1f,
                total
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

    private static Vector2 GetDir(SlideFromCharR from) => from switch
    {
        SlideFromCharR.Right => new Vector2(+1f, 0f),
        SlideFromCharR.Up    => new Vector2(0f, +1f),
        SlideFromCharR.Down  => new Vector2(0f, -1f),
        _                    => new Vector2(-1f, 0f),
    };

    // 0.35..0.85 (height 기반 자동 보간)
    private static float AirWidthFromHeight(float height)
    {
        // 0px -> 0.35, 80px -> ~0.8, 140px -> ~0.85
        float t = Mathf.Clamp01(height / 110f);
        t = t * t * (3f - 2f * t); // SmoothStep
        return Mathf.Lerp(0.35f, 0.85f, t);
    }

    // 공중(아치) 구간만 y를 올리고, 나머지는 지면(y=0) 유지.
    // airWidth가 작을수록 공중 시간이 짧아져 “단단한 땅 톡톡” 느낌이 강해짐.
    private static float HopHeight(float u, float height, float airWidth)
    {
        u = Mathf.Clamp01(u);
        if (height == 0f) return 0f;

        float preT = (1f - airWidth) * 0.5f;
        float airT = airWidth;

        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd || u > uAirEnd || airT <= 0f) return 0f;

        float a = (u - uPreEnd) / airT; // 0..1 in-air
        // 0 -> peak -> 0 (아치), 끝점에서 속도도 자연스럽게 0으로
        return Mathf.Sin(Mathf.PI * a) * height;
    }

    // x 진행도도 동일하게: ground(pre) -> air -> ground(post)로 나눠서
    // 공중이 좁으면 지면 구간이 늘어나 “땅을 딛는” 느낌이 생김.
    private static float HopAdvance(float u, float segLen, float airWidth)
    {
        u = Mathf.Clamp01(u);
        if (segLen == 0f) return 0f;

        float airLen = segLen * airWidth;
        float groundLen = segLen - airLen;
        float preLen = groundLen * 0.5f;
        float postLen = groundLen * 0.5f;

        float preT = (groundLen <= 0f) ? 0f : (preLen / segLen);
        float airT = airWidth;
        float postT = (groundLen <= 0f) ? 0f : (postLen / segLen);

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
}
