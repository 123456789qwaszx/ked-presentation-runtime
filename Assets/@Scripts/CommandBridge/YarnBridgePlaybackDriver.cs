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
    
    public void Enqueue(CommandSpecBase spec) => _collectedSpecs.Add(spec);
    public void Clear() => _collectedSpecs.Clear();
    
    public CommandRunTicket PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        if (specs.Count == 0)
            return CreateCompletedEmptyTicket();

        return _executor.PlaySpecs(specs, CurrentScope);
    }
    
    
    private CommandRunTicket CreateCompletedEmptyTicket()
    {
        var ticket = new CommandRunTicket(0);

        CurrentScope?.CleanupStep(CleanupPolicy.Finish);

        ticket.CloseEntry(CommandRunTicketCloseReason.Completed);
        return ticket;
    }
    
    public void SetPresentationActor(string actorKey)
    {
        CurrentScope?.CharacterTargetAliases.SetPresentationActor(actorKey);
    }
    
    public void RegisterPresentationActorAlias(string aliasSymbol, string targetKey)
    {
        CurrentScope?.CharacterTargetAliases.Register(aliasSymbol, targetKey);
    }
}