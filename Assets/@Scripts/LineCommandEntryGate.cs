using System.Collections;

public sealed class LineCommandEntryGate
{
    private readonly LineCommandEntryBarrier _barrier;
    private readonly DialogueAdvanceDispatcher _dispatcher;

    public LineCommandEntryGate(LineCommandEntryBarrier barrier, DialogueAdvanceDispatcher dispatcher)
    {
        _barrier = barrier;
        _dispatcher = dispatcher;
    }

    public void Register(CommandRunTicket ticket)
    {
        if (ticket == null)
            return;

        _barrier.Register(ticket);
    }

    public bool CanDispatch => _barrier.IsEntryClosed; 

    public void DispatchSeekNextIfReady()
    {
        if (!_barrier.IsEntryClosed)
            return;

        _dispatcher.DispatchSeekNext();
    }

    public IEnumerator WaitAndDispatchSeekNext()
    {
        while (!_barrier.IsEntryClosed)
            yield return null;

        _dispatcher.DispatchSeekNext();
    }
}