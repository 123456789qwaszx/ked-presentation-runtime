using System;
using System.Collections.Generic;

[Serializable]
public struct RollbackPoint
{
    public int historyIndex;
    public string nodeName;
    public string lineId;
    public string rawText;

    public RollbackPoint(
        int historyIndex,
        string nodeName,
        string lineId,
        string rawText)
    {
        this.historyIndex = historyIndex;
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.rawText = rawText;
    }
}

public sealed class RollbackController
{
    private readonly List<RollbackPoint> _points = new();
    private int _nextHistoryIndex;
    
    public IReadOnlyList<RollbackPoint> Points => _points;
    public bool CanRollbackOneStep => _points.Count >= 2;
    
    public void AddRollbackPoint(YarnLineMeta meta)
    {
        _points.Add(new RollbackPoint(
            historyIndex: _nextHistoryIndex++,
            nodeName: meta.nodeName,
            lineId: meta.lineId,
            rawText: meta.rawText));
    }

    public bool GetRollbackPoint(out RollbackPoint target)
    {
        target = default;

        if (!CanRollbackOneStep)
            return false;

        int targetListIndex = _points.Count - 2;
        target = _points[targetListIndex];
        
        return true;
    }

    public void ClearRollbackPoints()
    {
        _points.Clear();
        _nextHistoryIndex = 0;
    }
}