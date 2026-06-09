using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Slide In", Order = -771)]
public sealed class SlideInCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track;

    [Header("Slide")]
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("(overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;
}

public sealed class SlideInCommandCharR : CommandBase
{
    private readonly SlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public SlideInCommandCharR(SlideInCommandSpecCharR spec)
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

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.direction);
        Vector2 start = dest + fromDir * _spec.distance;

        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : -fromDir;

        _rect.anchoredPosition = start;

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = JuicyBump_End(e);
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
        _destPos = _rect.anchoredPosition;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.anchoredPosition = _destPos;

        HasClaimedTarget = false;
    }

    private static Vector2 GetDir(CharRigDirection from) => from switch
    {
        CharRigDirection.Right => new Vector2(+1f, 0f),
        CharRigDirection.Up => new Vector2(0f, +1f),
        CharRigDirection.Down => new Vector2(0f, -1f),
        _ => new Vector2(-1f, 0f),
    };

    private static float JuicyBump_End(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}