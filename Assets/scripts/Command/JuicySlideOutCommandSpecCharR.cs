using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Juicy Slide Out", Order = -772)]
public sealed class JuicySlideOutCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public SlideFromCharR to = SlideFromCharR.Right;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InCubic;

    [Header("Juice (launch kick at the start)")]
    [Tooltip("0이면 심심한 SlideOut. 8~20 정도가 예쁘게 튐.")]
    public float punch = 14f;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class JuicySlideOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly JuicySlideOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public JuicySlideOutCommandCharR(JuicySlideOutCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 start = _startPos;
        Vector2 dir = GetDir(_spec.to);
        Vector2 end = start + dir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = end;
            yield break;
        }

        Vector2 slideDir = (end - start);
        slideDir = slideDir.sqrMagnitude > 0f ? slideDir.normalized : dir;

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 basePos = Vector2.LerpUnclamped(start, end, e);

                    // SlideOut은 초반에 “발사 킥”이 있으면 맛이 남:
                    // (1-e)^2로 초반만 살리고, sin(πe)로 0..1..0 유지.
                    float bump = JuicyBump_Start(e);

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

        // Out은 end로 고정(원하면 제거 가능)
        _rect.anchoredPosition = _startPos + GetDir(_spec.to) * _spec.distance;

        _rect = null;
        _startPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _startPos = _rect.anchoredPosition;
    }

    private static Vector2 GetDir(SlideFromCharR from) => from switch
    {
        SlideFromCharR.Right => new Vector2(+1f, 0f),
        SlideFromCharR.Up    => new Vector2(0f, +1f),
        SlideFromCharR.Down  => new Vector2(0f, -1f),
        _                    => new Vector2(-1f, 0f),
    };

    private static float JuicyBump_Start(float e)
    {
        e = Mathf.Clamp01(e);
        float oneMinus = 1f - e;
        return Mathf.Sin(Mathf.PI * e) * (oneMinus * oneMinus);
    }
}
