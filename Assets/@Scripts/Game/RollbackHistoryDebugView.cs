using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RollbackHistoryDebugView : MonoBehaviour
{
    [SerializeField] private int count;
    [SerializeField] private bool canRollbackOneStep;

    [Header("Points")]
    [SerializeField] private List<RollbackPointDebugEntry> points = new();

    private RollbackHistory _history;
    private float _nextRefreshTime;

    public int Count => count;
    public bool CanRollbackOneStep => canRollbackOneStep;

    public void Bind(RollbackHistory history)
    {
        _history = history;
        RefreshSnapshot();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;
        
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, 0.25f);
        RefreshSnapshot();
    }

    public void RefreshSnapshot()
    {
        points.Clear();

        if (_history == null)
        {
            count = 0;
            canRollbackOneStep = false;
            return;
        }

        IReadOnlyList<RollbackPoint> source = _history.Points;

        count = source.Count;
        canRollbackOneStep = _history.CanRollbackOneStep;

        for (int i = 0; i < source.Count; i++)
        {
            points.Add(RollbackPointDebugEntry.From(i, source[i]));
        }
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