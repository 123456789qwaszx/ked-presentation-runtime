using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed class VNSideRunnerSyncHub
{
    private sealed class PresentationLaneState
    {
        public DialogueRunner Runner;

        public int PendingAdvanceCount;
        public int HoldRemainingLines;
        public int ExtraAdvanceCount;

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

            IsCompleted = true;
            IsReadyForAdvance = false;
            IsReleased = false;
            IsPaused = false;
        }
    }

    private readonly PresentationLaneState _presentation = new PresentationLaneState();

    public void RegisterPresentationLane(DialogueRunner runner)
    {
        _presentation.Runner = runner;
    }

    public IEnumerator StartPresentationLaneCoroutine(string nodeName)
    {
        if (_presentation.Runner == null)
        {
            Debug.LogWarning("[VNSideRunnerSyncHub] Cannot start presentation lane. Runner is null.");
            yield break;
        }

        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning("[VNSideRunnerSyncHub] Cannot start presentation lane. nodeName is null or empty.");
            yield break;
        }

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

    private int ConsumeExtraAdvanceOnly()
    {
        int count = _presentation.ExtraAdvanceCount;
        _presentation.ExtraAdvanceCount = 0;
        return count;
    }

    private void TryFlushPresentationLane()
    {
        if (!_presentation.CanAcceptAdvance)
            return;

        if (_presentation.IsPaused)
            return;

        if (_presentation.PendingAdvanceCount <= 0)
            return;

        if (!_presentation.IsReadyForAdvance)
            return;

        _presentation.PendingAdvanceCount--;
        _presentation.IsReadyForAdvance = false;
        _presentation.IsReleased = false;

        if (_presentation.Runner == null)
            return;

        if (!_presentation.Runner.IsDialogueRunning)
        {
            _presentation.MarkCompleted();
            return;
        }

        _presentation.Runner.RequestNextLine();
    }
}