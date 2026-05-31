using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

public sealed class CommandExecutor : MonoBehaviour
{
    [Header("Trace")]
    [SerializeField] private bool enableTrace = true;
    [SerializeField] private bool logTraceStreaming = false;
    [SerializeField] private bool logTraceDumpOnRunEnd = true;

    [SerializeField, TextArea(3, 20)] private string tracePreview;

    private readonly StringBuilder _trace = new StringBuilder(4096);
    private const int MaxTraceChars = 20000;

    private SequencePlayer _sequencePlayer;
    private CompositeCommandFactory _factory;

    private CancellationTokenSource _cts;
    private Coroutine _mainRoutine;
    private CommandRunScope _activeScope;
    private CommandRunTicket _activeTicket;

    private int _runId;
    private bool _isStopInProgress;
    private bool _initialized;

    public void Initialize(CompositeCommandFactory factory)
    {
        if (_initialized)
            return;

        _sequencePlayer = new SequencePlayer(this);
        _factory = factory;

        _initialized = true;
    }

    private void OnDestroy() => Stop(CleanupPolicy.Cancel);

    public void PlayStep(NodeSpec node, int stepIndex, CommandRunScope scope)
    {
        ClearTrace();

        CloseActiveTicketIfOpen("New PlayStep requested");

        int runId = NextRunId("PlayStep");

        Trace($"PlayStep requested: stepIndex={stepIndex}, runId={runId}");

        _activeScope = scope;

        if (_activeScope == null)
        {
            Trace($"PlayStep skipped: scope is null. stepIndex={stepIndex}, runId={runId}");
            return;
        }

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        Trace($"CleanupStep(policy={policy})");
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromStep(node, stepIndex);
        int commandCount = commands != null ? commands.Count : 0;

        CommandRunTicket ticket = new CommandRunTicket(runId, $"step:{stepIndex}", commandCount);
        _activeTicket = ticket;

        if (commands == null || commands.Count == 0)
        {
            Trace($"PlayStep skipped: stepIndex={stepIndex}, no commands. runId={runId}");
            ticket.CloseEntry();
            Trace($"CommandEntrySatisfied: {ticket.Snapshot()}");
            ClearActiveTicketIfSame(ticket);
            return;
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Trace($"PlayStep begin: stepIndex={stepIndex}, commands={commands.Count}, runId={runId}");
        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, ticket));
    }

    public CommandRunTicket PlaySpecs(
        IReadOnlyList<CommandSpecBase> specs,
        CommandRunScope scope,
        string debugSource = "bridge")
    {
        ClearTrace();

        CloseActiveTicketIfOpen($"New PlaySpecs requested. source={debugSource}");

        int runId = NextRunId($"PlaySpecs/{debugSource}");
        int specCount = specs != null ? specs.Count : 0;

        Trace($"PlaySpecs requested: source={debugSource}, specs={specCount}, runId={runId}");

        _activeScope = scope;

        if (_activeScope == null)
        {
            Trace($"PlaySpecs skipped: source={debugSource}, scope is null. runId={runId}");

            CommandRunTicket nullScopeTicket = new CommandRunTicket(runId, debugSource, 0);
            nullScopeTicket.CloseEntry();
            return nullScopeTicket;
        }

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        Trace($"CleanupStep(policy={policy})");
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        int commandCount = commands != null ? commands.Count : 0;

        CommandRunTicket ticket = new CommandRunTicket(runId, debugSource, commandCount);
        _activeTicket = ticket;

        if (commands == null || commands.Count == 0)
        {
            Trace($"PlaySpecs skipped: source={debugSource}, no commands. runId={runId}");
            ticket.CloseEntry();
            Trace($"CommandEntrySatisfied: {ticket.Snapshot()}");
            ClearActiveTicketIfSame(ticket);
            return ticket;
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Trace($"PlaySpecs begin: source={debugSource}, commands={commands.Count}, runId={runId}");
        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, runId, ticket));

        return ticket;
    }

    public IEnumerator PlaySpecsBlocking(
        IReadOnlyList<CommandSpecBase> specs,
        CommandRunScope scope,
        string debugSource = "bridge_blocking")
    {
        ClearTrace();

        CloseActiveTicketIfOpen($"New PlaySpecsBlocking requested. source={debugSource}");

        int runId = NextRunId($"PlaySpecsBlocking/{debugSource}");
        int specCount = specs != null ? specs.Count : 0;

        Trace($"PlaySpecsBlocking requested: source={debugSource}, specs={specCount}, runId={runId}");

        _activeScope = scope;

        if (_activeScope == null)
        {
            Trace($"PlaySpecsBlocking skipped: source={debugSource}, scope is null. runId={runId}");
            yield break;
        }

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        Trace($"CleanupStep(policy={policy})");
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        int commandCount = commands != null ? commands.Count : 0;

        CommandRunTicket ticket = new CommandRunTicket(runId, debugSource, commandCount);
        _activeTicket = ticket;

        if (commands == null || commands.Count == 0)
        {
            Trace($"PlaySpecsBlocking skipped: source={debugSource}, no commands. runId={runId}");
            ticket.CloseEntry();
            Trace($"CommandEntrySatisfied: {ticket.Snapshot()}");
            ClearActiveTicketIfSame(ticket);
            yield break;
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Trace($"PlaySpecsBlocking begin: source={debugSource}, commands={commands.Count}, runId={runId}");

        yield return RunNode(commands, _activeScope, runId, ticket);
    }

    private List<ISequenceCommand> BuildCommandsFromStep(NodeSpec node, int stepIndex)
    {
        var list = new List<ISequenceCommand>();

        if (node == null)
        {
            Trace("BuildCommandsFromStep skipped: node is null.");
            return list;
        }

        if (node.steps == null || node.steps.Count == 0)
        {
            Trace($"BuildCommandsFromStep skipped: node has no steps. node={node}");
            return list;
        }

        if (stepIndex < 0 || stepIndex >= node.steps.Count)
        {
            Trace($"BuildCommandsFromStep skipped: invalid stepIndex={stepIndex}, steps={node.steps.Count}");
            return list;
        }

        StepSpec step = node.steps[stepIndex];
        if (step == null)
        {
            Trace($"BuildCommandsFromStep skipped: step is null. stepIndex={stepIndex}");
            return list;
        }

        if (step.compiled == null || step.compiled.Count == 0)
        {
            Trace($"BuildCommandsFromStep skipped: step has no compiled specs. stepIndex={stepIndex}");
            return list;
        }

        Trace($"BuildCommandsFromStep: stepIndex={stepIndex}, specs={step.compiled.Count}");
        return BuildCommandsFromSpecs(step.compiled);
    }

    private List<ISequenceCommand> BuildCommandsFromSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        var list = new List<ISequenceCommand>();

        if (specs == null)
        {
            Trace("BuildCommandsFromSpecs skipped: specs is null.");
            return list;
        }

        for (int i = 0; i < specs.Count; i++)
        {
            CommandSpecBase spec = specs[i];
            if (spec == null)
            {
                Trace($"Spec[{i}] null; skipped.");
                continue;
            }

            string specType = spec.GetType().Name;

            if (_factory == null)
            {
                Trace($"Spec[{i}] {specType} failed: factory is null.");
                continue;
            }

            if (!_factory.TryCreate(spec, out ISequenceCommand command) || command == null)
            {
                Trace($"Spec[{i}] {specType} failed: factory could not create command.");
                continue;
            }

            Trace($"Spec[{i}] {specType} -> {command.GetType().Name}");
            list.Add(command);
        }

        Trace($"BuildCommandsFromSpecs complete: specs={specs.Count}, commands={list.Count}");
        return list;
    }

    private IEnumerator RunNode(
        List<ISequenceCommand> commands,
        CommandRunScope scope,
        int runId,
        CommandRunTicket ticket)
    {
        if (runId != _runId)
        {
            Trace($"RunNode exited early: stale runId={runId}, current={_runId}");
            CloseTicket(ticket, "RunNode exited early: stale run");
            yield break;
        }

        if (commands == null)
        {
            Trace($"RunNode exited early: commands is null. runId={runId}");
            CloseTicket(ticket, "RunNode exited early: commands null");
            yield break;
        }

        if (scope == null)
        {
            Trace($"RunNode exited early: scope is null. runId={runId}");
            CloseTicket(ticket, "RunNode exited early: scope null");
            yield break;
        }

        scope.SetNodeBusy(true);
        Trace($"Node Begin: runId={runId}, commands={commands.Count}");

        try
        {
            yield return _sequencePlayer.PlayCommands(
                commands,
                scope,
                runId: runId,
                isValid: () => runId == _runId,
                ticket: ticket,
                trace: Trace);
        }
        finally
        {
            CloseTicket(ticket, "RunNode finally");

            if (runId == _runId)
            {
                scope.SetNodeBusy(false);
                scope.Token = CancellationToken.None;
                _mainRoutine = null;

                Trace($"Node End: runId={runId}");

                if (logTraceDumpOnRunEnd)
                    DumpTraceToConsole("[CommandExecutor] Trace dump (Run End)");
            }
            else
            {
                scope.SetNodeBusy(false);
                Trace($"Node End skipped cleanup: stale runId={runId}, current={_runId}");
            }

            ClearActiveTicketIfSame(ticket);
        }
    }

    public void Stop() => Stop(CleanupPolicy.Cancel);
    public void FinishAll() => Stop(CleanupPolicy.Finish);

    private void Stop(CleanupPolicy policy)
    {
        if (_isStopInProgress)
        {
            Trace($"Stop ignored: already in progress. policy={policy}");
            return;
        }

        _isStopInProgress = true;

        try
        {
            int previousRunId = _runId;
            _runId++;

            Trace($"Stop begin: policy={policy}, runId {previousRunId} -> {_runId}");

            CloseActiveTicketIfOpen($"Stop({policy})");

            CancelAndDisposeToken();

            if (_mainRoutine != null)
            {
                StopCoroutine(_mainRoutine);
                _mainRoutine = null;
                Trace("Main routine stopped.");
            }

            _sequencePlayer?.Stop();

            if (_activeScope != null)
            {
                Trace($"Cleanup active scope: policy={policy}");

                _activeScope.ClearRuntimeState(policy);
                _activeScope = null;
            }

            Trace($"Stop complete: policy={policy}");

            if (logTraceDumpOnRunEnd)
                DumpTraceToConsole("[CommandExecutor] Trace dump (Stop)");
        }
        finally
        {
            _isStopInProgress = false;
        }
    }

    private void ResetToken()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        Trace("Token reset.");
    }

    private void CancelAndDisposeToken()
    {
        if (_cts == null)
        {
            Trace("CancelAndDisposeToken skipped: token is null.");
            return;
        }

        try
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                Trace("Token canceled.");
            }
        }
        catch (ObjectDisposedException)
        {
            Trace("Token cancel skipped: already disposed.");
        }

        _cts.Dispose();
        _cts = null;

        Trace("Token disposed.");
    }

    private CleanupPolicy DecideCleanupPolicy(CommandRunScope scope)
    {
        if (scope == null)
            return CleanupPolicy.Cancel;

        return CleanupPolicy.Finish;
    }

    private void CloseActiveTicketIfOpen(string reason)
    {
        if (_activeTicket == null)
            return;

        if (!_activeTicket.EntryClosed)
        {
            _activeTicket.CloseEntry();
            Trace($"ActiveTicketClosed: reason={reason}, {_activeTicket.Snapshot()}");

            if (!_activeTicket.EntrySatisfied)
                Trace($"CommandEntryGuaranteeFailed: reason={reason}, {_activeTicket.Snapshot()}");
        }

        _activeTicket = null;
    }

    private void CloseTicket(CommandRunTicket ticket, string reason)
    {
        if (ticket == null)
            return;

        if (!ticket.EntryClosed)
        {
            ticket.CloseEntry();
            Trace($"TicketClosed: reason={reason}, {ticket.Snapshot()}");
        }

        if (!ticket.EntrySatisfied)
            Trace($"CommandEntryGuaranteeFailed: reason={reason}, {ticket.Snapshot()}");
        else
            Trace($"CommandEntrySatisfied: reason={reason}, {ticket.Snapshot()}");
    }

    private void ClearActiveTicketIfSame(CommandRunTicket ticket)
    {
        if (ReferenceEquals(_activeTicket, ticket))
            _activeTicket = null;
    }

    private void Trace(string msg)
    {
        if (!enableTrace)
            return;

        if (_trace.Length > MaxTraceChars)
            _trace.Remove(0, _trace.Length - (MaxTraceChars / 2));

        string line = $"[{Time.frameCount}] {msg}";

        _trace.AppendLine(line);
        tracePreview = _trace.ToString();

        if (logTraceStreaming)
            Debug.Log($"[CommandExecutor] {line}", this);
    }

    private void DumpTraceToConsole(string header)
    {
        if (!enableTrace)
            return;

        Debug.Log($"{header}\n{_trace}", this);
    }

    public void ClearTrace()
    {
        //_trace.Clear();
        //tracePreview = string.Empty;
    }

    private int NextRunId(string reason)
    {
        int previous = _runId;
        _runId++;

        Trace($"RunId advanced: {previous} -> {_runId}, reason={reason}");

        return _runId;
    }
}