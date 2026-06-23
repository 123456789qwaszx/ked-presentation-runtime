using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private VNLinePresentationState _linePresentationAdvanceState;
    private VNTraceStream _trace;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        VNLinePresentationState linePresentationAdvanceState,
        VNTraceStream trace = null)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _trace = trace;
    }

    public void DispatchAdvance()
    {
        DispatchAdvance(AdvanceRequestKind.User);
    }

    public void DispatchAutoAdvance()
    {
        DispatchAdvance(AdvanceRequestKind.Auto);
    }

    public void DispatchSpeedUpModeAdvance()
    {
        DispatchAdvance(AdvanceRequestKind.SpeedUpMode);
    }

    public void DispatchRapidSkipAdvance()
    {
        DispatchAdvance(AdvanceRequestKind.RapidSkip);
    }

    private void DispatchAdvance(AdvanceRequestKind kind)
    {
        Trace("DispatchAdvanceRequested", $"kind={kind}");

        if (!_gate.TryAccept(kind))
        {
            Trace("DispatchAdvanceRejected", $"kind={kind}, reason=GateRejected");
            return;
        }

        if (TryDispatchToYarn(kind))
            return;

        _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
    }

    public void DispatchSeekNext()
    {
        Trace("DispatchSeekNextRequested");

        if (!_dialogueRunner.IsDialogueRunning)
            return;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _dialogueRunner.RequestHurryUpLine();
            Trace("DispatchSeekNextHurryUp");
        }

        _dialogueRunner.RequestNextLine();
        Trace("DispatchSeekNextRequestNextLine");
    }

    private bool TryDispatchToYarn(AdvanceRequestKind kind)
    {
        if (!_dialogueRunner.IsDialogueRunning)
            return false;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _dialogueRunner.RequestHurryUpLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterHurryUp(kind));
            Trace("TryDispatchToYarnHurryUp", $"kind={kind}");
        }
        else
        {
            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
            Trace("TryDispatchToYarnNextLine", $"kind={kind}");
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