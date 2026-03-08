using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

/// <summary>
/// �����ִ¡� �����̵� ��: (����) Anticipation -> (����) Overshoot -> Settle
/// Bounce(���̺�) ����.
/// </summary>
[Serializable]
[CommandMenuHint("Char Rig Motion", "Rich Slide In", Order = -768)]
public sealed class RichSlideInCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Direction")]
    public SlideFromCharR from = SlideFromCharR.Left;

    [Header("Slide")]
    [Tooltip("Start position = dest + offset(from, distance).")]
    public float distance = 480f;

    [Header("Anticipation (tiny pull-back before entering)")]
    public bool useAnticipation = true;
    [Tooltip("How much to pull opposite direction, in pixels.")]
    public float anticipationDistance = 18f;
    [Range(0f, 1f)]
    public float anticipationPortion = 0.12f;
    public Ease anticipationEase = Ease.OutQuad;

    [Header("Overshoot (go past dest, then settle)")]
    public bool useOvershoot = true;
    [Tooltip("How much to go past dest along slide direction, in pixels.")]
    public float overshootDistance = 24f;
    [Range(0f, 1f)]
    public float overshootPortion = 0.78f;
    public Ease approachEase = Ease.OutCubic;
    public Ease settleEase = Ease.OutQuart;

    [Header("Timing")]
    [Tooltip("Total duration. <= 0 => snap to dest.")]
    public float duration = 0.55f;

    [Header("Wait")]
    public bool wait = false;
}

/// <summary>
/// �����ִ¡� �����̵� �ƿ�: (����) Anticipation -> Launch (end offscreen-ish)
/// Bounce(���̺�) ����.
/// </summary>
[Serializable]
[CommandMenuHint("Char Rig Motion", "Rich Slide Out", Order = -769)]
public sealed class RichSlideOutCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Direction")]
    [Tooltip("Slide out toward this direction (the direction it exits).")]
    public SlideFromCharR to = SlideFromCharR.Left;

    [Header("Slide")]
    [Tooltip("End position = start + offset(to, distance).")]
    public float distance = 480f;

    [Header("Anticipation (tiny pull opposite before exiting)")]
    public bool useAnticipation = true;
    [Tooltip("How much to pull opposite direction, in pixels.")]
    public float anticipationDistance = 14f;
    [Range(0f, 1f)]
    public float anticipationPortion = 0.12f;
    public Ease anticipationEase = Ease.OutQuad;

    [Header("Launch")]
    public Ease launchEase = Ease.InCubic;

    [Header("Timing")]
    [Tooltip("Total duration. <= 0 => snap to end.")]
    public float duration = 0.45f;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class RichSlideInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly RichSlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _destPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RichSlideInCommandCharR(RichSlideInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 dest = _destPos;
        Vector2 dir = GetSlideDir(_spec.from);        // points ��from�� direction (where it comes from)
        Vector2 start = dest + dir * _spec.distance;  // start offscreen-ish

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = dest;
            yield break;
        }

        // time slices (normalized)
        float aP = Mathf.Clamp01(_spec.anticipationPortion);
        float oP = Mathf.Clamp01(_spec.overshootPortion);

        bool useA = _spec.useAnticipation && _spec.anticipationDistance != 0f && aP > 0f;
        bool useO = _spec.useOvershoot && _spec.overshootDistance != 0f && oP > 0f && oP < 1f;

        // Clamp segments so they always make sense
        if (!useA) aP = 0f;
        if (!useO) oP = 1f;

        // 1) place at start
        _rect.anchoredPosition = start;

        // positions
        Vector2 antiPos = start - dir * _spec.anticipationDistance; // slight pull opposite dir
        Vector2 overPos = dest - dir * _spec.overshootDistance;     // pass dest toward inside (opposite from-dir)

        float total = _spec.duration;

        // durations
        float tA = total * aP;
        float tApproach = total * (useO ? Mathf.Max(0.0001f, (oP - aP)) : Mathf.Max(0.0001f, (1f - aP)));
        float tSettle = useO ? total * Mathf.Max(0.0001f, (1f - oP)) : 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (useA)
        {
            seq.Append(_rect.DOAnchorPos(antiPos, tA).SetEase(_spec.anticipationEase));
        }

        if (useO)
        {
            seq.Append(_rect.DOAnchorPos(overPos, tApproach).SetEase(_spec.approachEase));
            seq.Append(_rect.DOAnchorPos(dest, tSettle).SetEase(_spec.settleEase));
        }
        else
        {
            seq.Append(_rect.DOAnchorPos(dest, tApproach).SetEase(_spec.approachEase));
        }

        // NOTE: BindToStep(scope) deliberately omitted per your request.

        if (_spec.wait)
            yield return seq.WaitForCompletion();
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

    private static Vector2 GetSlideDir(SlideFromCharR from)
    {
        return from switch
        {
            SlideFromCharR.Right => new Vector2(+1f, 0f),
            SlideFromCharR.Up    => new Vector2(0f, +1f),
            SlideFromCharR.Down  => new Vector2(0f, -1f),
            _                    => new Vector2(-1f, 0f), // Left
        };
    }
}

public sealed class RichSlideOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly RichSlideOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _startPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RichSlideOutCommandCharR(RichSlideOutCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) yield break;

        _rect.DOKill(false);

        Vector2 start = _startPos;                    // current
        Vector2 dir = GetSlideDir(_spec.to);          // exit direction
        Vector2 end = start + dir * _spec.distance;   // offscreen-ish

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = end;
            yield break;
        }

        float aP = Mathf.Clamp01(_spec.anticipationPortion);
        bool useA = _spec.useAnticipation && _spec.anticipationDistance != 0f && aP > 0f;

        if (!useA) aP = 0f;

        Vector2 antiPos = start - dir * _spec.anticipationDistance; // pull opposite before launch

        float total = _spec.duration;
        float tA = total * aP;
        float tL = total * Mathf.Max(0.0001f, (1f - aP));

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (useA)
            seq.Append(_rect.DOAnchorPos(antiPos, tA).SetEase(_spec.anticipationEase));

        seq.Append(_rect.DOAnchorPos(end, tL).SetEase(_spec.launchEase));

        // NOTE: BindToStep(scope) deliberately omitted per your request.

        if (_spec.wait)
            yield return seq.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted) ResolveRefs(scope);
        if (_rect == null) return;

        _rect.DOKill();
        // If you want "ensure out" on completion:
        _rect.anchoredPosition = _startPos + GetSlideDir(_spec.to) * _spec.distance;

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

    private static Vector2 GetSlideDir(SlideFromCharR from)
    {
        return from switch
        {
            SlideFromCharR.Right => new Vector2(+1f, 0f),
            SlideFromCharR.Up    => new Vector2(0f, +1f),
            SlideFromCharR.Down  => new Vector2(0f, -1f),
            _                    => new Vector2(-1f, 0f), // Left
        };
    }
}
