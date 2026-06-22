using System.Collections;
using System.Threading;
using Yarn.Unity;

public enum SyncGateRunResult
{
    Completed,
    Cancelled,
    Superseded,
    AlreadyRunning,
    LaneCompleted,
    LaneUnavailable,
}

public class VNSideRunnerSyncHub
{
    private readonly PresentationLaneState _lane = new();
    private readonly SyncGatePlanBuilder _planBuilder = new();
    private readonly SyncGateState _syncGate = new();
    private readonly SyncGateAdvancer _advancer = new();

    public PresentationLaneRunToken CurrentPresentationRun => _lane.CurrentRun;

    public void RegisterPresentationLane(DialogueRunner runner)
    {
        _lane.Register(runner);
    }

    public IEnumerator StartPresentationLaneCoroutine(string nodeName)
    {
        if (_lane.IsDialogueRunning)
        {
            YarnTask stopTask = _lane.StopDialogue();

            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        _lane.BeginRun();

        YarnTask startTask = _lane.StartDialogue(nodeName);

        while (!startTask.IsCompletedSuccessfully())
            yield return null;

        PumpSyncGate();
    }

    public IEnumerator StopPresentationLaneCoroutine()
    {
        _lane.CompleteRun();
        _syncGate.Clear();

        if (_lane.IsDialogueRunning)
        {
            YarnTask stopTask = _lane.StopDialogue();

            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }
    }

    public void ResetPresentationLane()
    {
        _lane.ResetAll();
        _syncGate.Clear();
        _planBuilder.Reset();
    }

    public void ClearAllForSeekOrLoad()
    {
        _lane.ClearForDeterministicReplay();
        _syncGate.Clear();
        _planBuilder.ClearForReplayBoundary();
    }

    public void NotifyPresentationLaneReady(PresentationLaneRunToken run)
    {
        _lane.NotifyReady(run);
        PumpSyncGate();
    }

    public void NotifyPresentationLaneNotReady(PresentationLaneRunToken run)
    {
        _lane.NotifyNotReady(run);
    }

    public void NotifyPresentationLaneReleased(PresentationLaneRunToken run)
    {
        _lane.NotifyReleased(run);
        PumpSyncGate();
    }

    public void NotifyPresentationLaneCompleted(PresentationLaneRunToken run)
    {
        _lane.NotifyCompleted(run);
        PumpSyncGate();
    }

    public void NotifyPresentationForwardSettled(PresentationLaneRunToken run)
    {
        _lane.NotifyForwardSettled(run);
        PumpSyncGate();
    }
    
    public async YarnTask<SyncGateRunResult> RunForwardSyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = _planBuilder.ConsumeForwardPlan(
            _lane.CanReceiveScriptedAdvance,
            _lane.ForwardSettleEpoch);

        return await RunSyncGatePlanAsync(plan, cancel);
    }
    
    public async YarnTask<SyncGateRunResult> RunSeekResyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = _planBuilder.ConsumeSeekResyncPlan(
            _lane.CanReceiveSeekResyncAdvance);

        return await RunSyncGatePlanAsync(plan, cancel);
    }

    // inline [advance]용 경로.
    public async YarnTask<SyncGateRunResult> RunInlineScriptedAdvanceAsync(
        int steps, CancellationToken cancel)
    {
        SyncGatePlan plan = _planBuilder.BuildInlineScriptedAdvancePlan(
            _lane.CanReceiveScriptedAdvance,
            _lane.ForwardSettleEpoch,
            steps);

        return await RunSyncGatePlanAsync(plan, cancel);
    }

    private async YarnTask<SyncGateRunResult> RunSyncGatePlanAsync(
        SyncGatePlan plan,
        CancellationToken cancel)
    {
        if (plan == null || plan.IsEmpty)
            return SyncGateRunResult.Completed;

        if (!_syncGate.TryBegin(plan))
            return SyncGateRunResult.AlreadyRunning;

        PresentationLaneRunToken run = _lane.CurrentRun;

        while (!_syncGate.IsCompleted)
        {
            if (cancel.IsCancellationRequested)
            {
                _syncGate.Clear();
                return SyncGateRunResult.Cancelled;
            }

            if (!_lane.IsCurrent(run))
            {
                _syncGate.Clear();
                return SyncGateRunResult.Superseded;
            }

            SyncGateAdvanceResult result = PumpSyncGate();

            switch (result)
            {
                case SyncGateAdvanceResult.LaneCompleted:
                    _syncGate.Clear();
                    return SyncGateRunResult.LaneCompleted;

                case SyncGateAdvanceResult.LaneUnavailable:
                    _syncGate.Clear();
                    return SyncGateRunResult.LaneUnavailable;
            }

            if (_syncGate.IsCompleted)
                break;

            await YarnTask.Yield();
        }

        return SyncGateRunResult.Completed;
    }

    public void HoldPresentation(int lines)
    {
        _planBuilder.Hold(lines);
    }

    public void AdvancePresentationExtra(int steps)
    {
        if (!_lane.CanReceiveForwardModifier)
            return;

        _planBuilder.AddExtraAdvance(steps);
    }

    public void SetPresentationSuppressFirstAutoAdvance(bool suppress)
    {
        _planBuilder.SetSuppressNextBaseAdvance(suppress);
    }

    public void PausePresentation()
    {
        _lane.Pause();
    }

    public void ResumePresentation()
    {
        _lane.Resume();
        PumpSyncGate();
    }

    private SyncGateAdvanceResult PumpSyncGate()
    {
        SyncGateAdvanceResult lastResult = SyncGateAdvanceResult.Completed;

        while (!_syncGate.IsCompleted)
        {
            SyncGateAdvanceResult result = _advancer.TryAdvanceCurrent(_syncGate, _lane);
            lastResult = result;

            if (result != SyncGateAdvanceResult.Progressed)
                return result;
        }

        return lastResult;
    }
}