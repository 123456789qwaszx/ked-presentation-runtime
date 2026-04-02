using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class CommandExecutor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    private SequencePlayer _sequencePlayer;
    private INodeCommandFactory _signalCommandFactory;
    private INodeCommandFactory _charRigCommandFactory;

    private CancellationTokenSource _cts;
    private Coroutine _mainRoutine;
    private CommandRunScope _activeScope;

    private int _runId;
    private bool _isStopInProgress;
    private bool _initialized;

    public void Initialize(
        INodeCommandFactory signalCommandFactory,
        INodeCommandFactory charRigCommandFactory)
    {
        _sequencePlayer = new SequencePlayer(this);
        _signalCommandFactory = signalCommandFactory;
        _charRigCommandFactory = charRigCommandFactory;
        _initialized = true;
    }

    private void OnDisable() => Stop(CleanupPolicy.Cancel);
    private void OnDestroy() => Stop(CleanupPolicy.Cancel);

    // ---- 기존 입구: Step 기반 ----
    public void PlayStep(NodeSpec node, int stepIndex, CommandRunScope scope)
    {
        if (!_initialized) return;
        if (node == null || scope == null) return;

        if (node.steps == null || stepIndex < 0 || stepIndex >= node.steps.Count)
        {
            Log($"Invalid stepIndex={stepIndex}");
            return;
        }

        StepSpec step = node.steps[stepIndex];
        if (step == null || step.compiled == null || step.compiled.Count == 0)
        {
            Log($"Step empty: stepIndex={stepIndex}");
            return;
        }

        StartPlay(step.compiled, scope, $"step={stepIndex}");
    }

    // ---- 새 입구: Bridge Spec 기반 ----
    public void PlaySpecs(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope, string debugSource = "bridge")
    {
        if (!_initialized) return;
        if (specs == null || scope == null) return;

        StartPlay(specs, scope, debugSource);
    }

    // ---- 공통 실행 경로 ----
    private void StartPlay(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope, string debugSource)
    {
        _activeScope = scope;

        CleanupPolicy policy = DecideCleanupPolicy(_activeScope);
        _activeScope.CleanupStep(policy);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);
        if (commands.Count == 0)
        {
            Log($"No commands ({debugSource})");
            return;
        }

        ResetToken();
        _activeScope.Token = _cts.Token;

        Log($"Play ({debugSource}), commands={commands.Count}");
        _mainRoutine = StartCoroutine(RunNode(commands, _activeScope, _runId));
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

            if (spec is CharRigCommandSpecBase)
            {
                if (!_charRigCommandFactory.TryCreate(spec, out ISequenceCommand cmd) || cmd == null)
                {
                    Log($"CharRig factory failed: {spec.GetType().Name}");
                    continue;
                }
                list.Add(cmd);
                continue;
            }

            if (!_signalCommandFactory.TryCreate(spec, out ISequenceCommand signalCmd) || signalCmd == null)
            {
                Log($"Signal factory failed: {spec.GetType().Name}");
                continue;
            }

            list.Add(signalCmd);
        }

        return list;
    }

    private void ResetToken()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    private void CancelAndDisposeToken()
    {
        if (_cts == null) return;
        try { if (!_cts.IsCancellationRequested) _cts.Cancel(); }
        catch (ObjectDisposedException) { }
        _cts.Dispose();
        _cts = null;
    }

    private CleanupPolicy DecideCleanupPolicy(CommandRunScope scope)
    {
        if (scope == null) return CleanupPolicy.Cancel;
        return CleanupPolicy.Finish;
    }

    private void Log(string msg)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[CommandExecutor] {msg}", this);
    }
}