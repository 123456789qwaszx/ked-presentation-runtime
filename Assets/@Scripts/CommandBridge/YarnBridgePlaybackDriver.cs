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
    private ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();
    
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
        if (_isHoldActive)
        {
            _heldSpecs.Add(spec);
            return;
        }
        
        _collectedSpecs.Add(spec);
    }

    // public void PlayCollected()
    // {
    //     var specs = new List<CommandSpecBase>(_collectedSpecs);
    //     _collectedSpecs.Clear();
    //
    //     _executor.PlaySpecs(specs, CurrentScope, "yarn-bridge");
    // }
    
    public CommandRunTicket PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        if (specs.Count == 0)
        {
            CommandRunTicket empty = new CommandRunTicket(-1, "yarn-bridge-empty", 0);
            empty.CloseEntry();
            return empty;
        }

        return _executor.PlaySpecs(specs, CurrentScope, "yarn-bridge");
    }

    public void PlayImmediate(IReadOnlyList<CommandSpecBase> specs, string debugSource = "yarn-inline")
    {
        if (specs == null || specs.Count == 0)
            return;

        var copied = new List<CommandSpecBase>(specs);
        _executor.PlaySpecs(copied, CurrentScope, debugSource);
    }
    
    public void BeginBlockCapture()
    {
        if (_isHoldActive)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] begin_hold called while hold is already active.");
            return;
        }
        
        if (_collectedSpecs.Count > 0)
        {
            string commandNames = string.Join(", ", _collectedSpecs.ConvertAll(spec => spec.GetType().Name));

            Debug.Log(
                $"[YarnBridgePlaybackDriver] <<block_begin>> found {_collectedSpecs.Count} pre-collected command spec(s): {commandNames}. " +
                $"These commands will run with the line after <<block_end>>. " +
                $"For readability, move them below <<block_end>> if that is the intended timing."
            );
        }

        _isHoldActive = true;
        _heldSpecs.Clear();
    }
    
    public IEnumerator PlayCapturedBlock()
    {
        if (!_isHoldActive)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] <<play_block>> called without active <<capture_block>>.");
            yield break;
        }

        _isHoldActive = false;

        if (_heldSpecs.Count == 0)
            yield break;

        var heldSpecs = new List<CommandSpecBase>(_heldSpecs);
        _heldSpecs.Clear();

        yield return _executor.PlaySpecsBlocking(heldSpecs, CurrentScope, "yarn-block");
    }
    
    public void Clear()
    {
        _collectedSpecs.Clear();
        _heldSpecs.Clear();
        _isHoldActive = false;
    }
}
    
