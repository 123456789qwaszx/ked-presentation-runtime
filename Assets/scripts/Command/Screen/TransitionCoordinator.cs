using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TransitionCoordinator
{
    private readonly ITransitionTargetRouter _router;
    private readonly ITransitionTargetPlayer _player;
    private readonly List<ITransitionSwapParticipant> _participants;

    private readonly Dictionary<CommandRunScope, TransitionContext> _activeContexts = new();

    public TransitionCoordinator(
        ITransitionTargetRouter router,
        ITransitionTargetPlayer player,
        List<ITransitionSwapParticipant> participants = null)
    {
        _router       = router;
        _player       = player;
        _participants = participants ?? new List<ITransitionSwapParticipant>();
    }

    public void RegisterParticipant(ITransitionSwapParticipant participant)
    {
        if (participant != null && !_participants.Contains(participant))
            _participants.Add(participant);
    }

    public void UnregisterParticipant(ITransitionSwapParticipant participant)
    {
        _participants.Remove(participant);
    }

    /// <summary>
    /// CommandBase.ExecuteInner에서 yield return으로 호출.
    /// wait 여부는 CommandBase.WaitForCompletion이 결정하므로 여기선 항상 전체 흐름을 실행.
    /// </summary>
    public IEnumerator Play(TransitionCommandSpec spec, CommandRunScope scope)
    {
        yield return PlayInternal(spec, scope);
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

    private IEnumerator PlayInternal(TransitionCommandSpec spec, CommandRunScope scope)
    {
        if (!_router.TryResolve(spec.targetKind, spec.customTargetKey, out var target)
            || target == null || !target.IsValid)
        {
            if (spec.warnIfTargetMissing)
                Debug.LogWarning(
                    $"[TransitionCoordinator] Target not resolved. " +
                    $"kind={spec.targetKind}, customKey='{spec.customTargetKey}'");
            yield break;
        }

        if (spec.setInitialUncoveredState)
            _player.SetInstant(target, spec.uncoveredAlpha, false);

        var context = new TransitionContext(spec, target, scope);

        if (scope != null)
            _activeContexts[scope] = context;

        try
        {
            // ---- Skip 패스 ----
            if (scope != null && scope.IsSkipping)
            {
                _player.SetInstant(target, spec.coveredAlpha, spec.blockRaycastsWhileCovered);
                context.MarkCoverCompleted();
                NotifyParticipants(context);
                context.MarkSwapEntered();
                context.MarkReadyCompleted();
                _player.SetInstant(target, spec.uncoveredAlpha, false);
                context.MarkFinished();
                yield break;
            }

            // ---- 1. Cover ----
            yield return _player.FadeTo(
                target,
                spec.coveredAlpha,
                spec.coverDuration,
                spec.blockRaycastsWhileCovered,
                spec.coverEase);

            context.MarkCoverCompleted();

            // ---- 2. Swap 진입 알림 ----
            NotifyParticipants(context);
            context.MarkSwapEntered();

            // ---- 3. Ready 대기 ----
            yield return WaitForReady(context);

            if (context.CancelRequested)
            {
                _player.SetInstant(target, spec.uncoveredAlpha, false);
                context.MarkFinished();
                yield break;
            }

            // ---- 4. Hold ----
            if (!context.SkipRequested && spec.holdAfterReadySeconds > 0f)
                yield return WaitUnscaled(spec.holdAfterReadySeconds);

            // ---- 5. Uncover ----
            if (context.SkipRequested)
                _player.SetInstant(target, spec.uncoveredAlpha, false);
            else
                yield return _player.FadeTo(
                    target,
                    spec.uncoveredAlpha,
                    spec.uncoverDuration,
                    false,
                    spec.uncoverEase);

            context.MarkFinished();
        }
        finally
        {
            if (scope != null)
                _activeContexts.Remove(scope);
        }
    }

    private void NotifyParticipants(TransitionContext context)
    {
        for (int i = 0; i < _participants.Count; i++)
        {
            try { _participants[i]?.OnTransitionSwapEntered(context); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
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