using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class RollbackHistoryDebugView : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private bool isBound;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 0.25f;

    [Header("Summary")]
    [SerializeField] private int count;
    [SerializeField] private bool canRollbackOneStep;
    [SerializeField] private int latestListIndex = -1;
    [SerializeField] private int nextRollbackTargetListIndex = -1;

    [Header("Latest Point")]
    [SerializeField] private RollbackPointDebugEntry latestPoint;

    [Header("Next Rollback Target")]
    [SerializeField] private RollbackPointDebugEntry nextRollbackTarget;

    [Header("Points")]
    [SerializeField] private List<RollbackPointDebugEntry> points = new();

    private RollbackHistory _history;
    private float _nextRefreshTime;

    public bool IsBound => isBound;
    public int Count => count;
    public bool CanRollbackOneStep => canRollbackOneStep;
    public int LatestListIndex => latestListIndex;
    public int NextRollbackTargetListIndex => nextRollbackTargetListIndex;
    public IReadOnlyList<RollbackPointDebugEntry> Points => points;

    public void Bind(RollbackHistory history)
    {
        _history = history;
        isBound = _history != null;
        RefreshSnapshot();
    }

    public void Unbind()
    {
        _history = null;
        isBound = false;
        ClearSnapshotOnly();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!autoRefresh)
            return;

        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, refreshInterval);
        RefreshSnapshot();
    }

    public void RefreshSnapshot()
    {
        points.Clear();

        if (_history == null)
        {
            isBound = false;
            count = 0;
            canRollbackOneStep = false;
            latestListIndex = -1;
            nextRollbackTargetListIndex = -1;
            latestPoint = RollbackPointDebugEntry.Empty();
            nextRollbackTarget = RollbackPointDebugEntry.Empty();
            return;
        }

        isBound = true;

        IReadOnlyList<RollbackPoint> source = _history.Points;

        count = source.Count;
        canRollbackOneStep = _history.CanRollbackOneStep;
        latestListIndex = count > 0 ? count - 1 : -1;
        nextRollbackTargetListIndex = count >= 2 ? count - 2 : -1;

        for (int i = 0; i < source.Count; i++)
        {
            points.Add(RollbackPointDebugEntry.From(i, source[i]));
        }

        latestPoint = latestListIndex >= 0
            ? RollbackPointDebugEntry.From(latestListIndex, source[latestListIndex])
            : RollbackPointDebugEntry.Empty();

        nextRollbackTarget = nextRollbackTargetListIndex >= 0
            ? RollbackPointDebugEntry.From(nextRollbackTargetListIndex, source[nextRollbackTargetListIndex])
            : RollbackPointDebugEntry.Empty();
    }

    public void ClearHistory()
    {
        if (_history == null)
            return;

        _history.ClearRollbackHistory();
        RefreshSnapshot();
    }

    public string BuildDump()
    {
        StringBuilder sb = new StringBuilder(4096);

        sb.AppendLine("[RollbackHistoryDebugView]");
        sb.AppendLine($"isBound={isBound}");
        sb.AppendLine($"count={count}");
        sb.AppendLine($"canRollbackOneStep={canRollbackOneStep}");
        sb.AppendLine($"latestListIndex={latestListIndex}");
        sb.AppendLine($"nextRollbackTargetListIndex={nextRollbackTargetListIndex}");
        sb.AppendLine();

        for (int i = 0; i < points.Count; i++)
        {
            RollbackPointDebugEntry point = points[i];

            string marker = "";

            if (i == latestListIndex)
                marker += " [Latest]";

            if (i == nextRollbackTargetListIndex)
                marker += " [NextRollbackTarget]";

            sb.AppendLine(
                $"[{i}] historyIndex={point.historyIndex}, node={point.nodeName}, line={point.lineId}{marker}");

            if (!string.IsNullOrWhiteSpace(point.rawText))
                sb.AppendLine($"    text={point.rawText}");
        }

        return sb.ToString();
    }

    public void DumpToConsole()
    {
        RefreshSnapshot();
        Debug.Log(BuildDump(), this);
    }

    private void ClearSnapshotOnly()
    {
        count = 0;
        canRollbackOneStep = false;
        latestListIndex = -1;
        nextRollbackTargetListIndex = -1;
        latestPoint = RollbackPointDebugEntry.Empty();
        nextRollbackTarget = RollbackPointDebugEntry.Empty();
        points.Clear();
    }
}

[Serializable]
public struct RollbackPointDebugEntry
{
    public int listIndex;
    public int historyIndex;

    public string nodeName;
    public string lineId;

    [TextArea(1, 4)]
    public string rawText;

    public static RollbackPointDebugEntry From(int listIndex, RollbackPoint point)
    {
        return new RollbackPointDebugEntry
        {
            listIndex = listIndex,
            historyIndex = point.historyIndex,
            nodeName = point.nodeName,
            lineId = point.lineId,
            rawText = point.rawText
        };
    }

    public static RollbackPointDebugEntry Empty()
    {
        return new RollbackPointDebugEntry
        {
            listIndex = -1,
            historyIndex = -1,
            nodeName = "",
            lineId = "",
            rawText = ""
        };
    }
}