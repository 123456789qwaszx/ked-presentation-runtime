public sealed class VNLinePresentationState
{
    private readonly VNTraceStream _trace;

    public bool IsFullyShown { get; private set; } = true;

    public VNLinePresentationState(VNTraceStream trace = null)
    {
        _trace = trace;
    }

    public void MarkLineEntered()
    {
        IsFullyShown = false;
        Trace("MarkLineEntered");
    }

    public void MarkLineDisplayCompleted()
    {
        IsFullyShown = true;
        Trace("MarkLineDisplayCompleted");
    }

    public void Reset()
    {
        IsFullyShown = true;
        Trace("Reset");
    }

    public string Snapshot()
    {
        return $"lineFullyShown={IsFullyShown}";
    }

    private void Trace(string evt)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNLinePresentationState), evt, Snapshot());
    }
}