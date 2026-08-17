using System.Threading;
using System.Threading.Tasks;

namespace Ked.Presentation.Sync
{
    public enum SyncGateRunResult
    {
        Completed,
        Cancelled
    }

    // 두 개의 독립적인 진행 흐름을 프레임 단위로 동기화한다.
    //
    // SyncGatePlanBuilder
    // -> SyncGatePlan (SyncGateToken 시퀀스)
    // -> SyncGateAdvancer(프레임당 1개씩만 소비제한 / 필요시 카운트방식으로 변경하여 프레임 리미트 제거 가능)
    // -> PresentationLaneState
    public class VNSideRunnerSyncHub
    {
        private SyncGatePlanBuilder _syncGatePlanBuilder;
        private SyncGateState _syncGateState;
        private SyncGateAdvancer _syncGateAdvancer;
        private PresentationLaneState _laneState;
        private IFrameClock _frameClock;

        public PresentationLaneRunToken CurrentPresentationRun =>
            _laneState.CurrentRun;

        public void Initialize(ILaneRunner runner, IFrameClock frameClock)
        {
            _syncGatePlanBuilder = new();
            _syncGateState = new();
            _syncGateAdvancer = new();

            _laneState = new(runner);
            _frameClock = frameClock;
        }

        public void HoldPresentation(int lines) =>
            _syncGatePlanBuilder.Hold(lines);

        public void AdvancePresentationExtra(int steps) =>
            _syncGatePlanBuilder.AddExtraAdvance(steps);

        public void PausePresentation() => _laneState.Pause();
        public void ResumePresentation() => _laneState.Resume();

        public void NotifyLaneReady(PresentationLaneRunToken run) =>
            _laneState.NotifyReady(run);

        public void NotifyLaneNotReady(PresentationLaneRunToken run) =>
            _laneState.NotifyNotReady(run);

        public void NotifyLaneReleased(PresentationLaneRunToken run) =>
            _laneState.NotifyReleased(run);

        public void NotifyLaneCompleted(PresentationLaneRunToken run) =>
            _laneState.NotifyCompleted(run);

        public void NotifyForwardSettled(PresentationLaneRunToken run) =>
            _laneState.NotifyForwardSettled(run);

        public void StartPresentationLane(string nodeName)
        {
            _laneState.BeginRun();
            _syncGatePlanBuilder.Hold(1);
            _laneState.StartDialogue(nodeName);
        }

        public async Task StopPresentationLaneAsync()
        {
            _laneState.CompleteRun();
            _syncGateState.Clear();
            _syncGatePlanBuilder.Reset();

            if (_laneState.IsDialogueRunning)
                await _laneState.StopDialogueAsync();
        }

        // stale pending/ready가 재시작된 세션으로 새지 않도록 하는 게 목적.
        // Runner의 중단을 확인한 뒤에 호출할 것.
        public void ResetPresentationLane()
        {
            _laneState.ResetAll();
            _syncGateState.Clear();
            _syncGatePlanBuilder.Reset();
        }

        public async Task<SyncGateRunResult> RunForwardSyncGatePlanAsync(
            CancellationToken cancel)
        {
            SyncGatePlan plan = _syncGatePlanBuilder.ConsumeForwardPlan(
                _laneState.CanReceiveScriptedAdvance,
                _laneState.ForwardSettleEpoch);

            return await RunSyncGatePlanAsync(plan, cancel);
        }

        public async Task<SyncGateRunResult> RunSeekResyncGatePlanAsync(
            CancellationToken cancel)
        {
            SyncGatePlan plan = _syncGatePlanBuilder.ConsumeSeekResyncPlan(
                _laneState.CanReceiveSeekResyncAdvance);

            return await RunSyncGatePlanAsync(plan, cancel);
        }

        // inline advance용 경로.
        public async Task<SyncGateRunResult> RunInlineScriptedAdvanceAsync(
            int steps,
            CancellationToken cancel)
        {
            if (!_laneState.CanReceiveScriptedAdvance)
                return SyncGateRunResult.Cancelled;

            SyncGatePlan plan =
                _syncGatePlanBuilder.BuildInlineScriptedAdvancePlan(
                    canAdvance: true,
                    currentForwardSettleEpoch: _laneState.ForwardSettleEpoch,
                    steps: steps);

            return await RunSyncGatePlanAsync(plan, cancel);
        }

        private async Task<SyncGateRunResult> RunSyncGatePlanAsync(
            SyncGatePlan plan,
            CancellationToken cancel)
        {
            if (plan.IsEmpty)
                return SyncGateRunResult.Completed;

            // 호출측 버그. 방어.
            if (!_syncGateState.Begin(plan))
                return SyncGateRunResult.Cancelled;

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

                await _frameClock.NextFrameAsync();
            }

            return SyncGateRunResult.Completed;
        }

        private SyncGateAdvanceResult PumpSyncGate()
        {
            while (!_syncGateState.IsCompleted)
            {
                SyncGateAdvanceResult result =
                    _syncGateAdvancer.TryAdvanceCurrent(
                        _syncGateState,
                        _laneState);

                if (result != SyncGateAdvanceResult.Progressed)
                    return result;
            }

            return SyncGateAdvanceResult.Completed;
        }
    }
}