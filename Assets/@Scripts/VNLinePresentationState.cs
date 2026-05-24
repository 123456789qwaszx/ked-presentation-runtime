public sealed class VNLinePresentationState
{
    public bool IsFullyShown { get; private set; } = true;

    public void MarkLineEntered()
    {
        IsFullyShown = false;
    }

    public void MarkLineDisplayCompleted()
    {
        IsFullyShown = true;
    }

    public void Reset()
    {
        IsFullyShown = true;
    }
}