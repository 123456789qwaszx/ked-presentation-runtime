using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// - Per-command exception safety.
/// - Gives every accepted command at least one execution-entry attempt.
/// - Supports non-blocking commands via background coroutines.
/// - Tracks background coroutine handles so Stop() can cancel them reliably.
/// </summary>
public sealed class SequencePlayer
{
    private sealed class ActiveBackgroundRoutine
    {
        public IEnumerator Routine;
        public Coroutine Coroutine;
        public string DebugName;
    }

    private readonly MonoBehaviour _host;
    private readonly List<ActiveBackgroundRoutine> _activeBackgroundRoutines =
        new List<ActiveBackgroundRoutine>();

    public SequencePlayer(MonoBehaviour host)
    {
        if (host == null)
            throw new ArgumentNullException(nameof(host));

        _host = host;
    }

    public IEnumerator PlayCommands(
        IReadOnlyList<ISequenceCommand> commands,
        CommandRunScope scope,
        int runId,
        Func<bool> isValid,
        CommandRunTicket ticket,
        Action<string> trace = null)
    {
        bool Valid()
        {
            return isValid == null || isValid();
        }

        void Trace(string s)
        {
            if (trace != null)
                trace(s);
        }

        int total = commands != null ? commands.Count : 0;
        Trace($"[run:{runId}] PlayCommands begin (count={total})");

        if (commands == null)
        {
            ticket?.CloseEntry();
            Trace($"[run:{runId}] PlayCommands end: commands null");
            yield break;
        }

        for (int i = 0; i < total; i++)
        {
            string tag = $"[run:{runId}][{i + 1}/{total}]";

            if (!Valid())
            {
                Trace($"{tag} Entry stopped: invalid run.");
                break;
            }

            if (scope == null)
            {
                Trace($"{tag} Entry stopped: scope is null.");
                break;
            }

            if (scope.Token.IsCancellationRequested)
            {
                Trace($"{tag} Entry stopped: token cancelled before command entry.");
                break;
            }

            ISequenceCommand command = commands[i];
            if (command == null)
            {
                Trace($"{tag} Command is null.");
                ticket?.MarkCommandFailed();
                continue;
            }

            string name = GetDebugName(command);

            IEnumerator routine;
            try
            {
                routine = command.Execute(scope);
            }
            catch (Exception e)
            {
                Trace($"{tag} Exception in Execute(): {name}");
                Debug.LogException(e);
                ticket?.MarkCommandFailed();
                continue;
            }

            if (routine == null)
            {
                Trace($"{tag} Execute() returned null: {name}");
                ticket?.MarkCommandFailed();
                continue;
            }

            bool hasMore;
            object firstYield;

            try
            {
                hasMore = routine.MoveNext();
                firstYield = hasMore ? routine.Current : null;

                ticket?.MarkCommandEntered();
                Trace($"{tag} Entered: {name}");
            }
            catch (Exception e)
            {
                Trace($"{tag} Exception on first MoveNext(): {name}");
                Debug.LogException(e);
                ticket?.MarkCommandFailed();
                continue;
            }

            if (!hasMore)
            {
                Trace($"{tag} Completed on entry: {name}");
                continue;
            }

            if (command.WaitForCompletion && scope.ShouldRespectCommandWait)
            {
                if (Valid() && !scope.Token.IsCancellationRequested)
                    yield return firstYield;

                while (Valid() && !scope.Token.IsCancellationRequested)
                {
                    bool movedNext;

                    try
                    {
                        movedNext = routine.MoveNext();
                    }
                    catch (Exception e)
                    {
                        Trace($"{tag} Exception while running: {name}");
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
                    Valid,
                    onFinished: null);

                ActiveBackgroundRoutine active = new ActiveBackgroundRoutine
                {
                    Routine = wrappedRoutine,
                    Coroutine = null,
                    DebugName = name,
                };

                _activeBackgroundRoutines.Add(active);

                if (command is IStepScopedCommand scopedCommand)
                    scopedCommand.RegisterStepLifetime(scope, _host, wrappedRoutine);

                active.Coroutine = _host.StartCoroutine(
                    RunTrackedBackgroundRoutine(active, wrappedRoutine));
            }
        }

        ticket?.CloseEntry();

        if (ticket != null)
        {
            if (ticket.EntrySatisfied)
                Trace($"[run:{runId}] CommandEntrySatisfied: {ticket.Snapshot()}");
            else
                Trace($"[run:{runId}] CommandEntryFailed: {ticket.Snapshot()}");
        }

        Trace($"[run:{runId}] PlayCommands end");
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
        Func<bool> isValid,
        Action onFinished)
    {
        try
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
        finally
        {
            if (onFinished != null)
                onFinished();
        }
    }

    private static string GetDebugName(ISequenceCommand command)
    {
        if (command is CommandBase commandBase)
            return commandBase.DebugName;

        return command.GetType().Name;
    }
}