using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Jolt", Order = -740)]
public sealed class JoltCommandSpec : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Nudge")]
    public float strength = 22f;
    public CharRDirection direction = CharRDirection.Right;
    public float duration = 0.88f;

    [Min(1)]
    public int taps = 3;

    public float damping = 6f;

    [Header("Style")]
    public float anticipation = 3f;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class JoltCommand : CommandBase, IStepScopedCommand
{
    private readonly JoltCommandSpec _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _restPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public JoltCommand(JoltCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _canCommitFinalState = true;

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            _rect.anchoredPosition = _restPos;
            ClearRuntimeRefs();
            yield break;
        }

        Vector2 rest = _restPos;

        float amplitude = Mathf.Abs(_spec.strength);
        int taps = Mathf.Max(1, _spec.taps);
        float damping = Mathf.Max(0.01f, _spec.damping);
        float anticipation = Mathf.Abs(_spec.anticipation);

        Vector2 dir = GetSignedDirection(_spec.direction);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float u = Mathf.Clamp01(t);

                    float antiTerm = 0f;
                    if (!Mathf.Approximately(anticipation, 0f))
                    {
                        float s = Mathf.Clamp01(u / 0.15f);
                        float bump = Mathf.Sin(Mathf.PI * s);
                        antiTerm = -anticipation * bump * (1f - s);
                    }

                    float decay = Mathf.Exp(-damping * u);
                    float settleEnvelope = Mathf.Sin(Mathf.PI * u);
                    float osc = Mathf.Sin(2f * Mathf.PI * taps * u);

                    float scalar = antiTerm + (amplitude * decay * osc * settleEnvelope);

                    _rect.anchoredPosition = rest + dir * scalar;
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = rest;
                ClearRuntimeRefs();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.anchoredPosition = _restPos;
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _restPos;

        ClearRuntimeRefs();
    }
    
    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRig(scope, _spec.roleKey);

        _rect = rig.GetRect(_spec.target);
        _restPos = _rect.anchoredPosition;
    }

    private void ClearRuntimeRefs()
    {
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private static Vector2 GetSignedDirection(CharRDirection direction)
    {
        return direction switch
        {
            CharRDirection.Left => Vector2.left,
            CharRDirection.Right => Vector2.right,
            CharRDirection.Up => Vector2.up,
            CharRDirection.Down => Vector2.down,
            _ => Vector2.right,
        };
    }
}