using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Nudge (Tap Neighbor)", Order = -740)]
public sealed class NudgeTapCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Nudge")]
    [Tooltip("How far the first tap pushes horizontally (px). + = right, - = left.")]
    public float strength = 22f;

    [Tooltip("Total duration for the whole nudge.")]
    public float duration = 0.28f;

    [Min(1)]
    [Tooltip("How many oscillations (back-and-forth) happen. 2~4 feels 'tap tap'.")]
    public int taps = 3;

    [Tooltip("Damping factor. Bigger = dies out faster. (3~9 recommended)")]
    public float damping = 6f;

    [Header("Style")]
    [Tooltip("Tiny anticipation before the main tap (in px). 0 disables.")]
    public float anticipation = 3f;

    [Header("Wait")]
    public bool wait = false;
}

public sealed class NudgeTapCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly NudgeTapCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _restPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public NudgeTapCommandCharR(NudgeTapCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        _rect.DOKill(false);

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            _rect.anchoredPosition = _restPos;
            yield break;
        }

        Vector2 rest = _restPos;

        float A = _spec.strength;                 // amplitude (px)
        int taps = Mathf.Max(1, _spec.taps);
        float k = Mathf.Max(0.01f, _spec.damping); // damping
        float anti = _spec.anticipation;

        // We drive t linearly (0..1) and compute x(t) ourselves.
        // x(t) = A * exp(-k t) * sin(2π * taps * t) * envelope(t)
        // envelope ensures x(0)=0 and x(1)=0 so we return to rest cleanly.
        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t);

                    // Anticipation: tiny opposite move early, then release into tap.
                    // Quick "pull back" before the first push.
                    float antiTerm = 0f;
                    if (!Mathf.Approximately(anti, 0f))
                    {
                        // a small bump in the first ~15% of time
                        float s = Mathf.Clamp01(u / 0.15f);
                        // smooth 0->1->0 bump
                        float bump = Mathf.Sin(Mathf.PI * s);
                        antiTerm = -Mathf.Sign(A) * anti * bump * (1f - s); // fades quickly
                    }

                    // Damped oscillation
                    float decay = Mathf.Exp(-k * u);

                    // Ensure exact start/end at rest: multiply by sin(πu) envelope
                    // (0 at u=0 and u=1, peak around middle)
                    float settleEnvelope = Mathf.Sin(Mathf.PI * u);

                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);

                    float x = antiTerm + (A * decay * osc * settleEnvelope);

                    _rect.anchoredPosition = rest + new Vector2(x, 0f);
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
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

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
}
