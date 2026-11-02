using Yarn.Unity;

public readonly struct YarnLineMeta
{
    public readonly string lineId;
    public readonly string charName;
    public readonly string rawText;

    public YarnLineMeta(string lineId, string charName, string rawText)
    {
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}

public sealed class YarnLineBoundary
{
    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackHistory _rollbackHistory;
    private readonly RuntimeStateProvider _runtimeStateProvider;

    public YarnLineBoundary(
        BacklogRecorder backlogRecorder,
        RollbackHistory rollbackHistory,
        RuntimeStateProvider runtimeStateProvider)
    {
        _backlogRecorder = backlogRecorder;
        _rollbackHistory = rollbackHistory;
        _runtimeStateProvider = runtimeStateProvider;
    }

    public YarnLineMeta BuildLineMeta(LocalizedLine line)
    {
        return new YarnLineMeta(
            line.TextID,
            line.CharacterName,
            line.TextWithoutCharacterName.Text);
    }

    public void CommitLineEntered(YarnLineMeta meta, LineRecordDecision decision)
    {
        _runtimeStateProvider.UpdateCurrentLineMeta(meta);

        if (decision.RecordBacklog)
            _backlogRecorder.Record(meta);

        if (decision.AddRollbackPoint)
            _rollbackHistory.AddRollbackPoint(meta);
    }
}