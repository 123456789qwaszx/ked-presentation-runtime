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
    private readonly MessengerLog _messengerLog;

    public YarnLineBoundary(
        BacklogRecorder backlogRecorder,
        RollbackHistory rollbackHistory,
        MessengerLog messengerLog)
    {
        _backlogRecorder = backlogRecorder;
        _rollbackHistory = rollbackHistory;
        _messengerLog = messengerLog;
    }

    public YarnLineMeta BuildLineMeta(LocalizedLine line)
    {
        return new YarnLineMeta(
            line.TextID,
            line.CharacterName,
            line.TextWithoutCharacterName.Text);
    }

    public void CommitLineEntered(YarnLineMeta meta, string voiceKey, LineRecordDecision decision)
    {
        if (decision.RecordBacklog)
            _backlogRecorder.Record(meta, voiceKey);

        if (decision.AddRollbackPoint)
            _rollbackHistory.AddRollbackPoint(meta);

        // 메신저 기록은 decision을 보지 않고 줄마다 적는다(구간이 열려 있을 때만).
        // 백로그는 되감기 너머로 살아남아 다시 적지 않지만, 이 기록은 장면 경계에서 비워지고 매번 다시 선다.
        _messengerLog.RecordLine(meta.charName, meta.rawText);
    }
}