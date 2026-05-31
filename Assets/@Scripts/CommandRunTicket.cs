public sealed class CommandRunTicket
{
    public readonly int RunId;
    public readonly string Source;
    public readonly int TotalCommands;

    public int EnteredCommands { get; private set; }
    public int FailedCommands { get; private set; }

    public bool EntryClosed { get; private set; }

    public bool EntrySatisfied
    {
        get { return EntryClosed && EnteredCommands == TotalCommands && FailedCommands == 0; }
    }

    public CommandRunTicket(int runId, string source, int totalCommands)
    {
        RunId = runId;
        Source = source ?? string.Empty;
        TotalCommands = totalCommands;
    }

    public void MarkCommandEntered()
    {
        EnteredCommands++;
    }

    public void MarkCommandFailed()
    {
        FailedCommands++;
    }

    public void CloseEntry()
    {
        EntryClosed = true;
    }

    public string Snapshot()
    {
        return
            $"runId={RunId}, " +
            $"source={Source}, " +
            $"entered={EnteredCommands}, " +
            $"failed={FailedCommands}, " +
            $"total={TotalCommands}, " +
            $"entryClosed={EntryClosed}, " +
            $"entrySatisfied={EntrySatisfied}";
    }
}