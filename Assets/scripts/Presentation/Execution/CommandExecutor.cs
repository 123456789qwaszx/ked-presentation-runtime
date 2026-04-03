using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class CommandExecutor : MonoBehaviour
{
    [Header("Debug")] [SerializeField] private bool enableDebugLog = true;

    private SequencePlayer _sequencePlayer;
    private CompositeCommandFactory _factory;

    private CancellationTokenSource _cts;
    private Coroutine _mainRoutine;
    private CommandRunScope _activeScope;

    private int _runId;
    private bool _isStopInProgress;
    private bool _initialized;

    public void Initialize(CompositeCommandFactory factory)
    {
        _sequencePlayer = new SequencePlayer(this);
        _factory = factory;
        _initialized = true;
    }

    private void OnDisable() => Stop(CleanupPolicy.Cancel);
    private void OnDestroy() => Stop(CleanupPolicy.Cancel);


    public void PlayStep(NodeSpec node, int stepIndex, CommandRunScope scope)
    {
        if (!_initialized) return;
        if (node == null || scope == null) return;

        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromStep(node, stepIndex);
        if (commands == null || commands.Count == 0)
        {
            Log($"Step has no commands: stepIndex={stepIndex}");
            return; // CleanupStep은 했지만 token/coroutine은 건드리지 않음
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Log($"Step Play: stepIndex={stepIndex}, commands={commands.Count}");
        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, _runId));
    }

    public void PlaySpecs(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope, string debugSource = "bridge")
    {
        if (!_initialized) return;
        if (specs == null || scope == null) return;

        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        if (commands == null || commands.Count == 0)
        {
            Log($"No commands ({debugSource})");
            return;
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Log($"Play ({debugSource}), commands={commands.Count}");
        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, _runId));
    }
    
    
    private List<ISequenceCommand> BuildCommandsFromStep(NodeSpec node, int stepIndex)
    {
        var list = new List<ISequenceCommand>();

        if (node.steps == null || node.steps.Count == 0)
        {
            Log($"Node Empty (node={node})");
            return list;
        }

        if (stepIndex < 0 || stepIndex >= node.steps.Count)
        {
            Log($"Invalid stepIndex: {stepIndex} (steps={node.steps.Count})");
            return list;
        }

        StepSpec step = node.steps[stepIndex];
        if (step == null || step.compiled == null || step.compiled.Count == 0)
        {
            Log($"Step Empty (step={step})");
            return list;
        }

        return BuildCommandsFromSpecs(step.compiled);
    }

    private List<ISequenceCommand> BuildCommandsFromSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        var list = new List<ISequenceCommand>();

        for (int i = 0; i < specs.Count; i++)
        {
            CommandSpecBase spec = specs[i];
            if (spec == null)
            {
                Log($"Null spec at index={i}; skipped.");
                continue;
            }

            if (!_factory.TryCreate(spec, out ISequenceCommand command) || command == null)
            {
                continue;
            }

            list.Add(command);
        }

        return list;
    }
    
    private IEnumerator RunNode(List<ISequenceCommand> commands, CommandRunScope scope, int runId)
    {
        if (runId != _runId)
        {
            Log($"RunNode exited early: stale runId={runId}, current={_runId}");
            yield break;
        }

        scope.SetNodeBusy(true);
        Log($"Node Begin (runId={runId})");

        try
        {
            yield return _sequencePlayer.PlayCommands(
                commands,
                scope,
                runId: runId,
                isValid: () => runId == _runId,
                trace: msg => Log(msg)
            );
        }
        finally
        {
            if (runId == _runId && scope != null)
            {
                scope.SetNodeBusy(false);
                scope.Token = CancellationToken.None;
                _mainRoutine = null;
                Log($"Node End (runId={runId})");
            }
        }
    }
    
    public void Stop() => Stop(CleanupPolicy.Cancel);
    public void FinishAll() => Stop(CleanupPolicy.Finish);

    private void Stop(CleanupPolicy policy)
    {
        if (_isStopInProgress) return;
        _isStopInProgress = true;

        try
        {
            _runId++;
            CancelAndDisposeToken();

            if (_mainRoutine != null)
            {
                StopCoroutine(_mainRoutine);
                _mainRoutine = null;
            }

            _sequencePlayer?.Stop();

            if (_activeScope != null)
            {
                _activeScope.CleanupStep(policy);
                _activeScope.CleanupRun(policy);
                _activeScope.SetNodeBusy(false);
                _activeScope.Token = CancellationToken.None;
                _activeScope = null;
            }

            Log($"Stop(policy={policy})");
        }
        finally
        {
            _isStopInProgress = false;
        }
    }

    private void ResetToken()
    {
        // Only responsible for creating a new token for the next run.
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    private void CancelAndDisposeToken()
    {
        if (_cts == null) return;
        try
        {
            if (!_cts.IsCancellationRequested) _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _cts.Dispose();
        _cts = null;
    }

    private CleanupPolicy DecideCleanupPolicy(CommandRunScope scope)
    {
        if (scope == null) return CleanupPolicy.Cancel;

        // Skip means "complete immediately".
        // if (scope.IsSkipping)
        //     return CleanupPolicy.Finish;

        return CleanupPolicy.Finish;
    }

    private void Log(string msg)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[CommandExecutor] {msg}", this);
    }
}