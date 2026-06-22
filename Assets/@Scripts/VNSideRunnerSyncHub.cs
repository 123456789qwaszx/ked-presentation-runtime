using System.Collections;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public enum SyncGateRunResult
{
    Completed,
    Cancelled
}

public class VNSideRunnerSyncHub
{
    private SyncGatePlanBuilder _syncGatePlanBuilder;
    private SyncGateState _syncGateState;
    private SyncGateAdvancer _syncGateAdvancer;
    private PresentationLaneState _laneState;

    public PresentationLaneRunToken CurrentPresentationRun => _laneState.CurrentRun;

    public void Initialize(DialogueRunner runner)
    {
        _syncGatePlanBuilder = new();
        _syncGateState = new();
        _syncGateAdvancer = new();
        
        _laneState = new(runner);
    }
    
    public void HoldPresentation(int lines) => _syncGatePlanBuilder.Hold(lines);
    public void AdvancePresentationExtra(int steps) => _syncGatePlanBuilder.AddExtraAdvance(steps);
    public void PausePresentation() => _laneState.Pause();
    public void ResumePresentation() => _laneState.Resume();
    
    public void NotifyLaneReady(PresentationLaneRunToken run) => _laneState.NotifyReady(run);
    public void NotifyLaneNotReady(PresentationLaneRunToken run) => _laneState.NotifyNotReady(run);
    public void NotifyLaneReleased(PresentationLaneRunToken run) => _laneState.NotifyReleased(run);
    public void NotifyLaneCompleted(PresentationLaneRunToken run) => _laneState.NotifyCompleted(run);
    public void NotifyForwardSettled(PresentationLaneRunToken run) => _laneState.NotifyForwardSettled(run);

    public void StartPresentationLaneCoroutine(string nodeName)
    {
        _laneState.BeginRun();
        _syncGatePlanBuilder.Hold(1);
        _laneState.StartDialogue(nodeName);
    }

    public IEnumerator StopPresentationLaneCoroutine()
    {
        _laneState.CompleteRun();
        _syncGateState.Clear();
        _syncGatePlanBuilder.Reset();

        if (_laneState.IsDialogueRunning)
        {
            YarnTask stopTask = _laneState.StopDialogue();

            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }
    }

    public void ResetPresentationLane()
    {
        _laneState.ResetAll();
        _syncGateState.Clear();
        _syncGatePlanBuilder.Reset();
    }
    
    public async YarnTask<SyncGateRunResult> RunForwardSyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = _syncGatePlanBuilder.ConsumeForwardPlan(
            _laneState.CanReceiveScriptedAdvance, 
            _laneState.ForwardSettleEpoch);

        return await RunSyncGatePlanAsync(plan, cancel);
    }
    
    public async YarnTask<SyncGateRunResult> RunSeekResyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = _syncGatePlanBuilder.ConsumeSeekResyncPlan(
            _laneState.CanReceiveSeekResyncAdvance);

        return await RunSyncGatePlanAsync(plan, cancel);
    }

    // inline [advance]용 경로.
    public async YarnTask<SyncGateRunResult> RunInlineScriptedAdvanceAsync(
        int steps, 
        CancellationToken cancel)
    {
        SyncGatePlan plan = _syncGatePlanBuilder.BuildInlineScriptedAdvancePlan(
            _laneState.CanReceiveScriptedAdvance,
            _laneState.ForwardSettleEpoch,
            steps);

        return await RunSyncGatePlanAsync(plan, cancel);
    }

    private async YarnTask<SyncGateRunResult> RunSyncGatePlanAsync(
        SyncGatePlan plan,
        CancellationToken cancel)
    {
        if (plan.IsEmpty)
            return SyncGateRunResult.Completed;

        if (!_syncGateState.Begin(plan))
        {
            Debug.LogError("[VNSideRunnerSyncHub] SyncGatePlan overlap detected.");
            return SyncGateRunResult.Cancelled;
        }

        PresentationLaneRunToken run = _laneState.CurrentRun;

        while (!_syncGateState.IsCompleted)
        {
            if (cancel.IsCancellationRequested)
            {
                _syncGateState.Clear();
                return SyncGateRunResult.Cancelled;
            }

            if (!_laneState.IsCurrent(run))
            {
                _syncGateState.Clear();
                return SyncGateRunResult.Cancelled;
            }

            SyncGateAdvanceResult result = PumpSyncGate();

            if (result == SyncGateAdvanceResult.LaneClosed)
            {
                _syncGateState.Clear();
                return SyncGateRunResult.Completed;
            }

            if (_syncGateState.IsCompleted)
                break;

            await YarnTask.Yield();
        }

        return SyncGateRunResult.Completed;
    }
    
    private SyncGateAdvanceResult PumpSyncGate()
    {
        while (!_syncGateState.IsCompleted)
        {
            SyncGateAdvanceResult result =
                _syncGateAdvancer.TryAdvanceCurrent(_syncGateState, _laneState);

            if (result != SyncGateAdvanceResult.Progressed)
                return result;
        }

        return SyncGateAdvanceResult.Completed;
    }
}