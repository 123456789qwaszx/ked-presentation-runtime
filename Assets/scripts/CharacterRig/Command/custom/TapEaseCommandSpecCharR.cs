using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Tap (Ease InOut)", Order = -739)]
public sealed class TapEaseCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Tap")]
    [Tooltip("Tap strength in px. +right, -left.")]
    public float strength = 34f;

    [Tooltip("Total duration.")]
    public float duration = 2f;

    [Min(1)]
    [Tooltip("How many back-and-forth oscillations.")]
    public int taps = 3;

    [Header("Damping")]
    [Tooltip("Bigger = dies out faster. 4~10 recommended.")]
    public float damping = 7f;

    [Header("Ease InOut principle")]
    [Tooltip("Time-warp for the motion (ease-in & ease-out).")]
    public Ease timeEase = Ease.InOutCubic;

    [Header("Tiny anticipation (very small)")]
    [Tooltip("Opposite micro prep in px. 0 disables. Keep small (0~3).")]
    public float prep = 1.5f;

    [Header("Wait")]
    public bool wait = true;
}

public sealed class TapEaseCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly TapEaseCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector2 _restPos;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TapEaseCommandCharR(TapEaseCommandSpecCharR spec) => _spec = spec;

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

        float A = _spec.strength;
        int taps = Mathf.Max(1, _spec.taps);
        float damp = Mathf.Max(0.01f, _spec.damping);
        float prep = Mathf.Clamp(_spec.prep, 0f, 6f);

        Tween tween = DOTween.To(
                () => 0f,
                t =>
                {
                    float raw = Mathf.Clamp01(t);

                    // 1) time-warp with EaseInOut (principle: accelerate then decelerate)
                    float u = DOVirtual.EasedValue(0f, 1f, raw, _spec.timeEase);

                    // 2) micro anticipation (very small and very early)
                    float prepTerm = 0f;
                    if (prep > 0f)
                    {
                        // lasts roughly first 10% in eased time
                        float s = Mathf.Clamp01(u / 0.10f);
                        // 0->1->0 bump
                        float bump = Mathf.Sin(Mathf.PI * s);
                        // opposite direction of main tap, fades quickly
                        prepTerm = -Mathf.Sign(A) * prep * bump * (1f - s);
                    }

                    // 3) envelope to guarantee x(0)=x(1)=0 (exact return to rest)
                    float envelope = Mathf.Sin(Mathf.PI * u); // 0..1..0

                    // 4) damped oscillation (back-and-forth)
                    float decay = Mathf.Exp(-damp * u);
                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);

                    float x = prepTerm + (A * envelope * decay * osc);

                    _rect.anchoredPosition = rest + new Vector2(x, 0f);
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)   // raw t is linear; we apply ease ourselves once
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
