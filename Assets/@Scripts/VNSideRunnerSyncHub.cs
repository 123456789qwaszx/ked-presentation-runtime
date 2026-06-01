using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNSideRunnerSyncHub
{
    private readonly List<string> _laneKeys = new ();
    
    private readonly Dictionary<string, VNSideRunnerLaneState> _lanes = new(StringComparer.Ordinal);

    private readonly VNTraceStream _trace;

    public event Action<string> LaneReady;

    public VNSideRunnerSyncHub(VNTraceStream trace = null)
    {
        _trace = trace;
    }
    
    public void RegisterLane(string laneKey, DialogueRunner runner)
    {
        _lanes[laneKey] = new VNSideRunnerLaneState(laneKey, runner);

        if (!_laneKeys.Contains(laneKey))
            _laneKeys.Add(laneKey);
    }
    
    public IEnumerator RestartLaneCoroutine(string laneKey, string nodeName)
    {
        if (!TryGetRunner(laneKey, out DialogueRunner runner))
            yield break;

        ResetLaneForRestart(laneKey);

        if (runner.IsDialogueRunning)
        {
            YarnTask stopTask = runner.Stop();

            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        YarnTask startTask = runner.StartDialogue(nodeName);

        while (!startTask.IsCompletedSuccessfully())
            yield return null;
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

    public async YarnTask WaitUntilLaneReadyAsync(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        if (lane.IsReadyForAdvance)
            return;
        
        bool ready = false;

        void OnLaneReady(string readyLaneKey)
        {
            if (!string.Equals(readyLaneKey, laneKey, StringComparison.Ordinal))
                return;

            ready = true;
        }

        LaneReady += OnLaneReady;

        while (!ready)
            await YarnTask.Yield();

        LaneReady -= OnLaneReady;
    }

    public void DispatchLaneAdvance(string laneKey)
    {
        if (!TryGetLane(laneKey, out VNSideRunnerLaneState lane))
            return;
        
        lane.PendingAdvanceCount++;

        TryFlush(lane);
    }

    public void NotifyLaneReady(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        lane.IsReadyForAdvance = true;

        Action<string> handler = LaneReady;
        if (handler != null)
            handler(laneKey);

        TryFlush(lane);
    }

    public void NotifyLaneNotReady(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        lane.IsReadyForAdvance = false;
    }

    public void ResetLaneForRestart(string laneKey)
    {
        VNSideRunnerLaneState lane;
        if (!TryGetLane(laneKey, out lane))
            return;

        lane.ResetForRestart();
    }

    public void ClearAllForSeekOrLoad()
    {
        foreach (VNSideRunnerLaneState lane in _lanes.Values)
        {
            lane.ResetForRestart();
        }
    }

    private bool TryFlush(VNSideRunnerLaneState lane)
    {
        if (lane.PendingAdvanceCount <= 0)
            return false;

        if (!lane.IsReadyForAdvance)
            return false;

        if (!lane.Runner.IsDialogueRunning)
            return false;
        
        lane.PendingAdvanceCount--;
        lane.IsReadyForAdvance = false;

        lane.Runner.RequestNextLine();
        return true;
    }

    private bool TryGetLane(string laneKey, out VNSideRunnerLaneState lane)
    {
        if (_lanes.TryGetValue(laneKey, out lane))
            return true;

        return false;
    }
}