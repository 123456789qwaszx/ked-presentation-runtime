using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Dip InOut", Order = -735)]
public sealed class DipInOutCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track_Y;

    [Header("Move")]
    public CharRigDirection dir = CharRigDirection.Down;

    [Tooltip("How far to dip (px).")]
    public float distance = 24f;

    [Tooltip("Total duration for enter + tiny hold + return. <=0 => snap.")]
    public float duration = 0.4f;

    [Tooltip("Base ease used as a hint. Enter will use an Out-ish ease, return will use an In-ish ease.")]
    public Ease ease = Ease.InCubic;
}

public sealed class DipInOutCommandCharR : CommandBase
{
    private readonly DipInOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _basePos;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public DipInOutCommandCharR(DipInOutCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Debug.Log("DipInOutCommandSpecCharR ExecuteInner");
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        float total = _spec.duration;
        float dist = _spec.distance;

        if (total <= 0f || Mathf.Approximately(dist, 0f))
        {
            CommitFinalState();
            yield break;
        }

        Vector2 basePos = _basePos;
        Vector2 dipped = basePos + GetOffset(_spec.dir, dist);

        float tEnter = total * 0.32f;
        float tHold = total * 0.24f;
        float tReturn = Mathf.Max(0.0001f, total - tEnter - tHold);

        float holdStart = tEnter;
        float returnStart = tEnter + tHold;

        Ease enterEase = ToEase(_spec.ease);
        Ease returnEase = ToEase(_spec.ease);

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (t <= holdStart)
                    {
                        float localT = tEnter <= 0.0001f ? 1f : t / tEnter;
                        float e = DOVirtual.EasedValue(0f, 1f, localT, enterEase);
                        _rect.anchoredPosition = Vector2.LerpUnclamped(basePos, dipped, e);
                        return;
                    }

                    if (t <= returnStart)
                    {
                        _rect.anchoredPosition = dipped;
                        return;
                    }

                    float localReturnT = tReturn <= 0.0001f ? 1f : (t - returnStart) / tReturn;
                    float eReturn = DOVirtual.EasedValue(0f, 1f, localReturnT, returnEase);
                    _rect.anchoredPosition = Vector2.LerpUnclamped(dipped, basePos, eReturn);
                },
                total,
                total
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
        Debug.Log("DipInOutCommandSpecCharR Skip");
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

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

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

    private static Vector2 GetOffset(CharRigDirection dir, float distance) => dir switch
    {
        CharRigDirection.Right => new Vector2(+distance, 0f),
        CharRigDirection.Up => new Vector2(0f, +distance),
        CharRigDirection.Down => new Vector2(0f, -distance),
        _ => new Vector2(-distance, 0f),
    };

    private static Ease ToEase(Ease baseEase) => baseEase switch
    {
        Ease.InQuad => Ease.OutQuad,
        Ease.InCubic => Ease.OutCubic,
        Ease.InQuart => Ease.OutQuart,
        Ease.InQuint => Ease.OutQuint,
        Ease.InSine => Ease.OutSine,
        Ease.InExpo => Ease.OutExpo,
        Ease.InCirc => Ease.OutCirc,
        Ease.InBack => Ease.OutBack,
        Ease.InOutQuad => Ease.OutQuad,
        Ease.InOutCubic => Ease.OutCubic,
        Ease.InOutQuart => Ease.OutQuart,
        Ease.InOutQuint => Ease.OutQuint,
        Ease.InOutSine => Ease.OutSine,
        Ease.InOutExpo => Ease.OutExpo,
        Ease.InOutCirc => Ease.OutCirc,
        Ease.InOutBack => Ease.OutBack,
        _ => baseEase,
    };

}