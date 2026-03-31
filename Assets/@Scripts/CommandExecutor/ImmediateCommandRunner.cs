using System.Collections;
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

    // 즉시 실행용 scope — 라이프타임은 이 MonoBehaviour에 종속
    private CommandRunScope _scope;
    private CancellationTokenSource _cts;
    private PresentationSessionContext _context;

    public void Initialize(
        INodeCommandFactory charRigFactory,
        INodeCommandFactory signalFactory,
        PresentationPlaybackSettings modes)
    {
        _charRigFactory = charRigFactory;
        _signalFactory  = signalFactory;
        _context = new PresentationSessionContext( modes );

        _cts   = new CancellationTokenSource();
        _scope = new CommandRunScope(_context);
    }

    /// <summary>
    /// Spec를 받아서 Command를 만들고 즉시 코루틴 실행.
    /// blocking=true면 Yarn이 완료까지 기다림 (YarnTask 반환).
    /// </summary>
    public Coroutine Run(CommandSpecBase spec, bool blocking = false)
    {
        if (!TryCreateCommand(spec, out ISequenceCommand command))
        {
            Debug.LogWarning($"[ImmediateRunner] Factory 실패: {spec?.GetType().Name}");
            return null;
        }

        return StartCoroutine(RunRoutine(command, blocking));
    }

    private IEnumerator RunRoutine(ISequenceCommand command, bool blocking)
    {
        IEnumerator routine;
        try { routine = command.Execute(_scope); }
        catch (System.Exception e) { Debug.LogException(e); yield break; }

        if (routine == null) yield break;

        if (blocking || command.WaitForCompletion)
        {
            while (!_cts.IsCancellationRequested)
            {
                bool movedNext;
                try { movedNext = routine.MoveNext(); }
                catch (System.Exception e) { Debug.LogException(e); yield break; }
                if (!movedNext) yield break;
                yield return routine.Current;
            }
        }
        else
        {
            StartCoroutine(routine);
        }
    }

    private bool TryCreateCommand(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = null;
        if (spec == null) return false;

        if (spec is CharRigCommandSpecBase)
            return _charRigFactory.TryCreate(spec, out command);

        return _signalFactory.TryCreate(spec, out command);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}