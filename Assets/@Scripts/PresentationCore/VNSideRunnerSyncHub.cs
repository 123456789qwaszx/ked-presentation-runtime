using System;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

public static class VNSideRunnerLaneKeys
{
    public const string Presentation = "presentation";
    public const string Camera = "camera";
    public const string Option = "option";
    public const string Choice = "choice";
    public const string Data = "data";
}

public sealed class VNSideRunnerSyncHub
{
    private sealed class LaneState
    {
        public readonly DialogueRunner Runner;
        public int PendingAdvanceCount;
        public bool IsReadyForAdvance;

        public LaneState(DialogueRunner runner)
        {
            Runner = runner;
        }

        public void Reset()
        {
            PendingAdvanceCount = 0;
            IsReadyForAdvance = false;
        }
    }

    private readonly Dictionary<string, LaneState> _lanes = new(StringComparer.Ordinal);

    public event Action<string> LaneReady;

    public bool RegisterPresentationLane(DialogueRunner runner) => RegisterLane(VNSideRunnerLaneKeys.Presentation, runner);
    public IEnumerator StartPresentationLaneCoroutine(string nodeName) => StartLaneCoroutine(VNSideRunnerLaneKeys.Presentation, nodeName);
    public YarnTask WaitUntilPresentationLaneReadyAsync() => WaitUntilLaneReadyAsync(VNSideRunnerLaneKeys.Presentation);
    public void DispatchPresentationAdvance() => DispatchLaneAdvance(VNSideRunnerLaneKeys.Presentation);
    public void NotifyPresentationLaneReady() => NotifyLaneReady(VNSideRunnerLaneKeys.Presentation);
    public void NotifyPresentationLaneNotReady() => NotifyLaneNotReady(VNSideRunnerLaneKeys.Presentation);
    
    public void ClearAllForSeekOrLoad()
    {
        foreach (LaneState lane in _lanes.Values)
        {
            lane.Reset();
        }
    }

    private bool RegisterLane(string laneKey, DialogueRunner runner)
    {
        if (string.IsNullOrEmpty(laneKey) || runner == null)
            return false;

        if (_lanes.ContainsKey(laneKey))
            return true;

        _lanes.Add(laneKey, new LaneState(runner));
        return true;
    }

    private IEnumerator StartLaneCoroutine(string laneKey, string nodeName)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            yield break;

        lane.Reset();

        YarnTask startTask = lane.Runner.StartDialogue(nodeName);

        while (!startTask.IsCompletedSuccessfully())
            yield return null;
    }

    private async YarnTask WaitUntilLaneReadyAsync(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
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

    private void DispatchLaneAdvance(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;

        lane.PendingAdvanceCount++;

        TryFlush(lane);
    }

    private void NotifyLaneReady(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;

        lane.IsReadyForAdvance = true;

        Action<string> handler = LaneReady;
        if (handler != null)
            handler(laneKey);

        TryFlush(lane);
    }

    private void NotifyLaneNotReady(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;

        lane.IsReadyForAdvance = false;
    }

    private bool TryFlush(LaneState lane)
    {
        if (lane.PendingAdvanceCount <= 0)
            return false;

        if (!lane.IsReadyForAdvance)
            return false;

        if (lane.Runner == null || !lane.Runner.IsDialogueRunning)
            return false;

        lane.PendingAdvanceCount--;
        lane.IsReadyForAdvance = false;

        lane.Runner.RequestNextLine();
        return true;
    }

    private LaneState GetLane(string laneKey)
    {
        _lanes.TryGetValue(laneKey, out LaneState lane);
        return lane;
    }
}