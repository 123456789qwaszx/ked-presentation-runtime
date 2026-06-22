using Yarn.Unity;

public sealed class PresentationLaneState
{
    private readonly DialogueRunner _runner;

    private int _runVersion;
    private bool _isRunning;
    private PresentationLaneGate _gate = PresentationLaneGate.Blocked;
    private bool _isPaused;

    private readonly ForwardSettleClock _settleClock = new();

    public PresentationLaneRunToken CurrentRun => new(_runVersion);

    public int ForwardSettleEpoch => _settleClock.Epoch;
    public bool IsPaused => _isPaused;

    public bool IsAvailable => _isRunning;

    public bool IsReadyForAdvance =>
        IsAvailable && _gate == PresentationLaneGate.Ready;

    public bool IsOpenForMain =>
        IsAvailable && _gate is PresentationLaneGate.Ready or PresentationLaneGate.Released;

    public bool IsDialogueRunning => _runner.IsDialogueRunning;

    public bool CanReceiveScriptedAdvance => IsAvailable && !_isPaused;
    public bool CanReceiveSeekResyncAdvance => IsAvailable && !_isPaused;
    public bool CanReceiveForwardModifier => IsAvailable;

    public PresentationLaneState(DialogueRunner runner)
    {
        _runner = runner;
    }

    public YarnTask StartDialogue(string nodeName) => _runner.StartDialogue(nodeName);
    public YarnTask StopDialogue() => _runner.Stop();
    public void RequestNextLine() => _runner.RequestNextLine();

    public void BeginRun()
    {
        _runVersion++;

        _isRunning = true;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void CompleteRun()
    {
        _isRunning = false;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void ResetAll()
    {
        _runVersion++;

        _isRunning = false;
        _gate = PresentationLaneGate.Blocked;
        _isPaused = false;

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

        if (!IsAvailable)
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

    public bool IsCurrent(PresentationLaneRunToken run)
    {
        return run.Version == _runVersion;
    }
}