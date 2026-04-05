using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class YarnBridgePlaybackDriver : MonoBehaviour
{
    private CommandExecutor _executor;
    private CommandRunScope _scope;

    private int _pendingImmediateWaitCount;
    private readonly List<CommandSpecBase> _collectedSpecs = new();

    private bool _isHoldActive;
    private readonly List<CommandSpecBase> _heldSpecs = new();

    public void Initialize(
        CommandExecutor executor,
        PresentationPlaybackSettings settings)
    {
        _executor = executor;

        PresentationSessionContext context = new(settings);
        _scope = new CommandRunScope(context);
    }

    public void BeginHold()
    {
        if (_isHoldActive)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] begin_hold called while hold is already active.");
            return;
        }

        _isHoldActive = true;
        _heldSpecs.Clear();
    }

    public IEnumerator EndHoldBlocking()
    {
        if (!_isHoldActive)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] end_hold called without begin_hold.");
            yield break;
        }

        _isHoldActive = false;

        Debug.Log(_heldSpecs.Count);
        if (_heldSpecs.Count == 0)
            yield break;

        var specs = new List<CommandSpecBase>(_heldSpecs);
        _heldSpecs.Clear();

        yield return _executor.PlaySpecsBlocking(specs, _scope, "yarn-hold");
    }

    public void ResetImmediateWaitForNewLine()
    {
        _pendingImmediateWaitCount = 0;
    }

    public void WaitNextImmediateCommands(int count = 1)
    {
        _pendingImmediateWaitCount = Mathf.Max(0, count);
    }

    public void Enqueue(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        ApplyImmediateWait(spec);

        if (_isHoldActive)
        {
            _heldSpecs.Add(spec);
            return;
        }

        _collectedSpecs.Add(spec);
    }

    public void PlayCollected()
    {
        if (_collectedSpecs.Count == 0)
            return;

        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        _executor.PlaySpecs(specs, _scope, "yarn-bridge");
    }

    public void ClearCollected()
    {
        _collectedSpecs.Clear();
    }

    private void ApplyImmediateWait(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        bool shouldWait = _pendingImmediateWaitCount > 0;

        switch (spec)
        {
            case NudgeTapCommandSpecCharR nudgeTap:
                nudgeTap.wait = shouldWait;
                break;

            case BounceArcInCommandSpecCharR bounceArcIn:
                bounceArcIn.wait = shouldWait;
                break;

            case DipInOutCommandSpecCharR dipInOut:
                dipInOut.wait = shouldWait;
                break;

            case MoveByCommandSpecCharR moveBy:
                moveBy.wait = shouldWait;
                break;

            case BouncySlideInCommandSpecCharR bouncySlideIn:
                bouncySlideIn.wait = shouldWait;
                break;

            case FadeInCommandSpecCharR fadeIn:
                fadeIn.wait = shouldWait;
                break;

            case FadeOutCommandSpecCharR fadeOut:
                fadeOut.wait = shouldWait;
                break;

            case JuicySlideInCommandSpecCharR slideIn:
                slideIn.wait = shouldWait;
                break;

            case JuicySlideOutCommandSpecCharR slideOut:
                slideOut.wait = shouldWait;
                break;

            case TransitionCommandSpec transition:
                transition.wait = shouldWait;
                break;
        }

        if (shouldWait)
            _pendingImmediateWaitCount--;
    }
}