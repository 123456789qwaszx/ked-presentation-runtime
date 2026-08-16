using System.Collections.Generic;


public sealed class VNRuntimeStateProvider
{
    private readonly RollbackHistory _rollbackHistory;
    private readonly ChoiceHistory _choiceHistory;

    private YarnLineMeta _currentLineMeta;
    private bool _hasCurrentLineMeta;

    public VNRuntimeStateProvider(
        RollbackHistory rollbackHistory,
        ChoiceHistory choiceHistory)
    {
        _rollbackHistory = rollbackHistory;
        _choiceHistory = choiceHistory;
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
            return 0;
        }
    }

    public string CurrentChapterLabel
    {
        get
        {
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

    public void UpdateCurrentLineMeta(YarnLineMeta meta)
    {
        _currentLineMeta = meta;
        _hasCurrentLineMeta = true;
    }

    public List<VNChoiceRecord> CreateChoiceSnapshot() => _choiceHistory.CreateChoiceSnapshot();
    

    private bool TryGetCurrentSavePoint(out RollbackPoint point)
    {
        if (_rollbackHistory.Points.Count == 0)
        {
            point = default;
            return false;
        }
        
        point =  _rollbackHistory.Points[^1];
        return true;
    }
}