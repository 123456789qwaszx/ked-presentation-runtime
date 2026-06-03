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
        if (spec == null)
            return;

        if (_isHoldActive)
        {
            _heldSpecs.Add(spec);
            return;
        }

        _collectedSpecs.Add(spec);
    }

    public CommandRunTicket PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        return PlayCopiedSpecs(specs);
    }

    public CommandRunTicket PlayImmediate(IReadOnlyList<CommandSpecBase> specs)
    {
        if (specs == null || specs.Count == 0)
            return CreateCompletedEmptyTicket();

        var copied = new List<CommandSpecBase>(specs);
        return PlayCopiedSpecs(copied);
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
                $"For readability, move them below <<block_end>> if that is the intended timing.");
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

        CommandRunScope scope = CurrentScope;
        if (scope == null)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] Cannot play captured block. CurrentScope is null.");
            yield break;
        }

        yield return _executor.PlaySpecsBlocking(heldSpecs, scope);
    }

    public void Clear()
    {
        _collectedSpecs.Clear();
        _heldSpecs.Clear();
        _isHoldActive = false;
    }

    private CommandRunTicket PlayCopiedSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        if (specs == null || specs.Count == 0)
            return CreateCompletedEmptyTicket();

        CommandRunScope scope = CurrentScope;
        if (scope == null)
        {
            Debug.LogWarning("[YarnBridgePlaybackDriver] Cannot play command specs. CurrentScope is null.");

            var failedTicket = new CommandRunTicket(specs.Count);
            failedTicket.CloseEntry(CommandRunTicketCloseReason.Faulted);
            return failedTicket;
        }

        return _executor.PlaySpecs(specs, scope);
    }

    private CommandRunTicket CreateCompletedEmptyTicket()
    {
        var ticket = new CommandRunTicket(0);

        CommandRunScope scope = CurrentScope;
        if (scope != null)
            scope.CleanupStep(CleanupPolicy.Finish);

        ticket.CloseEntry(CommandRunTicketCloseReason.Completed);
        return ticket;
    }
}