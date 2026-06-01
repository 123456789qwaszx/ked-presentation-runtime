using System.Collections.Generic;
using System.Text;

public sealed class CommandRunTicket
{
    private sealed class CommandEntryInfo
    {
        public readonly int Index;
        public readonly string Name;

        public bool Entered;
        public bool Failed;
        public string FailReason;

        public bool IsSatisfied
        {
            get { return Entered && !Failed; }
        }

        public bool IsClosedButUnsatisfied
        {
            get { return !Entered || Failed; }
        }

        public CommandEntryInfo(int index, string name)
        {
            Index = index;
            Name = string.IsNullOrEmpty(name) ? "<unnamed-command>" : name;
        }
    }

    public readonly int RunId;
    public readonly string Source;
    public readonly int TotalCommands;

    private readonly Dictionary<int, CommandEntryInfo> _entriesByIndex =
        new Dictionary<int, CommandEntryInfo>();

    public int EnteredCommands { get; private set; }
    public int FailedCommands { get; private set; }

    public bool EntryClosed { get; private set; }
    
    public bool WasInterrupted { get; private set; }
    public string InterruptReason { get; private set; }

    public bool HasFailures
    {
        get { return FailedCommands > 0; }
    }

    public bool EntrySatisfied
    {
        get
        {
            return EntryClosed &&
                   EnteredCommands == TotalCommands &&
                   FailedCommands == 0;
        }
    }
    
    public void MarkInterrupted(string reason)
    {
        if (EntryClosed)
            return;

        WasInterrupted = true;
        InterruptReason = reason ?? string.Empty;
    }

    public CommandRunTicket(int runId, string source, int totalCommands)
    {
        RunId = runId;
        Source = source ?? string.Empty;
        TotalCommands = totalCommands;
    }

    public void RegisterExpectedCommand(int index, string commandName)
    {
        if (EntryClosed)
            return;

        if (index < 0)
            return;

        if (_entriesByIndex.ContainsKey(index))
            return;

        _entriesByIndex.Add(index, new CommandEntryInfo(index, commandName));
    }

    public void MarkCommandEntered()
    {
        if (EntryClosed)
            return;

        EnteredCommands++;
    }

    public void MarkCommandEntered(int index)
    {
        if (EntryClosed)
            return;

        CommandEntryInfo entry = GetOrCreateEntry(index, "<unregistered-command>");

        if (entry.Entered)
            return;

        entry.Entered = true;
        EnteredCommands++;
    }

    public void MarkCommandFailed()
    {
        if (EntryClosed)
            return;

        FailedCommands++;
    }

    public void MarkCommandFailed(int index, string reason = null)
    {
        if (EntryClosed)
            return;

        CommandEntryInfo entry = GetOrCreateEntry(index, "<unregistered-command>");

        if (entry.Failed)
            return;

        entry.Failed = true;
        entry.FailReason = reason ?? string.Empty;
        FailedCommands++;
    }

    public void CloseEntry()
    {
        if (EntryClosed)
            return;

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
            $"entrySatisfied={EntrySatisfied}, " +
            $"interrupted={WasInterrupted}, " +
            $"interruptReason={InterruptReason}";
    }

    public string DetailedSnapshot()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(Snapshot());
        AppendUnsatisfiedCommands(sb);

        return sb.ToString();
    }

    public string UnsatisfiedCommandSnapshot()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(Snapshot());
        AppendUnsatisfiedCommands(sb);

        return sb.ToString();
    }

    private void AppendUnsatisfiedCommands(StringBuilder sb)
    {
        if (_entriesByIndex.Count == 0)
        {
            sb.AppendLine("unsatisfiedCommands=<no command debug entries registered>");
            return;
        }

        bool hasAny = false;

        for (int i = 0; i < TotalCommands; i++)
        {
            CommandEntryInfo entry;

            if (!_entriesByIndex.TryGetValue(i, out entry))
            {
                hasAny = true;
                sb.AppendLine($"unsatisfied[{i + 1}/{TotalCommands}]: <not-registered>");
                continue;
            }

            if (!entry.IsClosedButUnsatisfied)
                continue;

            hasAny = true;

            string reason = string.IsNullOrEmpty(entry.FailReason)
                ? ""
                : $", reason={entry.FailReason}";

            sb.AppendLine(
                $"unsatisfied[{entry.Index + 1}/{TotalCommands}]: {entry.Name}, " +
                $"entered={entry.Entered}, failed={entry.Failed}{reason}");
        }

        if (!hasAny)
            sb.AppendLine("unsatisfiedCommands=<none>");
    }

    private CommandEntryInfo GetOrCreateEntry(int index, string commandName)
    {
        CommandEntryInfo entry;

        if (_entriesByIndex.TryGetValue(index, out entry))
            return entry;

        entry = new CommandEntryInfo(index, commandName);
        _entriesByIndex.Add(index, entry);

        return entry;
    }
}