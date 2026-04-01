using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Yarn <<command>> 에서 즉시 단발 커맨드를 실행하는 런너.
/// PresentationSession/Step 라이프타임과 무관하게 독립 실행.
/// </summary>
public sealed class ImmediateCommandRunner : MonoBehaviour
{
    private INodeCommandFactory _charRigFactory;
    private INodeCommandFactory _signalFactory;

    private CommandRunScope _scope;
    private CancellationTokenSource _cts;
    private PresentationSessionContext _context;

    private sealed class ActiveRun
    {
        public string roleKey;
        public CommandBase commandBase;
        public Coroutine coroutine;
    }

    private readonly Dictionary<string, List<ActiveRun>> _activeByRole = new();

    public void Initialize(
        INodeCommandFactory charRigFactory,
        INodeCommandFactory signalFactory,
        PresentationPlaybackSettings modes)
    {
        _charRigFactory = charRigFactory;
        _signalFactory = signalFactory;
        _context = new PresentationSessionContext(modes);

        _cts = new CancellationTokenSource();
        _scope = new CommandRunScope(_context);
    }

    public Coroutine Run(CommandSpecBase spec, bool blocking = false)
    {
        if (!TryCreateCommand(spec, out ISequenceCommand command))
        {
            Debug.LogWarning($"[ImmediateRunner] Factory 실패: {spec?.GetType().Name}");
            return null;
        }

        string roleKey = null;
        if (spec is CharRigCommandSpecBase charSpec)
            roleKey = charSpec.roleKey;

        if (!string.IsNullOrWhiteSpace(roleKey))
            InterruptRole(roleKey);

        if (blocking || command.WaitForCompletion)
        {
            var active = new ActiveRun
            {
                roleKey = roleKey,
                commandBase = command as CommandBase
            };

            Coroutine handle = StartCoroutine(RunBlockingRoutine(command, active));
            active.coroutine = handle;

            Register(active);
            return handle;
        }
        else
        {
            return StartCoroutine(LaunchNonBlockingRoutine(command, roleKey));
        }
    }

    private IEnumerator LaunchNonBlockingRoutine(ISequenceCommand command, string roleKey)
    {
        IEnumerator routine;
        try
        {
            routine = command.Execute(_scope);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            yield break;
        }

        if (routine == null)
            yield break;

        var active = new ActiveRun
        {
            roleKey = roleKey,
            commandBase = command as CommandBase
        };

        Coroutine handle = StartCoroutine(RunBackgroundRoutine(routine, active));
        active.coroutine = handle;

        Register(active);
    }

    private IEnumerator RunBlockingRoutine(ISequenceCommand command, ActiveRun active)
    {
        IEnumerator routine;
        try
        {
            routine = command.Execute(_scope);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            Unregister(active);
            yield break;
        }

        if (routine == null)
        {
            Unregister(active);
            yield break;
        }

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                bool movedNext;
                try
                {
                    movedNext = routine.MoveNext();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    yield break;
                }

                if (!movedNext)
                    yield break;

                yield return routine.Current;
            }
        }
        finally
        {
            Unregister(active);
        }
    }

    private IEnumerator RunBackgroundRoutine(IEnumerator routine, ActiveRun active)
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                bool movedNext;
                try
                {
                    movedNext = routine.MoveNext();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    yield break;
                }

                if (!movedNext)
                    yield break;

                yield return routine.Current;
            }
        }
        finally
        {
            Unregister(active);
        }
    }

    private void InterruptRole(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            return;

        if (!_activeByRole.TryGetValue(roleKey, out var list) || list == null || list.Count == 0)
            return;

        ActiveRun[] snapshot = list.ToArray();
        _activeByRole.Remove(roleKey);

        for (int i = 0; i < snapshot.Length; i++)
        {
            ActiveRun run = snapshot[i];
            if (run == null)
                continue;

            if (run.coroutine != null)
                StopCoroutine(run.coroutine);

            run.commandBase?.CompleteNow(_scope);
        }
    }

    private void Register(ActiveRun active)
    {
        if (active == null || string.IsNullOrWhiteSpace(active.roleKey))
            return;

        if (!_activeByRole.TryGetValue(active.roleKey, out var list))
        {
            list = new List<ActiveRun>();
            _activeByRole.Add(active.roleKey, list);
        }

        list.Add(active);
    }

    private void Unregister(ActiveRun active)
    {
        if (active == null || string.IsNullOrWhiteSpace(active.roleKey))
            return;

        if (!_activeByRole.TryGetValue(active.roleKey, out var list) || list == null)
            return;

        list.Remove(active);

        if (list.Count == 0)
            _activeByRole.Remove(active.roleKey);
    }

    private bool TryCreateCommand(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = null;
        if (spec == null)
            return false;

        if (spec is CharRigCommandSpecBase)
            return _charRigFactory.TryCreate(spec, out command);

        return _signalFactory.TryCreate(spec, out command);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();

        foreach (var pair in _activeByRole)
        {
            var list = pair.Value;
            if (list == null)
                continue;

            for (int i = 0; i < list.Count; i++)
            {
                var run = list[i];
                if (run == null)
                    continue;

                if (run.coroutine != null)
                    StopCoroutine(run.coroutine);

                run.commandBase?.CompleteNow(_scope);
            }
        }

        _activeByRole.Clear();

        _cts?.Dispose();
    }
}