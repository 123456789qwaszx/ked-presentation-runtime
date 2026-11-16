using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CommandExecutor : MonoBehaviour
{
    private SequencePlayer _sequencePlayer;
    private CompositeCommandFactory _factory;

    private CancellationTokenSource _cts;
    // 지금 스텝의 실행들 — 스텝을 연 실행 하나와 그 스텝에 덧붙은 실행들(PlaySpecsInStep).
    private readonly List<Coroutine> _stepRoutines = new();

    // 지금 스텝에서 아직 도는 실행의 수. 0이 될 때 토큰과 진행 표시를 내린다.
    private int _activeStepRuns;
    private CommandRunScope _activeScope;

    private int _runId;

    // 스텝(PlaySpecs 한 번)의 세대 — _runId는 런(Stop)의 세대다.
    private int _stepId;

    public void Initialize(CompositeCommandFactory factory)
    {
        _sequencePlayer = new SequencePlayer(this);
        _factory = factory;
    }
    
    public void PlaySpecs(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope)
    {
        // 세션(scope)이 없을 때 스텝이 오는 것은 호출 순서의 잘못이다 — 막는 척하지 않고 드러낸다.
        if (scope == null)
        {
            Debug.LogWarning("[CommandExecutor] PlaySpecs without a scope — the session has not started or already ended. Skipped.");
            return;
        }

        int runId = _runId;
        int stepId = ++_stepId;
        _activeScope = scope;

        _activeScope.CleanupStep(CleanupPolicy.Finish);

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);

        ResetToken();
        _activeScope.Token = _cts.Token;

        _stepRoutines.Clear();
        _activeStepRuns = 0;

        StartStepRun(commands, runId, stepId);
    }

    // 지금 스텝에 덧붙여 실행한다 — 스텝 경계를 치지 않는다(앞 실행의 배경 커맨드를 마무리하지 않는다).
    // 덧붙은 커맨드의 배경 수명은 같은 스텝에 묶여 다음 스텝 경계에서 같이 정리된다.
    // 진행 표시(CommandIsPlaying)와 토큰은 이 스텝의 실행이 모두 끝날 때 내린다.
    // 이 세션에 열린 스텝이 없으면 새 스텝을 연다.
    public void PlaySpecsInStep(IReadOnlyList<CommandSpecBase> specs, CommandRunScope scope)
    {
        if (scope == null || !ReferenceEquals(scope, _activeScope) || scope.IsCancelled)
        {
            PlaySpecs(specs, scope);
            return;
        }

        List<ISequenceCommand> commands = BuildCommandsFromSpecs(specs);

        // 스텝의 실행이 다 끝나 토큰을 내린 뒤에 덧붙는 경우 — 정지(Stop)가 이 실행도 끊을 수 있게 다시 건다.
        if (_cts == null)
            ResetToken();

        _activeScope.Token = _cts.Token;

        StartStepRun(commands, _runId, _stepId);
    }

    private void StartStepRun(List<ISequenceCommand> commands, int runId, int stepId)
    {
        _activeStepRuns++;

        Coroutine routine = StartCoroutine(RunNode(commands, _activeScope, runId, stepId));

        // 한 프레임 안에 끝난 실행은 null이거나 이미 끝난 코루틴이다 — 멈출 것이 없다.
        if (routine != null)
            _stepRoutines.Add(routine);
    }

    private List<ISequenceCommand> BuildCommandsFromSpecs(IReadOnlyList<CommandSpecBase> specs)
    {
        var list = new List<ISequenceCommand>();

        for (int i = 0; i < specs.Count; i++)
        {
            if (!_factory.TryCreate(specs[i], out ISequenceCommand command))
                continue;

            list.Add(command);
        }

        return list;
    }

    private IEnumerator RunNode(
        List<ISequenceCommand> commands,
        CommandRunScope scope,
        int runId,
        int stepId)
    {
        scope.SetCommandIsPlaying(true);

        try
        {
            yield return _sequencePlayer.PlayCommands(
                commands,
                scope,
                isValid: () => runId == _runId);
        }
        finally
        {
            // 지금 스텝의 실행일 때만 센다 — 늦게 끝난 앞 스텝이 지금 스텝의 토큰·진행 표시를 덮지 않게.
            // 런이 바뀌었다면(Stop) 정리는 Stop이 이미 했다.
            // 스텝의 실행이 모두 끝났을 때 내린다 — 덧붙은 실행이 먼저 끝나도 앞 실행의 전경이 도는 동안은 그대로다.
            if (runId == _runId && stepId == _stepId && --_activeStepRuns <= 0)
            {
                _activeStepRuns = 0;
                _stepRoutines.Clear();
                scope.Token = CancellationToken.None;
                scope.SetCommandIsPlaying(false);
            }
        }
    }

    public void Stop(CleanupPolicy policy)
    {
        _runId++;

        if(policy == CleanupPolicy.Cancel)
            _activeScope?.MarkCancelled();
        
        CancelAndDisposeToken();
        
        for (int i = 0; i < _stepRoutines.Count; i++)
            StopCoroutine(_stepRoutines[i]);

        _stepRoutines.Clear();
        _activeStepRuns = 0;

        _activeScope?.ClearRuntimeState(policy);
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
}