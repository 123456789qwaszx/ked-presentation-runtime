using System;
using System.Threading;
using Yarn.Unity;

public partial class VNSideRunnerSyncHub
{
    // ---------------------------------------------------------------------
    // Legacy wait API
    // ---------------------------------------------------------------------

    /// <summary>
    /// 기존 VNLinePresentationFlow 호환용.
    /// Scripted forward advance가 targetEpoch까지 settle될 때까지 기다린다.
    /// 
    /// completed / unavailable / paused / cancelled 상태에서는 기존 흐름처럼
    /// 더 기다리지 않고 빠져나온다.
    /// </summary>
    public async YarnTask WaitUntilForwardSettledAsync(
        int targetEpoch,
        CancellationToken cancel)
    {
        while (_lane.ForwardSettleEpoch < targetEpoch)
        {
            if (cancel.IsCancellationRequested)
                return;

            if (_lane.IsCompleted)
                return;

            if (!_lane.IsAvailable)
                return;

            if (_lane.IsPaused)
                return;

            PumpSyncGate();

            await YarnTask.Yield();
        }
    }

    /// <summary>
    /// 기존 seek pass-through 호환용.
    /// 이름은 Ready지만, 새 구조에서는 Released도 main을 열 수 있는 상태다.
    /// </summary>
    public async YarnTask WaitUntilPresentationLaneReadyAsync()
    {
        while (true)
        {
            if (_lane.IsCompleted)
                return;

            if (!_lane.IsAvailable)
                return;

            if (_lane.IsOpenForMain)
                return;

            PumpSyncGate();

            await YarnTask.Yield();
        }
    }

    public async YarnTask WaitUntilPresentationLaneReadyAsync(
        CancellationToken cancel)
    {
        while (true)
        {
            if (cancel.IsCancellationRequested)
                return;

            if (_lane.IsCompleted)
                return;

            if (!_lane.IsAvailable)
                return;

            if (_lane.IsOpenForMain)
                return;

            PumpSyncGate();

            await YarnTask.Yield();
        }
    }

    // ---------------------------------------------------------------------
    // Legacy manual-step API
    // ---------------------------------------------------------------------

    /// <summary>
    /// 기존 StepPresentationOnce 호환용.
    /// 즉시 side lane을 명시적으로 진행시키는 수동 step이다.
    /// pause를 우회할 수 있고, forward settle 회계에는 포함하지 않는다.
    /// </summary>
    public void StepPresentationOnce(int steps = 1)
    {
        QueueManualPresentationStepBypassingPause(steps);
    }
}