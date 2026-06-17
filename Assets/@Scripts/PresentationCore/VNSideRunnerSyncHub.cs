using System.Collections;
using System.Threading;
using Yarn.Unity;

public sealed class VNSideRunnerSyncHub
{
    private sealed class PresentationLaneState
    {
        public DialogueRunner Runner;

        public int PendingAdvanceCount;
        public int HoldRemainingLines;
        public int ExtraAdvanceCount;

        // Manual single-step budget that is allowed to drive the lane even while paused.
        // Set by StepPresentationOnce(); consumed one line at a time as the lane becomes ready.
        public int ManualStepBudget;

        // Monotonic counter bumped once per sub beat that finishes its entry phase.
        // Main forward flow captures a baseline and waits for (baseline + dispatchedCount).
        // NOTE: this is NOT reset on start/seek/load; it is monotonic on purpose so that
        // an in-flight main wait never sees its target go backwards.
        public int ForwardSettleEpoch;

        public bool IsStarted;
        public bool IsCompleted;
        public bool IsReadyForAdvance;
        public bool IsReleased;
        public bool IsPaused;
        public bool SuppressFirstAutoAdvance;

        public bool CanAcceptAdvance
        {
            get
            {
                if (Runner == null)
                    return false;

                if (!IsStarted)
                    return false;

                if (IsCompleted)
                    return false;

                return true;
            }
        }

        public bool IsBlockingMainFlow
        {
            get
            {
                if (Runner == null)
                    return false;

                if (!IsStarted)
                    return false;

                if (IsCompleted)
                    return false;

                if (IsReadyForAdvance)
                    return false;

                if (IsReleased)
                    return false;

                return true;
            }
        }

        public void ResetForStart()
        {
            PendingAdvanceCount = 0;
            HoldRemainingLines = 0;
            ExtraAdvanceCount = 0;
            ManualStepBudget = 0;

            IsStarted = true;
            IsCompleted = false;
            IsReadyForAdvance = false;
            IsReleased = false;
            IsPaused = false;
        }

        public void ResetAll()
        {
            PendingAdvanceCount = 0;
            HoldRemainingLines = 0;
            ExtraAdvanceCount = 0;
            ManualStepBudget = 0;

            IsStarted = false;
            IsCompleted = false;
            IsReadyForAdvance = false;
            IsReleased = false;
            IsPaused = false;
            SuppressFirstAutoAdvance = false;
        }

        public void MarkCompleted()
        {
            PendingAdvanceCount = 0;
            ExtraAdvanceCount = 0;
            HoldRemainingLines = 0;
            ManualStepBudget = 0;

            IsCompleted = true;
            IsReadyForAdvance = false;
            IsReleased = false;
            IsPaused = false;
        }
    }

    private readonly PresentationLaneState _presentation = new();

    public void RegisterPresentationLane(DialogueRunner runner)
    {
        _presentation.Runner = runner;
    }

    public IEnumerator StartPresentationLaneCoroutine(string nodeName)
    {
        if (_presentation.Runner.IsDialogueRunning)
        {
            YarnTask stopTask = _presentation.Runner.Stop();
            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        _presentation.ResetForStart();

        YarnTask startTask = _presentation.Runner.StartDialogue(nodeName);
        while (!startTask.IsCompletedSuccessfully())
            yield return null;

        TryFlushPresentationLane();
    }

    public IEnumerator StopPresentationLaneCoroutine()
    {
        if (_presentation.Runner == null)
            yield break;

        _presentation.MarkCompleted();

        if (_presentation.Runner.IsDialogueRunning)
        {
            YarnTask stopTask = _presentation.Runner.Stop();
            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }
    }

    public void ClearAllForSeekOrLoad()
    {
        _presentation.PendingAdvanceCount = 0;
        _presentation.ManualStepBudget = 0;
        _presentation.IsReadyForAdvance = false;
        _presentation.IsReleased = false;
    }

    public void ResetPresentationLane()
    {
        _presentation.ResetAll();
    }

    public void NotifyPresentationLaneReady()
    {
        if (!_presentation.CanAcceptAdvance)
            return;

        _presentation.IsReadyForAdvance = true;
        _presentation.IsReleased = false;

        TryFlushPresentationLane();
    }

    public void NotifyPresentationLaneReleased()
    {
        if (!_presentation.CanAcceptAdvance)
            return;

        // Released means the current sub line was torn down by rollback/stop/request-next
        // before its command entry naturally completed.
        // It should unblock main-side waiting, but it must not consume pending advance.
        _presentation.IsReadyForAdvance = false;
        _presentation.IsReleased = true;
    }

    public void NotifyPresentationLaneNotReady()
    {
        if (_presentation.IsCompleted)
            return;

        _presentation.IsReadyForAdvance = false;
        _presentation.IsReleased = false;
    }

    public void NotifyPresentationLaneCompleted()
    {
        _presentation.MarkCompleted();
    }

    // ---- Forward settle handshake (Phase 2 plumbing) ----
    // Raised by SubPresentationPresenter once per beat, right after its command entry phase
    // resolves (WaitUntilCommandEntryClosedAsync). For a beat whose commands are wait=true,
    // entry-close == completion, so this fires only after the held visual finishes.
    // Main forward flow awaits this (Phase 3) so it respects sub holds without touching
    // NotifyPresentationLaneReady (which stays seek/advance-only).

    public int ForwardSettleEpoch => _presentation.ForwardSettleEpoch;

    public void NotifyPresentationForwardSettled()
    {
        unchecked
        {
            _presentation.ForwardSettleEpoch++;
        }
    }

    // Phase 3 will call this from VNLinePresentationFlow's forward branch.
    // Breaks early when the lane cannot produce further beats (completed / paused / not accepting),
    // so main never hangs on an advance that will never settle.
    public async YarnTask WaitUntilForwardSettledAsync(int targetEpoch, CancellationToken cancel)
    {
        while (_presentation.ForwardSettleEpoch < targetEpoch)
        {
            if (cancel.IsCancellationRequested)
                break;

            if (_presentation.IsCompleted)
                break;

            if (_presentation.IsPaused)
                break;

            if (!_presentation.CanAcceptAdvance)
                break;

            await YarnTask.Yield();
        }
    }

    public int ConsumePresentationAutoAdvanceCount()
    {
        if (!_presentation.CanAcceptAdvance)
            return 0;

        if (_presentation.IsPaused)
            return 0;

        if (_presentation.SuppressFirstAutoAdvance)
        {
            _presentation.SuppressFirstAutoAdvance = false;
            return ConsumeExtraAdvanceOnly();
        }

        if (_presentation.HoldRemainingLines > 0)
        {
            _presentation.HoldRemainingLines--;
            return ConsumeExtraAdvanceOnly();
        }

        int count = 1 + _presentation.ExtraAdvanceCount;
        _presentation.ExtraAdvanceCount = 0;

        return count;
    }

    public int ConsumePresentationSeekResyncCount()
    {
        if (!_presentation.CanAcceptAdvance)
            return 0;

        if (_presentation.IsPaused)
            return 0;

        // During seek, the main flow asks the presentation lane to resync to the base line.
        // Extra forward-play advances are not meaningful in seek, so clear them.
        _presentation.ExtraAdvanceCount = 0;

        return 1;
    }

    public void DispatchPresentationAdvance()
    {
        if (!_presentation.CanAcceptAdvance)
            return;

        if (_presentation.IsPaused)
            return;

        _presentation.PendingAdvanceCount++;
        TryFlushPresentationLane();
    }

    public async YarnTask WaitUntilPresentationLaneReadyAsync()
    {
        while (_presentation.IsBlockingMainFlow)
            await YarnTask.Yield();
    }

    public void HoldPresentation(int lines)
    {
        if (lines < 0)
            lines = 0;

        _presentation.HoldRemainingLines = lines;
    }

    public void AdvancePresentationExtra(int steps)
    {
        if (steps <= 0)
            return;

        if (!_presentation.CanAcceptAdvance)
            return;

        _presentation.ExtraAdvanceCount += steps;
    }

    public void SetPresentationSuppressFirstAutoAdvance(bool suppress)
    {
        _presentation.SuppressFirstAutoAdvance = suppress;
    }

    public void PausePresentation()
    {
        _presentation.IsPaused = true;
    }

    public void ResumePresentation()
    {
        _presentation.IsPaused = false;
        TryFlushPresentationLane();
    }

    // Manual single-step that bypasses the pause gate.
    // Advances the presentation lane exactly `steps` lines, one per ready cycle,
    // regardless of IsPaused. If the lane is mid-line (not ready) when called,
    // the step is buffered in ManualStepBudget and fires the moment the lane
    // becomes ready (see NotifyPresentationLaneReady -> TryFlushPresentationLane).
    public void StepPresentationOnce(int steps = 1)
    {
        if (steps <= 0)
            steps = 1;

        if (!_presentation.CanAcceptAdvance)
            return;

        _presentation.PendingAdvanceCount += steps;
        _presentation.ManualStepBudget += steps;

        // Try to consume one immediately; the rest drains as the lane re-readies.
        AdvanceOnceIfReady();
    }

    private int ConsumeExtraAdvanceOnly()
    {
        int count = _presentation.ExtraAdvanceCount;
        _presentation.ExtraAdvanceCount = 0;
        return count;
    }

    // Pause-aware flush used by the normal auto/dispatch/resume/ready paths.
    // While paused, only the manual-step budget is allowed to drive the lane.
    private void TryFlushPresentationLane()
    {
        if (_presentation.IsPaused)
        {
            if (_presentation.ManualStepBudget > 0)
                AdvanceOnceIfReady();

            return;
        }

        AdvanceOnceIfReady();
    }

    // Pause-agnostic core: advance exactly one line if the lane is ready.
    // Decrements ManualStepBudget alongside PendingAdvanceCount when a manual
    // step is outstanding, so manual and auto advances never double-count.
    private bool AdvanceOnceIfReady()
    {
        if (!_presentation.CanAcceptAdvance)
            return false;

        if (_presentation.PendingAdvanceCount <= 0)
            return false;

        if (!_presentation.IsReadyForAdvance)
            return false;

        _presentation.PendingAdvanceCount--;

        if (_presentation.ManualStepBudget > 0)
            _presentation.ManualStepBudget--;

        _presentation.IsReadyForAdvance = false;
        _presentation.IsReleased = false;

        if (_presentation.Runner == null)
            return false;

        if (!_presentation.Runner.IsDialogueRunning)
        {
            _presentation.MarkCompleted();
            return false;
        }

        _presentation.Runner.RequestNextLine();
        return true;
    }
}