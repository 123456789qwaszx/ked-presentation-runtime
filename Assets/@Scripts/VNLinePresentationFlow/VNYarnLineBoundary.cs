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

    // recordToHistory:
    //  - true  : 일반 대사 라인. backlog + rollback point에 기록한다.
    //  - false : 연출 비트(staging-only beat). 대사가 없는 라인이므로 backlog 항목으로도, rollback 네비게이션 대상으로도 남기지 않는다.
    public void CommitLineEntered(YarnLineMeta meta, bool recordToHistory)
    {
        // 런타임 '현재 라인' 포인터는 항상 갱신한다.
        // (seek 타겟/저장 위치가 참조하는 현재 위치)
        _runtimeStateProvider.UpdateCurrentLineMeta(meta);

        if (!recordToHistory)
            return;

        _backlogRecorder.Record(meta);
        _rollbackHistory.AddRollbackPoint(meta);
    }
}