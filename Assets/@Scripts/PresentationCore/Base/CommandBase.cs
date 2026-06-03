using System.Collections;
using UnityEngine;

// Cleanup at the STEP boundary (when the next step/line starts) — the shorter, default lifetime.
// CommandBase implements this, so every command is step-scoped by default: a background routine
// is cleaned at the next step unless it opts into a longer lifetime.
public interface IStepScopedCommand
{
    void RegisterStepLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine);
}

// Opt-in marker for background commands that must OUTLIVE a single step and live until the RUN
// ends (session/episode) — e.g., BGM, ambient/idle loops. Overrides the step-scoped default.
public interface IRunScopedCommand
{
    void RegisterRunLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine);
}

// Background (WaitForCompletion == false) lifetime model:
//   - Step-scoped by DEFAULT: CommandBase implements IStepScopedCommand, so a background routine
//     is cleaned at the next step boundary unless it opts out. Matches the common case —
//     transient effects shouldn't survive into the next step.
//   - Run-scoped by OPT-IN: a command that must live until the run ends additionally declares
//     ': IRunScopedCommand'. SequencePlayer checks Run before Step, so the opt-in wins.
//     (RegisterRunLifetime below is the inherited default impl for that opt-in.)
public abstract class CommandBase : ISequenceCommand, IRunScopedCommand
{
    // Ignore: drop trivial VFX/SFX/shakes on skip.
    // ExecuteEvenIfSkipping: must still run (text/log/signals).
    protected virtual SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    // If true, SequencePlayer waits for this command before moving on.
    // If false, it runs in the background (fire-and-forget). Cleanup is bound to a lifetime:
    // step by default (via IStepScopedCommand on CommandBase), or run if it opts into IRunScopedCommand.
    public virtual bool WaitForCompletion => false;
    
    public IEnumerator Execute(CommandRunScope scope)
    {
        if (scope.Token.IsCancellationRequested)
            yield break;

        if (scope.IsSkipping)
        {
            switch (SkipPolicy)
            {
                case SkipPolicy.Ignore:
                    yield break;

                case SkipPolicy.CompleteImmediately:
                    try
                    {
                        OnSkip(scope);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                    yield break;

                case SkipPolicy.ExecuteEvenIfSkipping:
                    break;
            }
        }

        if (scope.IsRollbackSeeking)
        {
            OnRollbackSeek(scope);
            yield break;
        }

        IEnumerator inner = null;
        try
        {
            inner = ExecuteInner(scope);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            yield break;
        }

        if (inner != null) yield return inner;
    }

    protected abstract IEnumerator ExecuteInner(CommandRunScope scope);

    protected virtual void OnSkip(CommandRunScope scope)
    {
    }

    protected virtual void OnRollbackSeek(CommandRunScope scope)
    {
    }
    
    protected IEnumerator Wait(CommandRunScope scope, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (scope.Token.IsCancellationRequested)
                yield break;

            if (scope.IsSkipping)
            {
                if (SkipPolicy == SkipPolicy.CompleteImmediately)
                {
                    try
                    {
                        OnSkip(scope);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                yield break;
            }

            elapsed += Time.unscaledDeltaTime * scope.TimeScale;

            yield return null;
        }
    }

    
    
    // Default (step) binding: every command gets this. Routine is stopped — and OnCommandCompleted
    // fired on Finish — at the next step boundary.
    public virtual void RegisterStepLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine)
    {
        scope.TrackStep(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);
                
                OnCommandCompleted(scope);
            });
    }
    
    
    // Opt-in (run) binding for IRunScopedCommand: same shape as step, but cleaned at run end
    // instead of the next step boundary. Use for work that must persist across steps.
    public virtual void RegisterRunLifetime(CommandRunScope scope, MonoBehaviour host, IEnumerator routine)
    {
        scope.TrackRun(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                OnCommandCompleted(scope);
            });
    }
    
    // Completion hook tied to step cleanup (CleanupPolicy.Finish).
    // Called when the step finishes (normal end / finish-all), not on Cancel-only cleanup.
    protected virtual void OnCommandCompleted(CommandRunScope scope) { }
}