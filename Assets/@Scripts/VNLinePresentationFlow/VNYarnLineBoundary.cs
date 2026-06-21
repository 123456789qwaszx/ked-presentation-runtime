using Yarn.Unity;

public readonly struct YarnLineMeta
{
    public readonly string nodeName;
    public readonly string lineId;
    public readonly string charName;
    public readonly string rawText;

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}

public sealed class VNYarnLineBoundary
{
    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackHistory _rollbackHistory;
    private readonly VNRuntimeStateProvider _runtimeStateProvider;

    public VNYarnLineBoundary(
        BacklogRecorder backlogRecorder,
        RollbackHistory rollbackHistory,
        VNRuntimeStateProvider runtimeStateProvider)
    {
        _backlogRecorder = backlogRecorder;
        _rollbackHistory = rollbackHistory;
        _runtimeStateProvider = runtimeStateProvider;
    }

    public YarnLineMeta BuildLineMeta(LocalizedLine line, string nodeName)
    {
        return new YarnLineMeta(
            nodeName,
            line.TextID,
            line.CharacterName,
            line.TextWithoutCharacterName.Text);
    }

    public void CommitLineEntered(YarnLineMeta meta, bool recordToHistory)
    {
        _runtimeStateProvider.UpdateCurrentLineMeta(meta);

        if (!recordToHistory)
            return;

        _backlogRecorder.Record(meta);
        _rollbackHistory.AddRollbackPoint(meta);
    }
}