using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// - Per-command exception safety.
/// - Gives every accepted command at least one execution-entry attempt.
/// - Supports non-blocking commands via background coroutines.
/// - Tracks background coroutine handles so Stop() can cancel them reliably.
/// - Closes CommandRunTicket entry in finally so external line barriers never wait forever.
/// </summary>
public sealed class SequencePlayer
{
    private sealed class ActiveBackgroundRoutine
    {
        public IEnumerator Routine;
        public Coroutine Coroutine;
    }

    private readonly MonoBehaviour _host;
    private readonly List<ActiveBackgroundRoutine> _activeBackgroundRoutines = new ();

    public SequencePlayer(MonoBehaviour host)
    {
        _host = host;
    }

    public IEnumerator PlayCommands(
        IReadOnlyList<ISequenceCommand> commands,
        CommandRunScope scope,
        Func<bool> isValid,
        CommandRunTicket ticket)
    {
        int total = commands.Count;

        try
        {
            for (int i = 0; i < total; i++)
            {
                ISequenceCommand command = commands[i];
                IEnumerator routine = command.Execute(scope);

                bool hasMore;
                object firstYield;

                try
                {
                    hasMore = routine.MoveNext();
                    firstYield = hasMore 
                        ? routine.Current 
                        : null;

                    ticket?.MarkCommandEntered();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ticket?.MarkCommandFailed();
                    continue;
                }

                if (!hasMore)
                    continue;

                if (command.WaitForCompletion && scope.ShouldRespectCommandWait)
                {
                    if (isValid())
                        yield return firstYield;

                    while (isValid())
                    {
                        bool movedNext;

                        try
                        {
                            movedNext = routine.MoveNext();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            break;
                        }

                        if (!movedNext)
                            break;

                        yield return routine.Current;
                    }
                }
                else
                {
                    IEnumerator wrappedRoutine = RunBackgroundRoutineToEndAfterFirstYield(
                        routine,
                        firstYield,
                        scope,
                        isValid);

                    ActiveBackgroundRoutine active = new ActiveBackgroundRoutine
                    {
                        Routine = wrappedRoutine,
                        Coroutine = null,
                    };

                    _activeBackgroundRoutines.Add(active);

                    if (command is IStepScopedCommand scopedCommand)
                        scopedCommand.RegisterStepLifetime(scope, _host, wrappedRoutine);

                    active.Coroutine = _host.StartCoroutine(
                        RunTrackedBackgroundRoutine(active, wrappedRoutine));
                }
            }
        }
        finally
        {
            ticket?.CloseEntry();
        }
    }

    public void Stop()
    {
        if (_activeBackgroundRoutines.Count <= 0)
            return;

        ActiveBackgroundRoutine[] snapshot = _activeBackgroundRoutines.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            ActiveBackgroundRoutine active = snapshot[i];
            if (active == null)
                continue;

            if (active.Coroutine != null)
                _host.StopCoroutine(active.Coroutine);
            else if (active.Routine != null)
                _host.StopCoroutine(active.Routine);
        }

        _activeBackgroundRoutines.Clear();
    }

    private IEnumerator RunTrackedBackgroundRoutine(
        ActiveBackgroundRoutine active,
        IEnumerator routine)
    {
        try
        {
            yield return routine;
        }
        finally
        {
            _activeBackgroundRoutines.Remove(active);
        }
    }

    private static IEnumerator RunBackgroundRoutineToEndAfterFirstYield(
        IEnumerator routine,
        object firstYield,
        CommandRunScope scope,
        Func<bool> isValid)
    {
            if (scope == null)
                yield break;

            if (isValid == null || isValid())
            {
                if (!scope.Token.IsCancellationRequested)
                    yield return firstYield;
            }

            while ((isValid == null || isValid()) && !scope.Token.IsCancellationRequested)
            {
                bool movedNext;

                try
                {
                    movedNext = routine.MoveNext();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    yield break;
                }

                if (!movedNext)
                    yield break;

                yield return routine.Current;
            }
        
    }
}