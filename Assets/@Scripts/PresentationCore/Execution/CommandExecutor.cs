using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class CommandExecutor : MonoBehaviour
{
    private SequencePlayer _sequencePlayer;
    private CompositeCommandFactory _factory;

    private CancellationTokenSource _cts;
    private Coroutine _mainRoutine;
    private CommandRunScope _activeScope;
    private CommandRunTicket _activeTicket;

    private int _runId;

    public void Initialize(CompositeCommandFactory factory)
    {
        _sequencePlayer = new SequencePlayer(this);
        _factory = factory;
    }

    public void PlayStep(NodeSpec node, int stepIndex, CommandRunScope scope)
    {
        CloseActiveTicketIfOpen(CommandRunTicketCloseReason.Superseded);

        int runId = _runId;
        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromStep(node, stepIndex);
        int commandCount = commands.Count;

        var ticket = new CommandRunTicket(commandCount);
        _activeTicket = ticket;

        ResetToken();
        _activeScope.Token = _cts.Token;

        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, ticket));
    }

    public CommandRunTicket PlaySpecs(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope)
    {
        CloseActiveTicketIfOpen(CommandRunTicketCloseReason.Superseded);

        int runId = _runId;
        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        int commandCount = commands.Count;

        var ticket = new CommandRunTicket(commandCount);
        _activeTicket = ticket;

        ResetToken();
        _activeScope.Token = _cts.Token;

        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, ticket));

        return ticket;
    }

    public IEnumerator PlaySpecsBlocking(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope)
    {
        CloseActiveTicketIfOpen(CommandRunTicketCloseReason.Superseded);

        int runId = _runId;
        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        int commandCount = commands.Count;

        var ticket = new CommandRunTicket(commandCount);
        _activeTicket = ticket;

        ResetToken();
        _activeScope.Token = _cts.Token;

        yield return RunNode(commands, _activeScope, runId, ticket);
    }

    private List<ISequenceCommand> BuildCommandsFromStep(NodeSpec node, int stepIndex)
    {
        var list = new List<ISequenceCommand>();

        if (node == null || node.steps == null || node.steps.Count == 0)
            return list;

        if (stepIndex < 0 || stepIndex >= node.steps.Count)
            return list;

        StepSpec step = node.steps[stepIndex];

        return BuildCommandsFromSpecs(step.compiled);
    }

    private List<ISequenceCommand> BuildCommandsFromSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        var list = new List<ISequenceCommand>();

        if (specs == null)
            return list;

        for (int i = 0; i < specs.Count; i++)
        {
            CommandSpecBase spec = specs[i];

            if (spec == null)
                continue;

            if (!_factory.TryCreate(spec, out ISequenceCommand command) || command == null)
            {
                Debug.LogWarning($"Spec[{i}] {spec.GetType().Name} failed: factory could not create command.");
                continue;
            }

            list.Add(command);
        }

        return list;
    }

    private IEnumerator RunNode(
        List<ISequenceCommand> commands,
        CommandRunScope scope,
        int runId,
        CommandRunTicket ticket)
    {
        scope.SetNodeBusy(true);

        try
        {
            yield return _sequencePlayer.PlayCommands(
                commands,
                scope,
                isValid: () => runId == _runId,
                ticket: ticket);
        }
        finally
        {
            if (!ticket.EntryClosed)
                ticket.CloseEntry(CommandRunTicketCloseReason.Completed);

            if (runId == _runId)
            {
                scope.Token = CancellationToken.None;
                _mainRoutine = null;

                if (_activeTicket == ticket)
                    _activeTicket = null;
            }

            scope.SetNodeBusy(false);
        }
    }

    public void Stop(CleanupPolicy policy)
    {
        _runId++;

        CommandRunTicketCloseReason reason = policy == CleanupPolicy.Cancel
            ? CommandRunTicketCloseReason.Cancelled
            : CommandRunTicketCloseReason.Finished;

        CloseActiveTicketIfOpen(reason);
        CancelAndDisposeToken();
        
        if (_mainRoutine != null)
            StopCoroutine(_mainRoutine);
        
        _activeScope?.ClearRuntimeState(policy);
        
        _mainRoutine = null;
    }

    private void ResetToken()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    private void CancelAndDisposeToken()
    {
        if (_cts == null)
            return;

        if (!_cts.IsCancellationRequested)
            _cts.Cancel();

        _cts.Dispose();
        _cts = null;
    }

    private void CloseActiveTicketIfOpen(CommandRunTicketCloseReason reason)
    {
        if (_activeTicket == null)
            return;

        if (!_activeTicket.EntryClosed)
            _activeTicket.CloseEntry(reason);

        _activeTicket = null;
    }

    private CleanupPolicy DecideCleanupPolicy(CommandRunScope scope)
    {
        if (scope == null)
            return CleanupPolicy.Cancel;

        return CleanupPolicy.Finish;
    }
}