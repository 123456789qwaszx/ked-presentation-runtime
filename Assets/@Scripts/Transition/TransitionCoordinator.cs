using System.Collections;
using UnityEngine;

public sealed class TransitionCoordinator
{
    public interface ITransitionTargetRouter
    {
        bool TryResolve(TransitionTargetKind kind, string customTargetKey, out TransitionTargetHandle handle);
    }

    public interface ITransitionTargetPlayer
    {
        void SetInstant(TransitionTargetHandle target, float alpha, bool blockRaycasts);

        IEnumerator FadeTo(
            TransitionTargetHandle target,
            float targetAlpha,
            float duration,
            bool blockRaycasts,
            AnimationCurve ease);
    }

    private readonly ITransitionTargetRouter _transitionTargetRouter;
    private readonly ITransitionTargetPlayer _transitionTargetPlayer;

    public TransitionCoordinator(
        ITransitionTargetRouter transitionTargetRouter,
        ITransitionTargetPlayer transitionTargetPlayer)
    {
        _transitionTargetRouter = transitionTargetRouter;
        _transitionTargetPlayer = transitionTargetPlayer;
    }

    public IEnumerator Play(TransitionCommandSpec spec, CommandRunScope scope)
    {
        if (spec == null)
            yield break;

        if (!_transitionTargetRouter.TryResolve(
                spec.targetKind,
                spec.customTargetKey,
                out TransitionTargetHandle target))
        {
            Debug.LogWarning(
                $"[TransitionCoordinator] Target not resolved. " +
                $"kind={spec.targetKind}, customKey='{spec.customTargetKey}'");
            yield break;
        }

        if (scope != null && scope.IsSkipping)
        {
            ApplySkipInstant(spec, target);
            yield break;
        }

        if (spec.resetToOpenAtStart)
        {
            _transitionTargetPlayer.SetInstant(
                target,
                spec.uncoveredAlpha,
                false);
        }

        yield return _transitionTargetPlayer.FadeTo(
            target,
            spec.coveredAlpha,
            spec.coverDuration,
            spec.blockRaycastsWhileCovered,
            spec.coverEase);

        if (spec.holdAfterReadySeconds > 0f)
            yield return WaitUnscaled(spec.holdAfterReadySeconds);

        yield return _transitionTargetPlayer.FadeTo(
            target,
            spec.uncoveredAlpha,
            spec.uncoverDuration,
            false,
            spec.uncoverEase);
    }

    public void SetCoveredInstant(TransitionCommandSpec spec)
    {
        if (spec == null)
            return;

        if (!_transitionTargetRouter.TryResolve(
                spec.targetKind,
                spec.customTargetKey,
                out TransitionTargetHandle target))
        {
            Debug.LogWarning(
                $"[TransitionCoordinator] Target not resolved. " +
                $"kind={spec.targetKind}, customKey='{spec.customTargetKey}'");
            return;
        }

        _transitionTargetPlayer.SetInstant(
            target,
            spec.coveredAlpha,
            spec.blockRaycastsWhileCovered);
    }

    public void SetUncoveredInstant(TransitionCommandSpec spec)
    {
        if (spec == null)
            return;

        if (!_transitionTargetRouter.TryResolve(
                spec.targetKind,
                spec.customTargetKey,
                out TransitionTargetHandle target))
        {
            Debug.LogWarning(
                $"[TransitionCoordinator] Target not resolved. " +
                $"kind={spec.targetKind}, customKey='{spec.customTargetKey}'");
            return;
        }

        _transitionTargetPlayer.SetInstant(
            target,
            spec.uncoveredAlpha,
            false);
    }

    private void ApplySkipInstant(TransitionCommandSpec spec, TransitionTargetHandle target)
    {
        _transitionTargetPlayer.SetInstant(
            target,
            spec.coveredAlpha,
            spec.blockRaycastsWhileCovered);

        _transitionTargetPlayer.SetInstant(
            target,
            spec.uncoveredAlpha,
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