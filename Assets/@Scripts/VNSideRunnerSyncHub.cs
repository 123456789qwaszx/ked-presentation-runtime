using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNSideRunnerSyncHub
{
    private readonly Dictionary<string, VNSideRunnerLaneState> _lanes = new(StringComparer.Ordinal);

    private readonly VNTraceStream _trace;

    public event Action<string, int> LaneReady;

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

    public async YarnTask WaitUntilLaneReadyAsync(string laneKey, int generation)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        if (generation != lane.Generation)
        {
            Trace("WaitUntilLaneReadyIgnored", $"reason=OldGeneration, received={generation}, {lane.Snapshot()}");
            return;
        }

        if (lane.IsReadyForAdvance)
        {
            Trace("WaitUntilLaneReadyAlreadyReady", lane.Snapshot());
            return;
        }

        bool ready = false;

        void OnLaneReady(string readyLaneKey, int readyGeneration)
        {
            if (!string.Equals(readyLaneKey, laneKey, StringComparison.Ordinal))
                return;

            if (readyGeneration != generation)
                return;

            ready = true;
        }

        LaneReady += OnLaneReady;

        Trace("WaitUntilLaneReadyStarted", lane.Snapshot());

        while (!ready)
            await YarnTask.Yield();

        LaneReady -= OnLaneReady;

        Trace("WaitUntilLaneReadyCompleted", lane.Snapshot());
    }

    public async YarnTask WaitUntilLaneReadyAsync(string laneKey)
    {
        int generation = GetLaneGeneration(laneKey);
        await WaitUntilLaneReadyAsync(laneKey, generation);
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
            return;

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

        Action<string, int> handler = LaneReady;
        if (handler != null)
            handler(laneKey, generation);

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