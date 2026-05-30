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

    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackController _rollbackController;
    private readonly VNRuntimeStateProvider _runtimeStateProvider;
    
    public VNLinePresentationCommitter(
        VNLinePresentationState advanceState,
        YarnBridgePlaybackDriver playbackDriver,
        BacklogRecorder backlogRecorder,
        RollbackController rollbackController,
        VNRuntimeStateProvider runtimeStateProvider)
    {
        _advanceState = advanceState;
        _playbackDriver = playbackDriver;
        _backlogRecorder = backlogRecorder;
        _rollbackController = rollbackController;
        _runtimeStateProvider = runtimeStateProvider;
    }

    private YarnLineMeta _currentMeta;

    public YarnLineMeta CommitLineEntered(LocalizedLine line, string nodeName)
    {
        YarnLineMeta meta = new (nodeName, line.TextID, line.CharacterName, line.TextWithoutCharacterName.Text);
        _currentMeta = meta;
        
        _runtimeStateProvider.HandleLineEntered(meta);

        if (!_advanceState.IsSeekingActive)
        {
            _backlogRecorder.Record(meta);

            if (_advanceState.CanRecordRollbackPoint)
                _rollbackController.AddRollbackPoint(meta);
        }
        
        _advanceState.MarkLineEntered();

        _playbackDriver.PlayCollected();

        return meta;
    }

    public void CommitLineProcessingCompleted()
    {
        _advanceState.MarkLineDisplayCompleted(_currentMeta);
    }
}