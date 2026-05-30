using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private DialogueRunner _subPresentationRunner;
    private InlineEventMarkupHandler _inlineMarkupHandler;
    private LinePresentationAdvanceState _linePresentationAdvanceState;
    private VNTraceStream _trace;

    private int _pendingSubPresentationAdvanceCount;
    private bool _isSubPresentationReadyForAdvance;
    
    public int PendingSubPresentationAdvanceCount => _pendingSubPresentationAdvanceCount;
    public bool IsSubPresentationReadyForAdvance => _isSubPresentationReadyForAdvance;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        DialogueRunner subPresentationRunner,
        InlineEventMarkupHandler inlineMarkupHandler,
        LinePresentationAdvanceState linePresentationAdvanceState,
        VNTraceStream trace = null)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _subPresentationRunner = subPresentationRunner;
        _inlineMarkupHandler = inlineMarkupHandler;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _trace = trace;
    }

    public void DispatchAdvance()
    {
        Trace("DispatchAdvanceRequested");

        if (!_gate.TryAcceptUserAdvance())
        {
            Trace("DispatchAdvanceRejected", "reason=GateRejected");
            return;
        }

        if (TryDispatchToYarn())
            return;

        _gate.AddCooldownSeconds(0.03f);
    }

    public void DispatchSeekNext()
    {
        Trace("DispatchSeekNextRequested");

        if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning)
        {
            Trace("DispatchSeekNextRejected", "reason=MainRunnerNotRunning");
            return;
        }

        if (_linePresentationAdvanceState != null && !_linePresentationAdvanceState.IsLineFullyShown)
        {
            _inlineMarkupHandler?.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();

            Trace("DispatchSeekNextHurryUp");
        }

        _dialogueRunner.RequestNextLine();
        Trace("DispatchSeekNextRequestNextLine");
    }

    private bool TryDispatchToYarn()
    {
        if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning)
        {
            Trace("TryDispatchToYarnFailed", "reason=MainRunnerNotRunning");
            return false;
        }

        if (_linePresentationAdvanceState != null && !_linePresentationAdvanceState.IsLineFullyShown)
        {
            _inlineMarkupHandler?.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();

            if (_gate != null)
                _gate.AddCooldownSeconds(_gate.CooldownAfterHurryUpSec);

            Trace("TryDispatchToYarnHurryUp");
        }
        else
        {
            _dialogueRunner.RequestNextLine();

            if (_gate != null)
                _gate.AddCooldownSeconds(_gate.CooldownAfterNextLineSec);

            Trace("TryDispatchToYarnNextLine");
        }

        return true;
    }

    public void DispatchSubPresentationAdvance()
    {
        _pendingSubPresentationAdvanceCount++;

        Trace("DispatchSubPresentationAdvanceLatched",
            $"pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");

        TryFlushSubPresentationAdvance("DispatchSubPresentationAdvance");
    }

    public void NotifySubPresentationReadyForAdvance()
    {
        _isSubPresentationReadyForAdvance = true;

        Trace("NotifySubPresentationReadyForAdvance",
            $"pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");

        TryFlushSubPresentationAdvance("NotifySubPresentationReadyForAdvance");
    }

    public void NotifySubPresentationNotReadyForAdvance(string reason = null)
    {
        _isSubPresentationReadyForAdvance = false;

        Trace("NotifySubPresentationNotReadyForAdvance",
            $"reason={reason}, pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");
    }

    private bool TryFlushSubPresentationAdvance(string reason)
    {
        if (_pendingSubPresentationAdvanceCount <= 0)
        {
            Trace("TryFlushSubPresentationAdvanceSkipped",
                $"reason={reason}, cause=NoPending, pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");
            return false;
        }

        if (!_isSubPresentationReadyForAdvance)
        {
            Trace("TryFlushSubPresentationAdvanceSkipped",
                $"reason={reason}, cause=NotReady, pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");
            return false;
        }

        if (_subPresentationRunner == null || !_subPresentationRunner.IsDialogueRunning)
        {
            Trace("TryFlushSubPresentationAdvanceSkipped",
                $"reason={reason}, cause=SubRunnerNotRunning, pending={_pendingSubPresentationAdvanceCount}, ready={_isSubPresentationReadyForAdvance}");
            return false;
        }

        _pendingSubPresentationAdvanceCount--;
        _isSubPresentationReadyForAdvance = false;

        _subPresentationRunner.RequestNextLine();
        return true;
    }

    public void Clear(string reason = null)
    {
        Trace("ClearPendingSubPresentationAdvances",
            $"reason={reason}, pendingBefore={_pendingSubPresentationAdvanceCount}, readyBefore={_isSubPresentationReadyForAdvance}");

        _pendingSubPresentationAdvanceCount = 0;
        _isSubPresentationReadyForAdvance = false;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state = _linePresentationAdvanceState == null
            ? "lineState=null"
            : _linePresentationAdvanceState.Snapshot();

        _trace.Trace(nameof(DialogueAdvanceDispatcher), evt, state, note, this);
    }
}