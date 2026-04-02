using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Move InOut (Normal)", Order = -734)]
public sealed class MoveInOutCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Move")]
    [Tooltip("Offset from rest position. (e.g., (24,0)=right, (0,24)=up)")]
    public Vector2 offset = new Vector2(24f, 0f);

    [Header("Timing")]
    [Tooltip("Total duration: out + hold + back. <= 0 => snap back to rest.")]
    public float duration = 0.22f;

    [Range(0f, 0.5f)]
    [Tooltip("Optional hold portion of total duration. 0 = no hold.")]
    public float holdPortion = 0.08f;

    [Header("Ease (single knob)")]
    [Tooltip("Base ease. Internally converted: Out for 'go', In for 'return'.")]
    public Ease ease = Ease.InOutCubic;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class MoveInOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly MoveInOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _restPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public MoveInOutCommandCharR(MoveInOutCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);

        float total = _spec.duration;
        if (total <= 0f)
        {
            _rect.anchoredPosition = _restPos;
            yield break;
        }

        Vector2 rest = _restPos;
        Vector2 target = rest + _spec.offset;

        float holdP = Mathf.Clamp(_spec.holdPortion, 0f, 0.5f);
        float tHold = total * holdP;

        float tMoveTotal = Mathf.Max(0.0001f, total - tHold);
        float tOut = tMoveTotal * 0.5f;
        float tBack = tMoveTotal - tOut;

        float holdStart = tOut;
        float backStart = tOut + tHold;

        Ease outEase = ToOutEase(_spec.ease);
        Ease inEase = ToInEase(_spec.ease);

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (_rect == null)
                        return;

                    if (t <= holdStart)
                    {
                        float localT = tOut <= 0.0001f ? 1f : t / tOut;
                        float eOut = DOVirtual.EasedValue(0f, 1f, localT, outEase);
                        _rect.anchoredPosition = Vector2.LerpUnclamped(rest, target, eOut);
                        return;
                    }

                    if (t <= backStart)
                    {
                        _rect.anchoredPosition = target;
                        return;
                    }

                    float localBackT = tBack <= 0.0001f ? 1f : (t - backStart) / tBack;
                    float eIn = DOVirtual.EasedValue(0f, 1f, localBackT, inEase);
                    _rect.anchoredPosition = Vector2.LerpUnclamped(target, rest, eIn);
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

    // --- Ease mapping: single knob -> out/in pair ---
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

        _ => baseEase,
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
