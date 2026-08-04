public enum CommandRunTicketCloseReason
{
    None = 0,

    Completed = 10,
    Cancelled = 20,
    Finished = 30,
    Superseded = 40,

    Faulted = 900,
}


// EntryClosed does not mean that every visual effect has completed.
// It means the command batch has finished its entry phase and the caller no longer needs to block.
//
// Each command is responsible for committing its own entry result to this ticket
// when its entry run completes, fails, or is interrupted.
// Background commands may continue afterward under their registered lifetime scope.
public sealed class CommandRunTicket
{
    private readonly int _totalCount;
    private int _enteredCount;
    private int _failedCount;

    public bool EntryClosed { get; private set; }
    public CommandRunTicketCloseReason CloseReason { get; private set; } =
        CommandRunTicketCloseReason.None;


    public bool EntryCompletedSuccessfully =>
        EntryClosed &&
        _enteredCount == _totalCount &&
        _failedCount == 0 &&
        CloseReason == CommandRunTicketCloseReason.Completed;

    public bool EntryInterruptedNormally =>
        EntryClosed &&
        _failedCount == 0 &&
        _enteredCount < _totalCount &&
        (
            CloseReason == CommandRunTicketCloseReason.Cancelled ||
            CloseReason == CommandRunTicketCloseReason.Superseded ||
            CloseReason == CommandRunTicketCloseReason.Finished
        );

    public bool EntryFailed =>
        EntryClosed &&
        (
            _failedCount > 0 ||
            CloseReason == CommandRunTicketCloseReason.Faulted
        );

    public bool EntryClosedUnexpectedly =>
        EntryClosed &&
        !EntryCompletedSuccessfully &&
        !EntryInterruptedNormally &&
        !EntryFailed;

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

    public void CloseEntry(CommandRunTicketCloseReason reason)
    {
        if (EntryClosed)
            return;

        CloseReason = reason;
        EntryClosed = true;
    }

    public string ToDebugString()
    {
        return $"entered={_enteredCount}/{_totalCount}, failed={_failedCount}, reason={CloseReason}";
    }
}