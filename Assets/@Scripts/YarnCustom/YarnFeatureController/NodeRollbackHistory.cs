using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RollbackPoint
{
    public int visitedIndex;
    public int frame;
    public string nodeName;
    public string lineId;
    public string rawText;

    public int presentationNodeIndex;
    public int presentationStepIndex;

    public RollbackPoint(
        int visitedIndex,
        int frame,
        string nodeName,
        string lineId,
        string rawText,
        int presentationNodeIndex,
        int presentationStepIndex)
    {
        this.visitedIndex = visitedIndex;
        this.frame = frame;
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.rawText = rawText;
        this.presentationNodeIndex = presentationNodeIndex;
        this.presentationStepIndex = presentationStepIndex;
    }
}

public sealed class RollbackRuntimeState
{
    public bool IsSeeking { get; private set; }

    public string TargetNodeName { get; private set; }
    public string TargetLineId { get; private set; }
    public int TargetVisitedIndex { get; private set; }

    public void BeginRollback(RollbackPoint target)
    {
        IsSeeking = true;
        TargetNodeName = target.nodeName;
        TargetLineId = target.lineId;
        TargetVisitedIndex = target.visitedIndex;
    }

    public void EndRollback()
    {
        IsSeeking = false;
        TargetNodeName = null;
        TargetLineId = null;
        TargetVisitedIndex = -1;
    }

    public bool IsTarget(string nodeName, string lineId)
    {
        if (!IsSeeking)
            return false;

        return TargetNodeName == nodeName && TargetLineId == lineId;
    }
}

public sealed class NodeRollbackHistory : IDisposable
{
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly RollbackRuntimeState _state;
    private readonly PresentationSessionBridge _presentationSessionBridge;

    private readonly List<RollbackPoint> _points = new();
    private string _currentNodeName = "";
    private int _visitedCounter = 0;

    public IReadOnlyList<RollbackPoint> Points => _points;
    public string CurrentNodeName => _currentNodeName;

    public NodeRollbackHistory(
        YarnLineLifecycleBridge bridge,
        RollbackRuntimeState state,
        PresentationSessionBridge presentationSessionBridge)
    {
        _bridge = bridge;
        _state = state;
        _presentationSessionBridge = presentationSessionBridge;

        _bridge.OnNodeStarted -= OnNodeStarted;
        _bridge.OnNodeStarted += OnNodeStarted;
    }

    private void OnNodeStarted(string nodeName)
    {
        if (_state.IsSeeking)
            return;

        if (_currentNodeName == nodeName)
            return;

        _currentNodeName = nodeName;
        _visitedCounter = 0;
        _points.Clear();
    }

    public void AddRollbackPoint(YarnLineMeta meta)
    {
        if (_state.IsSeeking)
            return;

        if (string.IsNullOrEmpty(meta.nodeName) || string.IsNullOrEmpty(meta.lineId))
            return;

        if (_currentNodeName != meta.nodeName)
        {
            _currentNodeName = meta.nodeName;
            _visitedCounter = 0;
            _points.Clear();
        }

        // 마지막 기록과 완전히 같은 라인이면 중복 추가 방지
        if (_points.Count > 0)
        {
            RollbackPoint last = _points[_points.Count - 1];

            if (last.nodeName == meta.nodeName && last.lineId == meta.lineId)
                return;
        }

        _presentationSessionBridge.TryGetCurrentAnchor(out int nodeIndex, out int stepIndex);

        _points.Add(new RollbackPoint(
            visitedIndex: _visitedCounter++,
            frame: meta.frame,
            nodeName: meta.nodeName,
            lineId: meta.lineId,
            rawText: meta.rawText,
            presentationNodeIndex: nodeIndex,
            presentationStepIndex: stepIndex
        ));
    }

    public bool CanRollbackOneStep()
    {
        return _points.Count >= 2;
    }

    public bool TryGetPreviousPoint(out RollbackPoint point)
    {
        point = default;

        if (_points.Count < 2)
            return false;

        point = _points[_points.Count - 2];
        return true;
    }

    public void TrimAfterVisitedIndex(int visitedIndex)
    {
        int keepCount = visitedIndex + 1;

        if (keepCount < 0)
            keepCount = 0;

        if (_points.Count > keepCount)
            _points.RemoveRange(keepCount, _points.Count - keepCount);

        _visitedCounter = _points.Count;
    }

    public void ClearRollbackHistory()
    {
        _visitedCounter = 0;
        _points.Clear();
    }

    public void Dispose()
    {
        if (_bridge == null)
            return;

        _bridge.OnNodeStarted -= OnNodeStarted;
    }
}