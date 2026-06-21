using System.Collections;
using System.Threading;
using Yarn.Unity;

public enum SyncGateRunResult
{
    Completed,
    Cancelled,
    LaneCompleted,
    LanePaused,
    LaneUnavailable,
}

/// <summary>
/// Main lane과 side presentation lane 사이의 진행 가능 여부를 조율한다.
///
/// 이 클래스는 직접 진행 조건을 흩뿌리지 않는다.
/// 모든 동기화 진행은 SyncGatePlan으로 표현되고,
/// SyncGateAdvancer가 현재 SyncGateToken을 소비할 수 있을 때만 진행한다.
/// </summary>
public partial class VNSideRunnerSyncHub
{
    private readonly PresentationLaneState _lane = new();
    private readonly SyncGatePlanBuilder _planBuilder = new();
    private readonly SyncGateState _syncGate = new();
    private readonly SyncGateAdvancer _advancer = new();

    public int ForwardSettleEpoch => _lane.ForwardSettleEpoch;
    public PresentationLaneRunToken CurrentPresentationRun => _lane.CurrentRun;

    public void RegisterPresentationLane(DialogueRunner runner) => _lane.Register(runner);

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

    // Side lane signals
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

    // gate-plan
    public SyncGatePlan BuildForwardSyncGatePlan()
    {
        return _planBuilder.BuildForwardPlan(
            _lane.CanReceiveScriptedAdvance,
            _lane.ForwardSettleEpoch);
    }

    public SyncGatePlan BuildSeekResyncGatePlan()
    {
        return _planBuilder.BuildSeekResyncPlan(
            _lane.CanReceiveSeekResyncAdvance);
    }

    public SyncGatePlan BuildManualStepGatePlan(int steps = 1)
    {
        return _planBuilder.BuildManualStepPlan(
            _lane.CanReceiveManualAdvance,
            steps);
    }

    public void EnqueueSyncGatePlan(SyncGatePlan plan)
    {
        _syncGate.Enqueue(plan);
        PumpSyncGate();
    }

    public async YarnTask<SyncGateRunResult> RunSyncGatePlanAsync(
        SyncGatePlan plan,
        CancellationToken cancel)
    {
        _syncGate.Enqueue(plan);

        while (!_syncGate.IsCompleted)
        {
            if (cancel.IsCancellationRequested)
                return SyncGateRunResult.Cancelled;

            SyncGateAdvanceResult result = PumpSyncGate();

            switch (result)
            {
                case SyncGateAdvanceResult.LaneCompleted:
                    return SyncGateRunResult.LaneCompleted;

                case SyncGateAdvanceResult.LanePaused:
                    return SyncGateRunResult.LanePaused;

                case SyncGateAdvanceResult.LaneUnavailable:
                    return SyncGateRunResult.LaneUnavailable;
            }

            if (_syncGate.IsCompleted)
                break;

            await YarnTask.Yield();
        }

        return SyncGateRunResult.Completed;
    }

    public YarnTask<SyncGateRunResult> RunForwardSyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = BuildForwardSyncGatePlan();
        return RunSyncGatePlanAsync(plan, cancel);
    }

    public YarnTask<SyncGateRunResult> RunSeekResyncGatePlanAsync(
        CancellationToken cancel)
    {
        SyncGatePlan plan = BuildSeekResyncGatePlan();
        return RunSyncGatePlanAsync(plan, cancel);
    }

    // Transitional compatibility

    // 기존 forward count API 호환용.
    // 새 구조에서는 RunForwardSyncGatePlanAsync 사용을 권장한다.
    public int ConsumePresentationAutoAdvanceCount()
    {
        SyncGatePlan plan = BuildForwardSyncGatePlan();
        return plan.DispatchAdvanceCount;
    }

    // 기존 seek count API 호환용.
    // 새 구조에서는 RunSeekResyncGatePlanAsync 사용을 권장한다.
    public int ConsumePresentationSeekResyncCount()
    {
        SyncGatePlan plan = BuildSeekResyncGatePlan();
        return plan.DispatchAdvanceCount;
    }

    public void DispatchPresentationAdvance(SyncAdvanceKind kind)
    {
        if (!CanReceiveAdvanceKind(kind))
            return;

        SyncGatePlan plan = SyncGatePlan.Single(
            SyncGateToken.DispatchAdvance(kind));

        _syncGate.Enqueue(plan);
        PumpSyncGate();
    }

    private bool CanReceiveAdvanceKind(SyncAdvanceKind kind)
    {
        switch (kind)
        {
            case SyncAdvanceKind.Scripted:
                return _lane.CanReceiveScriptedAdvance;

            case SyncAdvanceKind.SeekResync:
                return _lane.CanReceiveSeekResyncAdvance;

            case SyncAdvanceKind.ManualBypassPause:
                return _lane.CanReceiveManualAdvance;

            default:
                return false;
        }
    }

    // 명시적 수동 진행 전용.
    // pause를 우회할 수 있으므로 scripted flow와 이름부터 분리한다.
    public void QueueManualPresentationStepBypassingPause(int steps = 1)
    {
        SyncGatePlan plan = BuildManualStepGatePlan(steps);

        _syncGate.Enqueue(plan);
        PumpSyncGate();
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