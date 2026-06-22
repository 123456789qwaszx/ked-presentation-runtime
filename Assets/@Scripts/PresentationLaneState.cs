using Yarn.Unity;

public sealed class PresentationLaneState
{
    private enum Gate
    {
        // Side lane is inside a line or not ready to receive RequestNextLine.
        // Main must wait.
        Blocked,

        // Side lane is waiting at a stable line boundary.
        // Main may dispatch RequestNextLine.
        Ready,

        // Side lane was torn down before reaching the normal ready boundary.
        // Main wait may be released, but RequestNextLine must not be dispatched.
        Released,
    }

    private readonly DialogueRunner _runner;
    private readonly ForwardSettleClock _settleClock = new();

    private int _runVersion;
    private bool _isRunning;
    private Gate _gate = Gate.Blocked;
    private bool _isPaused;

    public PresentationLaneRunToken CurrentRun => new(_runVersion);

    public bool IsAvailable => _isRunning;
    public bool IsDialogueRunning => _runner.IsDialogueRunning;

    public int ForwardSettleEpoch => _settleClock.Epoch;

    public bool IsReadyForAdvance => 
        IsAvailable && _gate == Gate.Ready;
    
    public bool IsOpenForMain => 
        IsAvailable && (_gate == Gate.Ready || _gate == Gate.Released);

    public bool CanReceiveScriptedAdvance => IsAvailable && !_isPaused;
    public bool CanReceiveSeekResyncAdvance => IsAvailable && !_isPaused;
    public bool CanReceiveForwardModifier => IsAvailable;

    public PresentationLaneState(DialogueRunner runner)
    {
        _runner = runner;
    }

    public void StartDialogue(string nodeName) => _runner.StartDialogue(nodeName);
    public YarnTask StopDialogue() => _runner.Stop();
    public void RequestNextLine() => _runner.RequestNextLine();

    public void BeginRun()
    {
        _runVersion++;

        _isRunning = true;
        _gate = Gate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void CompleteRun()
    {
        _isRunning = false;
        _gate = Gate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void ResetAll()
    {
        _runVersion++;

        _isRunning = false;
        _gate = Gate.Blocked;
        _isPaused = false;

        _settleClock.ClearInFlightSettles();
    }

    public void NotifyReady(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _gate = Gate.Ready;
    }

    public void NotifyNotReady(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _gate = Gate.Blocked;
    }

    public void NotifyReleased(PresentationLaneRunToken run)
    {
        if (!IsCurrent(run))
            return;

        if (!IsAvailable)
            return;

        _gate = Gate.Released;
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

    public void MarkAdvanceDispatched(SyncAdvanceKind advanceKind)
    {
        _gate = Gate.Blocked;
        
        if (advanceKind == SyncAdvanceKind.ScriptedForward)
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