using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private InlineEventMarkupHandler _inlineMarkupHandler;
    private VNLinePresentationState _linePresentationAdvanceState;
    private VNTraceStream _trace;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        InlineEventMarkupHandler inlineMarkupHandler,
        VNLinePresentationState linePresentationAdvanceState,
        VNTraceStream trace = null)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
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

        if (!_dialogueRunner.IsDialogueRunning)
            return;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
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
        if (!_dialogueRunner.IsDialogueRunning)
            return false;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _inlineMarkupHandler?.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();
            _gate.AddCooldownSeconds(_gate.CooldownAfterHurryUpSec);
            Trace("TryDispatchToYarnHurryUp");
        }
        else
        {
            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.CooldownAfterNextLineSec);
            Trace("TryDispatchToYarnNextLine");
        }

        return true;
    }

    public void Clear()
    {
        Trace("Clear");
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        //_trace.Trace(nameof(DialogueAdvanceDispatcher), evt, _linePresentationAdvanceState.Snapshot(), note, this);
    }
}