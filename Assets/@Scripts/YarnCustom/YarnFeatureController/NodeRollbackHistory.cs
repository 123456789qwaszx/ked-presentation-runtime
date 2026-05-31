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

public sealed class RollbackHistory
{
    private readonly List<RollbackPoint> _points = new();

    private int _nextHistoryIndex = 0;

    public IReadOnlyList<RollbackPoint> Points => _points;

    public bool CanRollbackOneStep => _points.Count >= 2;

    public void AddRollbackPoint(YarnLineMeta meta)
    {
        if (IsDuplicateOfLastPoint(meta))
            return;

        _points.Add(new RollbackPoint(
            historyIndex: _nextHistoryIndex++,
            nodeName: meta.nodeName,
            lineId: meta.lineId,
            rawText: meta.rawText
        ));
    }

    public bool TryPrepareRollbackOneStep(out RollbackPoint target)
    {
        target = default;

        if (!CanRollbackOneStep)
            return false;

        int targetListIndex = _points.Count - 2;
        target = _points[targetListIndex];

        ClearRollbackHistory();

        return true;
    }

    public bool TryPrepareRollbackToHistoryIndex(
        int historyIndex,
        out RollbackPoint target)
    {
        target = default;

        int targetListIndex = FindListIndexByHistoryIndex(historyIndex);
        if (targetListIndex < 0)
            return false;

        if (targetListIndex >= _points.Count - 1)
            return false;

        target = _points[targetListIndex];

        ClearRollbackHistory();

        return true;
    }
    
    public bool TryGetLatestPoint(out RollbackPoint point)
    {
        if (_points.Count <= 0)
        {
            point = default;
            return false;
        }

        point = _points[^1];
        return true;
    }
    
    public void ClearRollbackHistory()
    {
        _points.Clear();
        _nextHistoryIndex = 0;
    }

    public bool TryGetPointByHistoryIndex(int historyIndex, out RollbackPoint point)
    {
        int index = FindListIndexByHistoryIndex(historyIndex);
        if (index < 0)
        {
            point = default;
            return false;
        }

        point = _points[index];
        return true;
    }

    public bool IsDuplicateOfLastPoint(YarnLineMeta meta)
    {
        if (_points.Count == 0)
            return false;

        RollbackPoint last = _points[^1];

        return last.nodeName == meta.nodeName &&
               last.lineId == meta.lineId;
    }


    public List<RollbackPoint> CreateSnapshot()
    {
        return new List<RollbackPoint>(_points);
    }

    public void RestoreSnapshot(
        IReadOnlyList<RollbackPoint> points,
        int nextHistoryIndex = -1)
    {
        _points.Clear();

        if (points != null)
            _points.AddRange(points);

        if (nextHistoryIndex >= 0)
        {
            _nextHistoryIndex = nextHistoryIndex;
            return;
        }

        _nextHistoryIndex = CalculateNextHistoryIndex();
    }

    private int FindListIndexByHistoryIndex(int historyIndex)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i].historyIndex == historyIndex)
                return i;
        }

        return -1;
    }
    
    private void RemoveFromListIndex(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _points.Count)
            return;

        _points.RemoveRange(listIndex, _points.Count - listIndex);
    }
    
    private void RemoveAfterListIndex(int listIndex)
    {
        if (listIndex < 0 || listIndex >= _points.Count)
            return;

        int removeStart = listIndex + 1;
        if (removeStart >= _points.Count)
            return;

        _points.RemoveRange(removeStart, _points.Count - removeStart);
    }

    private int CalculateNextHistoryIndex()
    {
        int max = -1;

        for (int i = 0; i < _points.Count; i++)
        {
            if (_points[i].historyIndex > max)
                max = _points[i].historyIndex;
        }

        return max + 1;
    }
}