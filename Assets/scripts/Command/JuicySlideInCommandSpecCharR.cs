using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Juicy Slide In", Order = -771)]
public sealed class JuicySlideInCommandSpecCharR : CommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public SlideFromCharR direction = SlideFromCharR.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("Juice (overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;

    [Header("Wait")]
    public bool wait = false;
}


public sealed class JuicySlideInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly JuicySlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public JuicySlideInCommandCharR(JuicySlideInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.direction);
        Vector2 start = dest + fromDir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = dest;
            yield break;
        }

        Vector2 slideDir = (dest - start);
        slideDir = slideDir.sqrMagnitude > 0f ? slideDir.normalized : (-fromDir);

        _rect.anchoredPosition = start;

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    // t는 Linear, ease는 딱 1번만 적용
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);

                    // 후반에만 살짝 “지나쳤다가” 끝에서 0으로 돌아오는 bump
                    float bump = JuicyBump_End(e); // 0..1..0

                    Vector2 offset = slideDir * (_spec.punch * bump);
                    _rect.anchoredPosition = basePos + offset;
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

    private static Vector2 GetDir(SlideFromCharR from) => from switch
    {
        SlideFromCharR.Right => new Vector2(+1f, 0f),
        SlideFromCharR.Up    => new Vector2(0f, +1f),
        SlideFromCharR.Down  => new Vector2(0f, -1f),
        _                    => new Vector2(-1f, 0f),
    };

    // “도착 직전”에만 맛이 나고, 끝에서 0으로 정착하는 bump
    // sin(πe)는 0..1..0, e^2가 초반을 눌러서 peak를 후반으로 당김.
    private static float JuicyBump_End(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}
