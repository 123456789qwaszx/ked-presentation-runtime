using System;
using System.Collections.Generic;

[Serializable]
public struct RollbackPoint
{
    public int historyIndex;
    public string nodeName;
    public string lineId;
    public string rawText;

    // 장면 시작 이후 같은 (nodeName, lineId)의 몇 번째 등장인가 (1부터).
    public int occurrence;

    public RollbackPoint(
        int historyIndex,
        string nodeName,
        string lineId,
        string rawText,
        int occurrence)
    {
        this.historyIndex = historyIndex;
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.rawText = rawText;
        this.occurrence = occurrence;
    }
}

public sealed class RollbackHistory
{
    private readonly List<RollbackPoint> _points = new();
    private int _nextHistoryIndex;

    // (노드, 라인)별 등장 횟수. 시크 좌표 용도(occurrence).
    private readonly Dictionary<(string nodeName, string lineId), int> _seenCount = new();
    
    public IReadOnlyList<RollbackPoint> Points => _points;
    public bool CanRollbackOneStep => _points.Count >= 2;
    
    public void AddRollbackPoint(YarnLineMeta meta)
    {
        (string, string) key = (meta.nodeName, meta.lineId);

        _seenCount.TryGetValue(key, out int seen);
        int occurrence = seen + 1;
        _seenCount[key] = occurrence;

        _points.Add(new RollbackPoint(
            historyIndex: _nextHistoryIndex++,
            nodeName: meta.nodeName,
            lineId: meta.lineId,
            rawText: meta.rawText,
            occurrence: occurrence));
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
        _seenCount.Clear();
        _nextHistoryIndex = 0;
    }
}