using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

public enum NudgeDirectionCharR
{
    Right = 0,
    Left = 1,
    Up = 2,
    Down = 3,
}

[Serializable]
[CommandMenuHint("Char Rig Motion", "Nudge (Tap Neighbor)", Order = -740)]
public sealed class NudgeTapCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Nudge")]
    [Tooltip("How far the first tap pushes in the selected direction (px).")]
    public float strength = 22f;

    [Tooltip("Direction of the nudge.")]
    public SlideFromCharR direction = SlideFromCharR.Right;

    [Tooltip("Total duration for the whole nudge.")]
    public float duration = 0.88f;

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

        float amplitude = Mathf.Abs(_spec.strength);
        int taps = Mathf.Max(1, _spec.taps);
        float damping = Mathf.Max(0.01f, _spec.damping);
        float anticipation = Mathf.Abs(_spec.anticipation);

        // Signed world/local direction for this nudge.
        // Right/Up = positive axis, Left/Down = negative axis.
        Vector2 dir = GetSignedDirection(_spec.direction);

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t);

                    // Tiny anticipation before the first push.
                    // Briefly pulls opposite, then releases into the main tap.
                    float antiTerm = 0f;
                    if (!Mathf.Approximately(anticipation, 0f))
                    {
                        float s = Mathf.Clamp01(u / 0.15f);

                        // Smooth 0 -> 1 -> 0 bump
                        float bump = Mathf.Sin(Mathf.PI * s);

                        // Negative because anticipation goes opposite to main direction
                        antiTerm = -anticipation * bump * (1f - s);
                    }

                    // Damped oscillation
                    float decay = Mathf.Exp(-damping * u);

                    // Guarantees exact return to rest at start/end
                    float settleEnvelope = Mathf.Sin(Mathf.PI * u);

                    // Back-and-forth tap motion
                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);

                    // Final scalar amount along selected direction
                    float scalar = antiTerm + (amplitude * decay * osc * settleEnvelope);

                    _rect.anchoredPosition = rest + dir * scalar;
                },
                1f,
                _spec.duration
            )
            // Raw t stays linear; motion shaping is applied manually above
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

    private static Vector2 GetSignedDirection(SlideFromCharR direction)
    {
        return direction switch
        {
            SlideFromCharR.Left => Vector2.left,
            SlideFromCharR.Right => Vector2.right,
            SlideFromCharR.Up => Vector2.up,
            SlideFromCharR.Down => Vector2.down,
            _ => Vector2.right,
        };
    }
}