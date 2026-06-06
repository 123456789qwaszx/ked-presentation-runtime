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

    public void Clear()
    {
        _collectedSpecs.Clear();
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