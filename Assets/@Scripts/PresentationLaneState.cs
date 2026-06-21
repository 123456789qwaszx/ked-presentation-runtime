using Yarn.Unity;

/// <summary>
/// Side presentation lane의 런타임 상태.
/// 
/// 이 클래스는 lane의 phase/gate/pause/settle clock을 가진다.
/// 실제 token 소비 판단은 SyncGateAdvancer가 한다.
/// </summary>
public sealed class PresentationLaneState
{
    private DialogueRunner _runner;

    private int _runVersion;
    private PresentationLanePhase _phase = PresentationLanePhase.Stopped;
    private PresentationLaneGate _gate = PresentationLaneGate.Blocked;
    private bool _isPaused;

    private readonly ForwardSettleClock _settleClock = new();

    public PresentationLaneRunToken CurrentRun => new(_runVersion);

    public int ForwardSettleEpoch => _settleClock.Epoch;
    public bool IsPaused => _isPaused;

    public bool IsCompleted => _phase == PresentationLanePhase.Completed;
    public bool IsAvailable => _phase == PresentationLanePhase.Running; 
    public bool IsReadyForAdvance =>IsAvailable && _gate == PresentationLaneGate.Ready; 


    public bool IsOpenForMain
    {
        get
        {
            if (!IsAvailable)
                return false;

            return _gate == PresentationLaneGate.Ready ||
                   _gate == PresentationLaneGate.Released;
        }
    }

    public bool IsBlockingMainFlow
    {
        get
        {
            if (!IsAvailable)
                return false;

            return _gate == PresentationLaneGate.Blocked;
        }
    }

    public bool IsDialogueRunning
    {
        get { return _runner.IsDialogueRunning; }
    }

    public bool CanReceiveScriptedAdvance
    {
        get { return IsAvailable && !_isPaused; }
    }

    public bool CanReceiveSeekResyncAdvance
    {
        get { return IsAvailable && !_isPaused; }
    }

    public bool CanReceiveManualAdvance
    {
        get { return IsAvailable; }
    }

    public bool CanReceiveForwardModifier
    {
        get { return IsAvailable; }
    }

    public void Register(DialogueRunner runner)
    {
        _runner = runner;
    }

    public YarnTask StartDialogue(string nodeName)
    {
        return _runner.StartDialogue(nodeName);
    }

    public YarnTask StopDialogue()
    {
        return _runner.Stop();
    }

    public void RequestNextLine()
    {
        _runner.RequestNextLine();
    }

    public void BeginRun()
    {
        _runVersion++;

        _phase = PresentationLanePhase.Running;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void CompleteRun()
    {
        _phase = PresentationLanePhase.Completed;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void ResetAll()
    {
        _runVersion++;

        _phase = PresentationLanePhase.Stopped;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void ClearForDeterministicReplay()
    {
        _runVersion++;

        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        // Epoch은 유지한다.
        // 이전 재실행에서 dispatch되었지만 아직 settle되지 않은 in-flight settle만 폐기한다.
        _settleClock.ClearInFlightSettles();
    }

    public void NotifyReady(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _gate = PresentationLaneGate.Ready;
    }

    public void NotifyNotReady(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (_phase == PresentationLanePhase.Completed)
            return;

        _gate = PresentationLaneGate.Blocked;
    }

    public void NotifyReleased(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _gate = PresentationLaneGate.Released;
    }

    public void NotifyCompleted(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        CompleteRun();
    }

    public void NotifyForwardSettled(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _settleClock.NotifySettled();
    }

    public void MarkAdvanceDispatched(SyncGateToken token)
    {
        _gate = PresentationLaneGate.Blocked;

        if (token.CountsForForwardSettle)
            _settleClock.BeginForwardSettle();
    }

    public void Pause()
    {
        if (!IsAvailable)
            return;

        _isPaused = true;
    }

    public void Resume()
    {
        if (!IsAvailable)
            return;

        _isPaused = false;
    }

    private bool IsCurrent(PresentationLaneRunToken run)
    {
        return run.Version == _runVersion;
    }
}