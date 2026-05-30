public sealed class VNRuntimeStateProvider : IVNRuntimeStateProvider
{
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly RollbackHistory _history;
    private readonly VNPlaytimeTracker _playtimeTracker;

    private YarnLineMeta _currentLineMeta;
    private bool _hasCurrentLineMeta;

    public VNRuntimeStateProvider(
        YarnLineLifecycleBridge bridge,
        RollbackHistory history,
        VNPlaytimeTracker playtimeTracker)
    {
        _bridge = bridge;
        _history = history;
        _playtimeTracker = playtimeTracker;

        _bridge.LineEntered += OnLineEntered;
    }

    public string CurrentNodeName
    {
        get
        {
            if (TryGetCurrentSavePoint(out RollbackPoint point))
                return point.nodeName;

            return _hasCurrentLineMeta ? _currentLineMeta.nodeName : "";
        }
    }

    public string CurrentLineId
    {
        get
        {
            if (TryGetCurrentSavePoint(out RollbackPoint point))
                return point.lineId;

            return _hasCurrentLineMeta ? _currentLineMeta.lineId : "";
        }
    }

    public string CurrentCharacterKey
    {
        get
        {
            return _hasCurrentLineMeta ? _currentLineMeta.charName : "";
        }
    }

    public int CurrentVisitedIndex
    {
        get
        {
            if (TryGetCurrentSavePoint(out RollbackPoint point))
                return point.historyIndex;

            return -1;
        }
    }

    public int CurrentLineVisitCountInNode
    {
        get
        {
            // MVP에서는 historyIndex가 있으므로 0으로.
            return 0;
        }
    }

    public string CurrentChapterLabel
    {
        get
        {
            // 복원용 값이 아니라 Save 슬롯 UI 표시용.
            // MVP에서는 nodeName을 fallback으로.
            string nodeName = CurrentNodeName;

            if (string.IsNullOrWhiteSpace(nodeName))
                return "";

            return nodeName;
        }
    }

    public string CurrentLinePreview
    {
        get
        {
            if (TryGetCurrentSavePoint(out RollbackPoint point))
                return point.rawText;

            return _hasCurrentLineMeta ? _currentLineMeta.rawText : "";
        }
    }

    public int CurrentPlaytimeSeconds
    {
        get
        {
            if (_playtimeTracker == null)
                return 0;

            return _playtimeTracker.CurrentPlaytimeSeconds;
        }
    }

    private void OnLineEntered(YarnLineMeta meta)
    {
        _currentLineMeta = meta;
        _hasCurrentLineMeta = true;
    }

    private bool TryGetCurrentSavePoint(out RollbackPoint point)
    {
        if (_history != null && _history.TryGetLatestPoint(out point))
            return true;

        point = default;
        return false;
    }
}