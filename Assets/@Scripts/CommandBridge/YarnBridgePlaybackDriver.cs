using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public interface ICommandRunScopeProvider
{
    CommandRunScope CurrentScope { get; }
}

public sealed class YarnBridgePlaybackDriver : MonoBehaviour
{
    private CommandExecutor _executor;
    private ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();
    private int _pendingImmediateWaitCount;

    private readonly List<CommandSpecBase> _heldSpecs = new();
    private bool _isHoldActive;
    
    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;
    
    public void Initialize(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
    }

    public void Enqueue(CommandSpecBase spec)
    {
        if (_pendingImmediateWaitCount > 0)
        {
            _pendingImmediateWaitCount--;
            spec.wait = true;
        }
        
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

        //Debug.Log("Playing collected spec: " + _collectedSpecs.Count);
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        _executor.PlaySpecs(specs, CurrentScope, "yarn-bridge");
    }

    public void PlayImmediate(IReadOnlyList<CommandSpecBase> specs, string debugSource = "yarn-inline")
    {
        if (specs == null || specs.Count == 0)
            return;

        var copied = new List<CommandSpecBase>(specs);
        _executor.PlaySpecs(copied, CurrentScope, debugSource);
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

        //Debug.Log("[YarnBridgePlaybackDriver] play holded Spec.");
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

    public void ClearCollected()
    {
        _collectedSpecs.Clear();
    }

    
    public IEnumerator PlaySubPresentationNodeBlocking(
        DialogueRunner runner,
        string nodeName,
        string debugSource = "sub-presentation")
    {
        if (runner == null)
        {
            Debug.LogWarning($"[YarnBridgePlaybackDriver] {debugSource} failed. DialogueRunner is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning($"[YarnBridgePlaybackDriver] {debugSource} failed. nodeName is null or empty.");
            yield break;
        }

        if (runner.IsDialogueRunning)
        {
            Debug.LogWarning($"[YarnBridgePlaybackDriver] {debugSource} ignored. Runner is already running. node='{nodeName}'.");
            yield break;
        }

        bool completed = false;

        void OnDialogueComplete()
        {
            completed = true;
        }

        runner.onDialogueComplete.AddListener(OnDialogueComplete);

        Debug.Log($"[YarnBridgePlaybackDriver] {debugSource} start. node='{nodeName}'.");

        runner.StartDialogue(nodeName);

        while (!completed && runner != null && runner.IsDialogueRunning)
            yield return null;

        runner.onDialogueComplete.RemoveListener(OnDialogueComplete);

        Debug.Log($"[YarnBridgePlaybackDriver] {debugSource} complete. node='{nodeName}'.");
    }
}