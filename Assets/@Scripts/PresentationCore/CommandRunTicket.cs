public sealed class CommandRunTicket
{
    private readonly int _totalCount;
    private int _enteredCount;
    private int _failedCount;

    public bool EntryClosed { get; private set; } = false;

    public bool EntrySatisfied => EntryClosed && _enteredCount == _totalCount && _failedCount == 0;
    
    public CommandRunTicket(int totalCommands)
    {
        _totalCount = totalCommands;
    }

    public void MarkCommandEntered()
    {
        if (EntryClosed)
            return;
        
        _enteredCount++;
    }
    
    public void MarkCommandFailed()
    {
        if (EntryClosed)
            return;
        
        _failedCount++;
    }

    public void CloseEntry()
    {
        EntryClosed = true; 
    }
}