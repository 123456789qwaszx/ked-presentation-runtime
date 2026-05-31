using System;
using Yarn.Unity;

[Serializable]
public struct YarnLineMeta
{
    public string nodeName;
    public string lineId;
    public string charName;
    public string rawText;

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}

// 라인 처리 중 발생하는 도메인 커밋을 전담한다.
// 시각적 작업(Box, Typewriter)은 포함하지 않는다.
public sealed class VNLinePresentationCommitter
{
    private readonly VNLinePresentationState _advanceState;
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly LineCommandEntryGate _commandEntryGate;

    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackController _rollbackController;
    private readonly VNRuntimeStateProvider _runtimeStateProvider;

    public VNLinePresentationCommitter(
        VNLinePresentationState advanceState,
        YarnBridgePlaybackDriver playbackDriver,
        LineCommandEntryGate commandEntryGate,
        BacklogRecorder backlogRecorder,
        RollbackController rollbackController,
        VNRuntimeStateProvider runtimeStateProvider)
    {
        _advanceState = advanceState;
        _playbackDriver = playbackDriver;
        _commandEntryGate = commandEntryGate;
        _backlogRecorder = backlogRecorder;
        _rollbackController = rollbackController;
        _runtimeStateProvider = runtimeStateProvider;
    }

    public YarnLineMeta CommitLineEntered(LocalizedLine line, string nodeName)
    {
        YarnLineMeta meta = new YarnLineMeta(
            nodeName,
            line.TextID,
            line.CharacterName,
            line.TextWithoutCharacterName.Text);

        _runtimeStateProvider.HandleLineEntered(meta);
        _advanceState.MarkLineEntered();

        _backlogRecorder.Record(meta);
        _rollbackController.AddRollbackPoint(meta);
        
        CommandRunTicket ticket = _playbackDriver.PlayCollected();
        _commandEntryGate?.Register(ticket);

        return meta;
    }
}