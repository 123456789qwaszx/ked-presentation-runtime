using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Dip InOut", Order = -735)]
public sealed class DipInOutCommandSpecCharR : CommandSpecBase
{
    [Header("Target")] public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Move")] public SlideFromCharR dir = SlideFromCharR.Down;

    [Tooltip("How far to dip (px).")] public float distance = 24f;

    [Tooltip("Total duration for enter + tiny hold + return. <=0 => snap.")]
    public float duration = 0.4f;

    [Tooltip("Base ease used as a hint. Enter will use an Out-ish ease, return will use an In-ish ease.")]
    public Ease ease = Ease.InCubic;

    [Header("Wait")] public bool wait = false;
}

public sealed class DipInOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly DipInOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _restPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public DipInOutCommandCharR(DipInOutCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        float total = _spec.duration;
        float dist = _spec.distance;

        if (total <= 0f || Mathf.Approximately(dist, 0f))
        {
            _rect.anchoredPosition = _restPos;
            yield break;
        }

        Vector2 rest = _restPos;
        Vector2 dipped = rest + GetOffset(_spec.dir, dist);

        float tEnter = total * 0.32f;
        float tHold = total * 0.24f;
        float tReturn = Mathf.Max(0.0001f, total - tEnter - tHold);

        float holdStart = tEnter;
        float returnStart = tEnter + tHold;

        Ease enterEase = ToOutEase(_spec.ease);
        Ease returnEase = ToInEase(_spec.ease);

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (_rect == null)
                        return;

                    if (t <= holdStart)
                    {
                        float localT = tEnter <= 0.0001f ? 1f : t / tEnter;
                        float e = DOVirtual.EasedValue(0f, 1f, localT, enterEase);
                        _rect.anchoredPosition = Vector2.LerpUnclamped(rest, dipped, e);
                        return;
                    }

                    if (t <= returnStart)
                    {
                        _rect.anchoredPosition = dipped;
                        return;
                    }

                    float localReturnT = tReturn <= 0.0001f ? 1f : (t - returnStart) / tReturn;
                    float eReturn = DOVirtual.EasedValue(0f, 1f, localReturnT, returnEase);
                    _rect.anchoredPosition = Vector2.LerpUnclamped(dipped, rest, eReturn);
                },
                total,
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
        _rect.anchoredPosition = _restPos;

        _rect = null;
        _restPos = default;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            return;

        _rect = rig.GetRect(_spec.target);
        _restPos = _rect.anchoredPosition;
    }

    private static Vector2 GetOffset(SlideFromCharR dir, float distance) => dir switch
    {
        SlideFromCharR.Right => new Vector2(+distance, 0f),
        SlideFromCharR.Up => new Vector2(0f, +distance),
        SlideFromCharR.Down => new Vector2(0f, -distance),
        _ => new Vector2(-distance, 0f),
    };

    private static Ease ToOutEase(Ease baseEase) => baseEase switch
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
        _ => baseEase, // 이미 Out 계열이거나 특수면 그대로
    };

    private static Ease ToInEase(Ease baseEase) => baseEase switch
    {
        Ease.OutQuad => Ease.InQuad,
        Ease.OutCubic => Ease.InCubic,
        Ease.OutQuart => Ease.InQuart,
        Ease.OutQuint => Ease.InQuint,
        Ease.OutSine => Ease.InSine,
        Ease.OutExpo => Ease.InExpo,
        Ease.OutCirc => Ease.InCirc,
        Ease.OutBack => Ease.InBack,
        Ease.InOutQuad => Ease.InQuad,
        Ease.InOutCubic => Ease.InCubic,
        Ease.InOutQuart => Ease.InQuart,
        Ease.InOutQuint => Ease.InQuint,
        Ease.InOutSine => Ease.InSine,
        Ease.InOutExpo => Ease.InExpo,
        Ease.InOutCirc => Ease.InCirc,
        Ease.InOutBack => Ease.InBack,
        _ => baseEase,
    };
}