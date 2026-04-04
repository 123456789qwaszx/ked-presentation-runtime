using System.Collections;
using System.Collections.Generic;
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
        IEnumerator FadeTo(TransitionTargetHandle target, float targetAlpha, float duration, bool blockRaycasts, AnimationCurve ease);
    }
    
    private readonly ITransitionTargetRouter _transitionTargetRouter;
    private readonly ITransitionTargetPlayer _transitionTargetPlayer;

    private readonly Dictionary<CommandRunScope, TransitionContext> _activeContexts = new();

    public TransitionCoordinator(
        ITransitionTargetRouter transitionTargetRouter,
        ITransitionTargetPlayer transitionTargetPlayer)
    {
        _transitionTargetRouter = transitionTargetRouter;
        _transitionTargetPlayer = transitionTargetPlayer;
    }
    
    public IEnumerator Play(TransitionCommandSpec spec, CommandRunScope scope)
    {
        if (!_transitionTargetRouter.TryResolve(spec.targetKind, spec.customTargetKey, out TransitionTargetHandle target))
        {
            Debug.LogWarning($"[TransitionCoordinator] Target not resolved. kind={spec.targetKind}, customKey='{spec.customTargetKey}'");
            yield break;
        }
        
        // Start from alpha 0, then cover fades 0 -> 1.
        if (spec.resetToOpenAtStart)
            _transitionTargetPlayer.SetInstant(target, spec.uncoveredAlpha, false);

        TransitionContext context = new (spec, target, scope);
        _activeContexts[scope] = context;

        try
        {
            if (scope.IsSkipping)
            {
                // If already skipping, collapse the whole transition into one instant pass:
                // cover -> swap -> ready -> uncover, with no tween or wait.
                _transitionTargetPlayer.SetInstant(target, spec.coveredAlpha, spec.blockRaycastsWhileCovered);
                context.MarkCoverCompleted();
                context.MarkSwapEntered();
                context.MarkReadyCompleted();
                _transitionTargetPlayer.SetInstant(target, spec.uncoveredAlpha, false);
                context.MarkFinished();
                yield break;
            }

            // ---- 1. Cover ----
            yield return _transitionTargetPlayer.FadeTo(
                target,
                spec.coveredAlpha,
                spec.coverDuration,
                spec.blockRaycastsWhileCovered,
                spec.coverEase);

            context.MarkCoverCompleted();

            // ---- 2. Swap 진입 알림 ----
            context.MarkSwapEntered();

            // ---- 3. Ready 대기 ----
            yield return WaitForReady(context);

            if (context.CancelRequested)
            {
                _transitionTargetPlayer.SetInstant(target, spec.uncoveredAlpha, false);
                context.MarkFinished();
                yield break;
            }

            // ---- 4. Hold ----
            if (!context.SkipRequested && spec.holdAfterReadySeconds > 0f)
                yield return WaitUnscaled(spec.holdAfterReadySeconds);

            // ---- 5. Uncover ----
            if (context.SkipRequested)
                _transitionTargetPlayer.SetInstant(target, spec.uncoveredAlpha, false);
            else
                yield return _transitionTargetPlayer.FadeTo(
                    target,
                    spec.uncoveredAlpha,
                    spec.uncoverDuration,
                    false,
                    spec.uncoverEase);

            context.MarkFinished();
        }
        finally
        {
            _activeContexts.Remove(scope);
        }
    }

    public void RequestSkip(CommandRunScope scope)
    {
        if (_activeContexts.TryGetValue(scope, out var ctx))
            ctx.RequestSkip();
    }

    public void RequestCancel(CommandRunScope scope)
    {
        if (_activeContexts.TryGetValue(scope, out var ctx))
            ctx.RequestCancel();
    }

    private IEnumerator WaitForReady(TransitionContext context)
    {
        while (!context.IsReadyToUncover())
        {
            if (context.IsTimedOut())
            {
                HandleTimeout(context);
                yield break;
            }
            yield return null;
        }

        context.MarkReadyCompleted();
    }

    private void HandleTimeout(TransitionContext context)
    {
        Debug.LogWarning(
            $"[TransitionCoordinator] Ready timeout. policy={context.Spec.timeoutPolicy}");

        switch (context.Spec.timeoutPolicy)
        {
            case TransitionTimeoutPolicy.ForceUncover:
                context.MarkReadyCompleted();
                break;
            case TransitionTimeoutPolicy.KeepCovered:
                context.RequestSkip();
                context.MarkReadyCompleted();
                break;
            case TransitionTimeoutPolicy.CancelTransition:
                context.RequestCancel();
                context.MarkReadyCompleted();
                break;
        }
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}