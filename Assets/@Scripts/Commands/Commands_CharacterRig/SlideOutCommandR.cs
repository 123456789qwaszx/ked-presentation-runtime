using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Slide Out", Order = -772)]
public sealed class SlideOutCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track;

    [Header("Slide")]
    public CharRigDirection to = CharRigDirection.Right;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InCubic;

    [Header("Juice (launch kick at the start)")]
    [Tooltip("0이면 심심한 SlideOut. 8~20 정도가 예쁘게 튐.")]
    public float punch = 14f;
}

public sealed class SlideOutCommandCharR : CommandBase
{
    private readonly SlideOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private Vector2 _endPos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlideOutCommandCharR(SlideOutCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Vector2 start = _startPos;
        Vector2 end = _endPos;

        Vector2 slideDir = end - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : GetDir(_spec.to);

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 basePos = Vector2.LerpUnclamped(start, end, e);

                    float bump = JuicyBump_Start(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
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

        _startPos = _rect.anchoredPosition;
        _endPos = _startPos + GetDir(_spec.to) * _spec.distance;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _endPos;

        HasClaimedTarget = false;
    }

    private static Vector2 GetDir(CharRigDirection from) => from switch
    {
        CharRigDirection.Right => new Vector2(+1f, 0f),
        CharRigDirection.Up => new Vector2(0f, +1f),
        CharRigDirection.Down => new Vector2(0f, -1f),
        _ => new Vector2(-1f, 0f),
    };

    private static float JuicyBump_Start(float e)
    {
        e = Mathf.Clamp01(e);
        float oneMinus = 1f - e;
        return Mathf.Sin(Mathf.PI * e) * (oneMinus * oneMinus);
    }
}