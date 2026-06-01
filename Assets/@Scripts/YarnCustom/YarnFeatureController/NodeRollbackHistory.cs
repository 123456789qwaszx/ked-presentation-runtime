using System;
using System.Collections.Generic;
using UnityEngine;

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
    public const int MaxOptionCount = 5;

    private readonly List<RollbackPoint> _points = new List<RollbackPoint>();
    private readonly List<VNChoiceRecord> _choices = new List<VNChoiceRecord>();

    private int _nextHistoryIndex = 0;

    private string _currentChoiceNodeName = "";
    private int _nextChoiceIndexInNode = 0;

    public IReadOnlyList<RollbackPoint> Points
    {
        get { return _points; }
    }

    public IReadOnlyList<VNChoiceRecord> Choices
    {
        get { return _choices; }
    }

    public bool CanRollbackOneStep
    {
        get { return _points.Count >= 2; }
    }

    public void NotifyNodeStarted(string nodeName)
    {
        _currentChoiceNodeName = nodeName ?? "";
        _nextChoiceIndexInNode = 0;
    }

    public int ConsumeChoiceIndexInCurrentNode(string nodeName)
    {
        nodeName = nodeName ?? "";

        if (!string.Equals(_currentChoiceNodeName, nodeName, StringComparison.Ordinal))
            NotifyNodeStarted(nodeName);

        int index = _nextChoiceIndexInNode;
        _nextChoiceIndexInNode++;

        return index;
    }

    public void AddRollbackPoint(YarnLineMeta meta)
    {
        if (IsDuplicateOfLastPoint(meta))
            return;

        _points.Add(new RollbackPoint(
            historyIndex: _nextHistoryIndex++,
            nodeName: meta.nodeName,
            lineId: meta.lineId,
            rawText: meta.rawText));
    }

    public bool TryPrepareRollbackOneStep(out RollbackPoint target)
    {
        target = default;

        if (!CanRollbackOneStep)
            return false;

        int targetListIndex = _points.Count - 2;
        target = _points[targetListIndex];

        TrimChoicesAfterHistoryIndex(target.historyIndex);
        ClearRollbackPointsForSeekRebuild();

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

        TrimChoicesAfterHistoryIndex(target.historyIndex);
        ClearRollbackPointsForSeekRebuild();

        return true;
    }

    public bool TryGetLatestPoint(out RollbackPoint point)
    {
        if (_points.Count <= 0)
        {
            point = default;
            return false;
        }

        point = _points[_points.Count - 1];
        return true;
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

        RollbackPoint last = _points[_points.Count - 1];

        return last.nodeName == meta.nodeName &&
               last.lineId == meta.lineId;
    }

    public void AddChoiceRecord(
        string nodeName,
        int choiceIndexInNode,
        int selectedOptionIndex)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
            return;

        if (choiceIndexInNode < 0)
            return;

        if (selectedOptionIndex < 0 || selectedOptionIndex >= MaxOptionCount)
        {
            Debug.LogWarning(
                $"[RollbackHistory] Choice index out of supported range. selectedOptionIndex={selectedOptionIndex}, max={MaxOptionCount}");
            return;
        }

        int anchorHistoryIndex = GetLatestHistoryIndexOrDefault();

        RemoveExistingChoiceRecord(nodeName, choiceIndexInNode);

        _choices.Add(new VNChoiceRecord(
            anchorHistoryIndex,
            nodeName,
            choiceIndexInNode,
            selectedOptionIndex));
    }

    public bool TryGetChoiceRecord(
        string nodeName,
        int choiceIndexInNode,
        out VNChoiceRecord record)
    {
        record = default;

        if (string.IsNullOrWhiteSpace(nodeName))
            return false;

        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (!string.Equals(choice.nodeName, nodeName, StringComparison.Ordinal))
                continue;

            if (choice.choiceIndexInNode != choiceIndexInNode)
                continue;

            record = choice;
            return true;
        }

        return false;
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

    public List<VNChoiceRecord> CreateChoiceSnapshot()
    {
        return new List<VNChoiceRecord>(_choices);
    }

    public void RestoreChoiceSnapshot(IReadOnlyList<VNChoiceRecord> choices)
    {
        _choices.Clear();

        if (choices == null)
            return;

        for (int i = 0; i < choices.Count; i++)
        {
            VNChoiceRecord choice = choices[i];

            if (!choice.IsValid())
                continue;

            if (choice.selectedOptionIndex < 0 ||
                choice.selectedOptionIndex >= MaxOptionCount)
                continue;

            _choices.Add(choice);
        }
    }

    public void ClearAll()
    {
        _points.Clear();
        _choices.Clear();

        _nextHistoryIndex = 0;
        _currentChoiceNodeName = "";
        _nextChoiceIndexInNode = 0;
    }

    public void ClearRollbackPointsForSeekRebuild()
    {
        _points.Clear();
        _nextHistoryIndex = 0;

        _currentChoiceNodeName = "";
        _nextChoiceIndexInNode = 0;
    }

    private void TrimChoicesAfterHistoryIndex(int historyIndex)
    {
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            if (_choices[i].anchorHistoryIndex > historyIndex)
                _choices.RemoveAt(i);
        }
    }

    private void RemoveExistingChoiceRecord(string nodeName, int choiceIndexInNode)
    {
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (!string.Equals(choice.nodeName, nodeName, StringComparison.Ordinal))
                continue;

            if (choice.choiceIndexInNode != choiceIndexInNode)
                continue;

            _choices.RemoveAt(i);
        }
    }

    private int GetLatestHistoryIndexOrDefault()
    {
        if (_points.Count <= 0)
            return -1;

        return _points[_points.Count - 1].historyIndex;
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