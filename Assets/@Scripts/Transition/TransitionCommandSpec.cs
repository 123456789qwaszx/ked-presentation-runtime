using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint("Transition", "Play Transition", Order = -950)]
public sealed class TransitionCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public TransitionTargetKind targetKind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";

    [Header("Opacity")]
    [Range(0f, 1f)]
    public float coveredAlpha = 1f;

    [Range(0f, 1f)]
    public float uncoveredAlpha = 0f;

    [Header("Durations")]
    [Min(0f)]
    public float coverDuration = 0.20f;

    [Min(0f)]
    public float uncoverDuration = 0.20f;

    [Tooltip("화면이 완전히 덮인 뒤, 다시 열기 전에 유지할 시간.")]
    [Min(0f)]
    public float holdCoveredSeconds = 0.5f;

    [Header("Ease")]
    public AnimationCurve coverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve uncoverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Playback")]
    [Tooltip("체크하면 전환이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = true;

    [Header("Options")]
    [Tooltip("덮인 동안 Raycast를 막습니다.")]
    public bool blockRaycastsWhileCovered = true;

    [Tooltip("시작 전에 강제로 열린 상태(uncoveredAlpha)로 초기화합니다.")]
    public bool resetToOpenAtStart = true;
}

public sealed class TransitionCommand : CommandBase
{
    private readonly TransitionCommandSpec _spec;
    private readonly ITransitionTargetRouter _targetRouter;
    private readonly ITransitionTargetPlayer _targetPlayer;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionCommand(
        ITransitionTargetRouter targetRouter,
        ITransitionTargetPlayer targetPlayer,
        TransitionCommandSpec spec)
    {
        _targetRouter = targetRouter;
        _targetPlayer = targetPlayer;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_spec == null)
            yield break;

        if (_targetRouter == null || _targetPlayer == null)
        {
            Debug.LogWarning("[TransitionCommand] Router or Player is null.");
            yield break;
        }

        if (!_targetRouter.TryResolve(
                _spec.targetKind,
                _spec.customTargetKey,
                out TransitionTargetHandle target))
        {
            Debug.LogWarning(
                $"[TransitionCommand] Target not resolved. " +
                $"kind={_spec.targetKind}, customKey='{_spec.customTargetKey}'");
            yield break;
        }

        if (_spec.resetToOpenAtStart)
        {
            _targetPlayer.SetInstant(
                target,
                _spec.uncoveredAlpha,
                false);
        }

        if (scope != null && scope.IsSkipping)
        {
            ApplySkipInstant(target);
            yield break;
        }

        yield return Cover(target);

        if (_spec.holdCoveredSeconds > 0f)
            yield return WaitUnscaled(_spec.holdCoveredSeconds);

        yield return Uncover(target);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (_spec == null)
            return;

        if (_targetRouter == null || _targetPlayer == null)
            return;

        if (!_targetRouter.TryResolve(
                _spec.targetKind,
                _spec.customTargetKey,
                out TransitionTargetHandle target))
        {
            return;
        }

        ApplySkipInstant(target);
    }

    private IEnumerator Cover(TransitionTargetHandle target)
    {
        if (_spec.coverDuration <= 0f)
        {
            _targetPlayer.SetInstant(
                target,
                _spec.coveredAlpha,
                _spec.blockRaycastsWhileCovered);
            yield break;
        }

        yield return _targetPlayer.FadeTo(
            target,
            _spec.coveredAlpha,
            _spec.coverDuration,
            _spec.blockRaycastsWhileCovered,
            _spec.coverEase);
    }

    private IEnumerator Uncover(TransitionTargetHandle target)
    {
        if (_spec.uncoverDuration <= 0f)
        {
            _targetPlayer.SetInstant(
                target,
                _spec.uncoveredAlpha,
                false);
            yield break;
        }

        yield return _targetPlayer.FadeTo(
            target,
            _spec.uncoveredAlpha,
            _spec.uncoverDuration,
            false,
            _spec.uncoverEase);
    }

    private void ApplySkipInstant(TransitionTargetHandle target)
    {
        _targetPlayer.SetInstant(
            target,
            _spec.coveredAlpha,
            _spec.blockRaycastsWhileCovered);

        _targetPlayer.SetInstant(
            target,
            _spec.uncoveredAlpha,
            false);
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}