using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNSideRunnerSyncHub
{
    private readonly Dictionary<string, VNSideRunnerLaneState> _lanes = new (StringComparer.Ordinal);

    private readonly VNTraceStream _trace;

    public VNSideRunnerSyncHub(VNTraceStream trace = null)
    {
        _trace = trace;
    }

    public void RegisterLane(string laneKey, DialogueRunner runner)
    {
        if (string.IsNullOrEmpty(laneKey))
        {
            Debug.LogError("[VNSideRunnerSyncHub] laneKey is null or empty.");
            return;
        }

        if (runner == null)
        {
            Debug.LogError($"[VNSideRunnerSyncHub] runner is null. laneKey='{laneKey}'");
            return;
        }

        _lanes[laneKey] = new VNSideRunnerLaneState(laneKey, runner);
        Trace("RegisterLane", _lanes[laneKey].Snapshot());
    }

    public int GetLaneGeneration(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return -1;

        return lane.Generation;
    }

    public bool TryGetRunner(string laneKey, out DialogueRunner runner)
    {
        runner = null;

        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return false;

        runner = lane.Runner;
        return runner != null;
    }

    public void DispatchLaneAdvance(string laneKey)
    {
        int generation = GetLaneGeneration(laneKey);
        DispatchLaneAdvance(laneKey, generation);
    }

    public void DispatchLaneAdvance(string laneKey, int generation)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        if (generation != lane.Generation)
        {
            Trace("DispatchLaneAdvanceIgnored", $"reason=OldGeneration, received={generation}, {lane.Snapshot()}");
            return;
        }

        lane.PendingAdvanceCount++;
        Trace("DispatchLaneAdvance", lane.Snapshot());

        TryFlush(lane);
    }

    public void NotifyLaneReady(string laneKey, int generation)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        if (generation != lane.Generation)
        {
            Trace("NotifyLaneReadyIgnored", $"reason=OldGeneration, received={generation}, {lane.Snapshot()}");
            return;
        }

        lane.IsReadyForAdvance = true;
        Trace("NotifyLaneReady", lane.Snapshot());

        TryFlush(lane);
    }

    public void NotifyLaneNotReady(string laneKey, int generation)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        if (generation != lane.Generation)
        {
            Trace("NotifyLaneNotReadyIgnored", $"reason=OldGeneration, received={generation}, {lane.Snapshot()}");
            return;
        }

        lane.IsReadyForAdvance = false;
        Trace("NotifyLaneNotReady", lane.Snapshot());
    }

    public void ResetLaneForRestart(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        lane.ResetForRestart();
        Trace("ResetLaneForRestart", lane.Snapshot());
    }

    public void ClearAllForSeekOrLoad()
    {
        foreach (VNSideRunnerLaneState lane in _lanes.Values)
        {
            lane.ResetForRestart();
            Trace("ClearLaneForSeekOrLoad", lane.Snapshot());
        }
    }

    private bool TryFlush(VNSideRunnerLaneState lane)
    {
        if (lane.PendingAdvanceCount <= 0)
            return false;

        if (!lane.IsReadyForAdvance)
            return false;

        if (lane.Runner == null || !lane.Runner.IsDialogueRunning)
        {
            Trace("FlushLaneAdvanceBlocked", $"reason=RunnerNotRunning, {lane.Snapshot()}");
            return false;
        }

        lane.PendingAdvanceCount--;
        lane.IsReadyForAdvance = false;

        Trace("FlushLaneAdvance", lane.Snapshot());

        lane.Runner.RequestNextLine();
        return true;
    }

    private bool TryGetLane(string laneKey, out VNSideRunnerLaneState lane)
    {
        if (_lanes.TryGetValue(laneKey, out lane))
            return true;

        Debug.LogWarning($"[VNSideRunnerSyncHub] Lane not registered. laneKey='{laneKey}'");
        return false;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNSideRunnerSyncHub), evt, "sideRunnerSync", note);
    }
}