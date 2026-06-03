using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 모든 커맨드에 최소 한 번의 실행 진입을 보장
// rm 직후 ticket?.MarkCommandEntered()로 "이 커맨드는 진입했다"를 기록
public sealed class SequencePlayer
{
    private readonly MonoBehaviour _host;

    public SequencePlayer(MonoBehaviour host)
    {
        _host = host;
    }

    public IEnumerator PlayCommands(
        IReadOnlyList<ISequenceCommand> commands, CommandRunScope scope, Func<bool> isValid, CommandRunTicket ticket)
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
                    // ---- Non-blocking commands ----
                    // These still run even after PlayCommands yield break
                    IEnumerator wrappedRoutine = RunBackgroundRoutineToEndAfterFirstYield(
                        routine,
                        firstYield,
                        scope,
                        isValid);

                    // Non-blocking commands can bind themselves to step lifetime.
                    if (command is IStepScopedCommand scopedCommand)
                        scopedCommand.RegisterStepLifetime(scope, _host, wrappedRoutine);

                    _host.StartCoroutine(wrappedRoutine);
                }
            }
        }
        finally
        {
            ticket?.CloseEntry();
        }
    }

    private static IEnumerator RunBackgroundRoutineToEndAfterFirstYield(
        IEnumerator routine, object firstYield, CommandRunScope scope, Func<bool> isValid)
    {
        if (isValid())
            yield return firstYield;

        while (isValid() && !scope.Token.IsCancellationRequested)
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