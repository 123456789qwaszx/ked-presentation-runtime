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

public sealed class VNLineEntryCommitter
{
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly LineCommandEntryGate _commandEntryGate;

    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackController _rollbackController;
    private readonly VNRuntimeStateProvider _runtimeStateProvider;

    public VNLineEntryCommitter(
        YarnBridgePlaybackDriver playbackDriver,
        LineCommandEntryGate commandEntryGate,
        BacklogRecorder backlogRecorder,
        RollbackController rollbackController,
        VNRuntimeStateProvider runtimeStateProvider)
    {
        _playbackDriver = playbackDriver;
        _commandEntryGate = commandEntryGate;
        _backlogRecorder = backlogRecorder;
        _rollbackController = rollbackController;
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

    public void CommitLineEntered(YarnLineMeta meta)
    {
        _runtimeStateProvider.UpdateCurrentLineMeta(meta);

        _backlogRecorder.Record(meta);
        _rollbackController.AddRollbackPoint(meta);
        
        CommandRunTicket ticket = _playbackDriver.PlayCollected();
        _commandEntryGate.Register(ticket);
    }
}