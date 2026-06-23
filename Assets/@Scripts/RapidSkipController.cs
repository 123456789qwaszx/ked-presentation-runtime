public sealed class RapidSkipController
{
    private readonly DialogueAdvanceDispatcher _dispatcher;

    private bool _isHeld;

    public RapidSkipController(DialogueAdvanceDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void SetHeld(bool held)
    {
        _isHeld = held;
    }

    public void Tick()
    {
        if (!_isHeld)
            return;

        _dispatcher.DispatchRapidSkipAdvance();
    }
}