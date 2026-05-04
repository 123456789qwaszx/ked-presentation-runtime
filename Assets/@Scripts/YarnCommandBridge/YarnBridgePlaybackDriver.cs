using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommandRunScopeProvider
{
    CommandRunScope CurrentScope { get; }
}

public sealed class YarnBridgePlaybackDriver : MonoBehaviour
{
    private CommandExecutor _executor;

    private int _pendingImmediateWaitCount;
    private readonly List<CommandSpecBase> _collectedSpecs = new();

    private bool _isHoldActive;
    private readonly List<CommandSpecBase> _heldSpecs = new();
    
    private ICommandRunScopeProvider _scopeProvider;
    private CommandRunScope CurrentScope => _scopeProvider != null
        ? _scopeProvider.CurrentScope
        : null;

    public void Initialize(
        CommandExecutor executor,
        ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
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

        if (_heldSpecs.Count == 0)
            yield break;

        var specs = new List<CommandSpecBase>(_heldSpecs);
        _heldSpecs.Clear();

        yield return _executor.PlaySpecsBlocking(specs, CurrentScope, "yarn-hold");
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
        // if (_collectedSpecs.Count == 0)
        //     return;

        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        _executor.PlaySpecs(specs, CurrentScope, "yarn-bridge");
    }

    public void ClearCollected()
    {
        _collectedSpecs.Clear();
    }

    private void ApplyImmediateWait(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        if (_pendingImmediateWaitCount <= 0)
            return;

        _pendingImmediateWaitCount--;
        spec.wait = true;
    }

    public void PlayImmediate(IReadOnlyList<CommandSpecBase> specs, string debugSource = "yarn-inline")
    {
        if (specs == null || specs.Count == 0)
            return;

        var copied = new List<CommandSpecBase>(specs);
        _executor.PlaySpecs(copied, CurrentScope, debugSource);
    }
}